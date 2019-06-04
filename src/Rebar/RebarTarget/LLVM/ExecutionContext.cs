using System;
using System.Runtime.InteropServices;
using LLVMSharp;
using NationalInstruments.Composition;
using NationalInstruments.Core;

namespace Rebar.RebarTarget.LLVM
{
    internal class ExecutionContext
    {
        private static readonly LLVMMCJITCompilerOptions _options;
        private static ICompositionHost _host;

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
                CodeModel = LLVMCodeModel.LLVMCodeModelLarge
            };
            LLVMSharp.LLVM.InitializeMCJITCompilerOptions(_options);

            IntPtr outputIntPtr = Marshal.GetFunctionPointerForDelegate(new Output(OutputInt));
            LLVMSharp.LLVM.AddSymbol("outputInt", outputIntPtr);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void Output(int v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void ExecFunc();

        private static void OutputInt(int value)
        {
            string message = $"Output: {value}";
            _host
                .GetSharedExportedValue<IDebugHost>()
                .LogMessage(new DebugMessage("Rebar runtime", DebugMessageSeverity.Information, message));
        }

        private readonly LLVMExecutionEngineRef _engine;
        private readonly Module _globalModule;

        public ExecutionContext(ICompositionHost host)
        {
            _host = host;
            _globalModule = new Module("global");
            string error;
            LLVMBool Success = new LLVMBool(0);
            if (LLVMSharp.LLVM.CreateMCJITCompilerForModule(
                out _engine, 
                GetModuleRefFromModule(_globalModule), 
                _options, 
                out error) != Success)
            {
                Console.WriteLine($"Error: {error}");
            }
        }

        public void LoadFunction(Module functionModule)
        {
            LLVMSharp.LLVM.LinkModules2(
                GetModuleRefFromModule(_globalModule),
                GetModuleRefFromModule(functionModule.Clone()));
        }

        public void ExecuteFunctionTopLevel(string functionName)
        {
            LLVMValueRef funcValue = _globalModule.GetNamedFunction(functionName);
            IntPtr pointerToFunc = LLVMSharp.LLVM.GetPointerToGlobal(_engine, funcValue);
            ExecFunc func = Marshal.GetDelegateForFunctionPointer<ExecFunc>(pointerToFunc);
            func();
        }

        private static LLVMModuleRef GetModuleRefFromModule(Module module)
        {
            System.Reflection.FieldInfo field = typeof(Module).GetField(
                "instance",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (LLVMModuleRef)field.GetValue(module);
        }
    }
}
