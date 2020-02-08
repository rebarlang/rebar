using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using LLVMSharp;

namespace Rebar.RebarTarget.LLVM
{
    public class ExecutionContext
    {
        private static readonly LLVMMCJITCompilerOptions _options;
        private static IRebarTargetRuntimeServices _runtimeServices;
        private static readonly IntPtr _topLevelCallerWakerFunctionPtr;
        private static readonly Queue<ScheduledTask> _scheduledTasks = new Queue<ScheduledTask>();
        private static bool _executing;

        static ExecutionContext()
        {
            LLVMSharp.LLVM.LinkInMCJIT();

            LLVMSharp.LLVM.InitializeX86TargetMC();
            LLVMSharp.LLVM.InitializeX86Target();
            LLVMSharp.LLVM.InitializeX86TargetInfo();
            LLVMSharp.LLVM.InitializeX86AsmParser();
            LLVMSharp.LLVM.InitializeX86AsmPrinter();

            _options = new LLVMMCJITCompilerOptions
            {
                NoFramePointerElim = 1,
                // TODO: comment about why this is necessary
                CodeModel = LLVMCodeModel.LLVMCodeModelLarge,
            };
            LLVMSharp.LLVM.InitializeMCJITCompilerOptions(_options);

            AddSymbolForDelegate("schedule", _schedule);
            AddSymbolForDelegate("output_bool", _outputBool);
            AddSymbolForDelegate("output_int8", _outputInt8);
            AddSymbolForDelegate("output_uint8", _outputUInt8);
            AddSymbolForDelegate("output_int16", _outputInt16);
            AddSymbolForDelegate("output_uint16", _outputUInt16);
            AddSymbolForDelegate("output_int32", _outputInt32);
            AddSymbolForDelegate("output_uint32", _outputUInt32);
            AddSymbolForDelegate("output_int64", _outputInt64);
            AddSymbolForDelegate("output_uint64", _outputUInt64);
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
        private delegate void OutputInt8Delegate(sbyte v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void OutputUInt8Delegate(byte v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void OutputInt16Delegate(short v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void OutputUInt16Delegate(ushort v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void OutputInt32Delegate(int v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void OutputUInt32Delegate(uint v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void OutputInt64Delegate(long v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void OutputUInt64Delegate(ulong v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void OutputStringDelegate(IntPtr bufferPtr, int size);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void ExecFunc(IntPtr callerWakerFunctionPtr, IntPtr callerWakerStatePtr);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void ScheduledTaskFunction(IntPtr statePtr);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void CallerWaker(IntPtr statePtr);

        private static readonly ScheduleDelegate _schedule = Schedule;
        private static readonly CallerWaker _topLevelCallerWaker = TopLevelCallerWaker;

        private static void OutputBool(bool value)
        {
            _runtimeServices.Output(value ? "true" : "false");
        }

        private static OutputBoolDelegate _outputBool = OutputBool;

        private static void OutputInt8(sbyte value)
        {
            _runtimeServices.Output(value.ToString());
        }

        private static OutputInt8Delegate _outputInt8 = OutputInt8;

        private static void OutputUInt8(byte value)
        {
            _runtimeServices.Output(value.ToString());
        }

        private static OutputUInt8Delegate _outputUInt8 = OutputUInt8;

        private static void OutputInt16(short value)
        {
            _runtimeServices.Output(value.ToString());
        }

        private static OutputInt16Delegate _outputInt16 = OutputInt16;

        private static void OutputUInt16(ushort value)
        {
            _runtimeServices.Output(value.ToString());
        }

        private static OutputUInt16Delegate _outputUInt16 = OutputUInt16;

        private static void OutputInt32(int value)
        {
            _runtimeServices.Output(value.ToString());
        }

        private static OutputInt32Delegate _outputInt32 = OutputInt32;

        private static void OutputUInt32(uint value)
        {
            _runtimeServices.Output(value.ToString());
        }

        private static OutputUInt32Delegate _outputUInt32 = OutputUInt32;

        private static void OutputInt64(long value)
        {
            _runtimeServices.Output(value.ToString());
        }

        private static OutputInt64Delegate _outputInt64 = OutputInt64;

        private static void OutputUInt64(ulong value)
        {
            _runtimeServices.Output(value.ToString());
        }

        private static OutputUInt64Delegate _outputUInt64 = OutputUInt64;

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

        private static OutputInt32Delegate _fakeDrop = FakeDrop;

        private readonly LLVMExecutionEngineRef _engine;
        private readonly Module _globalModule;
        private readonly LLVMTargetDataRef _targetData;
        private readonly HashSet<string> _linkedCommonModules = new HashSet<string>();

        public ExecutionContext(IRebarTargetRuntimeServices runtimeServices)
        {
            _runtimeServices = runtimeServices;
            _globalModule = new Module("global");

            string error;
            LLVMBool Success = new LLVMBool(0);
            if (LLVMSharp.LLVM.CreateMCJITCompilerForModule(
                out _engine,
                _globalModule.GetModuleRef(), 
                _options, 
                out error) != Success)
            {
                throw new InvalidOperationException($"Error creating JIT: {error}");
            }
            _targetData = LLVMSharp.LLVM.GetExecutionEngineTargetData(_engine);
        }

        public void LoadFunction(Module functionModule, IEnumerable<string> commonModuleDependencies)
        {
            functionModule.VerifyAndThrowIfInvalid();
            _globalModule.LinkInModule(functionModule.Clone());
            foreach (string commonModuleName in commonModuleDependencies)
            {
                if (!_linkedCommonModules.Contains(commonModuleName))
                {
                    _globalModule.LinkInModule(CommonModules.GetModule(commonModuleName).Clone());
                    _linkedCommonModules.Add(commonModuleName);
                }
            }
        }

        public void ExecuteFunctionTopLevel(string functionName)
        {
            LLVMValueRef funcValue = _globalModule.GetNamedFunction(functionName);
            funcValue.ThrowIfNull();
            IntPtr pointerToFunc = LLVMSharp.LLVM.GetPointerToGlobal(_engine, funcValue);
            ExecFunc func = Marshal.GetDelegateForFunctionPointer<ExecFunc>(pointerToFunc);

            _executing = true;
            func(_topLevelCallerWakerFunctionPtr, IntPtr.Zero);
            ExecuteTasksUntilDone();
            if (_executing)
            {
                throw new InvalidOperationException("Execution queue is empty, but top-level waker was not called.");
            }
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
    }
}
