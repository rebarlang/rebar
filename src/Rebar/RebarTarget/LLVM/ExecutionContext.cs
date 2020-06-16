using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using LLVMSharp;

namespace Rebar.RebarTarget.LLVM
{
    public class ExecutionContext : IDisposable
    {
        private static IRebarTargetRuntimeServices _runtimeServices;
        private static readonly IntPtr _topLevelCallerWakerFunctionPtr;
        private static readonly Queue<ScheduledTask> _scheduledTasks = new Queue<ScheduledTask>();
        private static bool _executing;

        static ExecutionContext()
        {
            AddSymbolForDelegate("schedule", _schedule);
            AddSymbolForDelegate("output_string", _outputString);
            AddSymbolForDelegate("fake_drop", _fakeDrop);

            IntPtr kernel32Instance = LoadLibrary("kernel32.dll");
            LLVMSharp.LLVM.AddSymbol("CopyMemory", GetProcAddress(kernel32Instance, "RtlCopyMemory"));
            LLVMSharp.LLVM.AddSymbol("CloseHandle", GetProcAddress(kernel32Instance, "CloseHandle"));
            LLVMSharp.LLVM.AddSymbol("CreateFileA", GetProcAddress(kernel32Instance, "CreateFileA"));
            LLVMSharp.LLVM.AddSymbol("ReadFile", GetProcAddress(kernel32Instance, "ReadFile"));
            LLVMSharp.LLVM.AddSymbol("WriteFile", GetProcAddress(kernel32Instance, "WriteFile"));

            _topLevelCallerWakerFunctionPtr = Marshal.GetFunctionPointerForDelegate<CallerWaker>(_topLevelCallerWaker);
        }

        private static void AddSymbolForDelegate<TDelegate>(string symbolName, TDelegate del)
        {
            IntPtr delegatePtr = Marshal.GetFunctionPointerForDelegate<TDelegate>(del);
            LLVMSharp.LLVM.AddSymbol(symbolName, delegatePtr);
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

        private struct ScheduledTask
        {
            public IntPtr TaskFunction;
            public IntPtr TaskState;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void ScheduleDelegate(IntPtr taskFunction, IntPtr taskState);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void OutputBoolDelegate(bool v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void OutputStringDelegate(IntPtr bufferPtr, int size);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void FakeDropDelegate(int v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr ParameterlessInitializeStateFunc();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PollFunc(IntPtr statePtr, IntPtr callerWakerFunctionPtr, IntPtr callerWakerStatePtr);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void ScheduledTaskFunction(IntPtr statePtr);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void CallerWaker(IntPtr statePtr);

        private static readonly ScheduleDelegate _schedule = Schedule;
        private static readonly CallerWaker _topLevelCallerWaker = TopLevelCallerWaker;

        private static void OutputString(IntPtr bufferPtr, int size)
        {
            byte[] data = new byte[size];
            Marshal.Copy(bufferPtr, data, 0, size);
            string str = Encoding.UTF8.GetString(data);
            _runtimeServices.Output(str);
        }

        private static OutputStringDelegate _outputString = OutputString;

        private static void FakeDrop(int id)
        {
            _runtimeServices.FakeDrop(id);
        }

        private static FakeDropDelegate _fakeDrop = FakeDrop;

        private readonly ContextWrapper _contextWrapper;
        private readonly LLVMExecutionEngineRef _engine;
        private readonly Module _globalModule;
        private readonly LLVMTargetDataRef _targetData;

        public ExecutionContext(IRebarTargetRuntimeServices runtimeServices)
        {
            _contextWrapper = new ContextWrapper();
            _runtimeServices = runtimeServices;
            _globalModule = _contextWrapper.CreateModule("global");
            foreach (ContextFreeModule module in new[]
                {
                    CommonModules.FakeDropModule,
                    CommonModules.SchedulerModule,
                    CommonModules.StringModule,
                    CommonModules.OutputModule,
                    CommonModules.RangeModule,
                    CommonModules.FileModule
                })
            {
                _globalModule.LinkInModule(_contextWrapper.LoadContextFreeModule(module));
            }

            _engine = _globalModule.CreateMCJITCompilerForModule();
            _targetData = LLVMSharp.LLVM.GetExecutionEngineTargetData(_engine);
        }

        public void LoadFunction(ContextFreeModule contextFreeModule)
        {
            Module functionModule = _contextWrapper.LoadContextFreeModule(contextFreeModule);
            functionModule.VerifyAndThrowIfInvalid();
            _globalModule.LinkInModule(functionModule.Clone());
        }

        public void ExecuteFunctionTopLevel(string functionName, bool isAsync)
        {
            byte completionStatus = 0;
            if (isAsync)
            {
                ParameterlessInitializeStateFunc initializeFunc = GetNamedFunctionDelegate<ParameterlessInitializeStateFunc>(
                    FunctionNames.GetInitializeStateFunctionName(functionName));
                PollFunc pollFunc = GetNamedFunctionDelegate<PollFunc>(
                    FunctionNames.GetPollFunctionName(functionName));

                _executing = true;
                IntPtr statePtr = initializeFunc();
                pollFunc(statePtr, _topLevelCallerWakerFunctionPtr, IntPtr.Zero);
                ExecuteTasksUntilDone();
                if (_executing)
                {
                    throw new InvalidOperationException("Execution queue is empty, but top-level waker was not called.");
                }

                unsafe
                {
                    completionStatus = *((byte*)statePtr);
                }
                // TODO: deallocate statePtr
            }
            else
            {
                Action func = GetNamedFunctionDelegate<Action>(
                    FunctionNames.GetSynchronousFunctionName(functionName));

                _executing = true;
                /* completionStatus = */func();
                _executing = false;
            }
            if (completionStatus == RuntimeConstants.FunctionPanickedStatus)
            {
                _runtimeServices.PanicOccurred = true;
            }
        }

        private T GetNamedFunctionDelegate<T>(string functionName)
        {
            LLVMValueRef funcValue = _globalModule.GetNamedFunction(functionName);
            funcValue.ThrowIfNull();
            IntPtr pointerToFunc = LLVMSharp.LLVM.GetPointerToGlobal(_engine, funcValue);
            return Marshal.GetDelegateForFunctionPointer<T>(pointerToFunc);
        }

        private void ExecuteTasksUntilDone()
        {
            while (_scheduledTasks.Count > 0)
            {
                if (!_executing)
                {
                    throw new InvalidOperationException("Top-level waker was called, but execution queue is not empty.");
                }
                ScheduledTask scheduledTask = _scheduledTasks.Dequeue();
                ScheduledTaskFunction scheduledTaskFunction = Marshal.GetDelegateForFunctionPointer<ScheduledTaskFunction>(scheduledTask.TaskFunction);
                scheduledTaskFunction(scheduledTask.TaskState);
            }
        }

        private static void Schedule(IntPtr taskFunction, IntPtr taskState)
        {
            _scheduledTasks.Enqueue(new ScheduledTask() { TaskFunction = taskFunction, TaskState = taskState });
        }

        private static void TopLevelCallerWaker(IntPtr statePtr)
        {
            _executing = false;
        }

        public byte[] ReadGlobalData(string globalName)
        {
            LLVMValueRef globalValue = _globalModule.GetNamedGlobal(globalName);
            LLVMTypeRef pointedToType = globalValue.TypeOf().GetElementType();

            int size = (int)LLVMSharp.LLVM.StoreSizeOfType(_targetData, pointedToType);
            IntPtr globalAddress = new IntPtr((long)LLVMSharp.LLVM.GetGlobalValueAddress(_engine, globalName));

            byte[] data = new byte[size];
            Marshal.Copy(globalAddress, data, 0, size);
            return data;
        }

        public void Dispose()
        {
            _contextWrapper.Dispose();
        }
    }
}
