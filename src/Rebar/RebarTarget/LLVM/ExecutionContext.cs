using System;
using System.Runtime.InteropServices;
using System.Text;
using LLVMSharp;

namespace Rebar.RebarTarget.LLVM
{
    public class ExecutionContext
    {
        private static readonly LLVMMCJITCompilerOptions _options;
        private static IRebarTargetRuntimeServices _runtimeServices;

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

            AddSymbolForDelegate<OutputIntDelegate>("output_int", _outputInt);
            AddSymbolForDelegate<OutputStringDelegate>("output_string", _outputString);

            IntPtr kernel32Instance = LoadLibrary("kernel32.dll");
            IntPtr copyMemoryProc = GetProcAddress(kernel32Instance, "RtlCopyMemory");
            LLVMSharp.LLVM.AddSymbol("CopyMemory", copyMemoryProc);
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

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void OutputIntDelegate(int v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void OutputStringDelegate(IntPtr bufferPtr, int size);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void ExecFunc();

        private static void OutputInt(int value)
        {
            _runtimeServices.Output(value.ToString());
        }

        private static OutputIntDelegate _outputInt = OutputInt;

        private static void OutputString(IntPtr bufferPtr, int size)
        {
            byte[] data = new byte[size];
            Marshal.Copy(bufferPtr, data, 0, size);
            string str = Encoding.UTF8.GetString(data);
            _runtimeServices.Output(str);
        }

        private static OutputStringDelegate _outputString = OutputString;

        private readonly LLVMExecutionEngineRef _engine;
        private readonly Module _globalModule;
        private readonly LLVMTargetDataRef _targetData;

        public ExecutionContext(IRebarTargetRuntimeServices runtimeServices)
        {
            _runtimeServices = runtimeServices;
            _globalModule = new Module("global");
            _globalModule.LinkInModule(CommonModules.StringModule.Clone());
            _globalModule.LinkInModule(CommonModules.RangeModule.Clone());

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

        public void LoadFunction(Module functionModule)
        {
            functionModule.VerifyAndThrowIfInvalid();
            _globalModule.LinkInModule(functionModule.Clone());
        }

        public void ExecuteFunctionTopLevel(string functionName)
        {
            LLVMValueRef funcValue = _globalModule.GetNamedFunction(functionName);
            funcValue.ThrowIfNull();
            IntPtr pointerToFunc = LLVMSharp.LLVM.GetPointerToGlobal(_engine, funcValue);
            ExecFunc func = Marshal.GetDelegateForFunctionPointer<ExecFunc>(pointerToFunc);
            func();
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
