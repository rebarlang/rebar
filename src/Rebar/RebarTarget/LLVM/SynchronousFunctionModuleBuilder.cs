using LLVMSharp;

namespace Rebar.RebarTarget.LLVM
{
    internal class SynchronousFunctionModuleBuilder : FunctionModuleBuilder
    {
        public SynchronousFunctionModuleBuilder(Module module, FunctionCompiler functionCompiler, string functionName)
            : base(module)
        {
            var parameterTypes = functionCompiler.GetParameterLLVMTypes();
            LLVMTypeRef syncFunctionType = LLVMSharp.LLVM.FunctionType(LLVMSharp.LLVM.VoidType(), parameterTypes.ToArray(), false);
            SyncFunction = Module.AddFunction(FunctionCompiler.GetSynchronousFunctionName(functionName), syncFunctionType);
            SyncFunctionEntryBlock = SyncFunction.AppendBasicBlock("entry");
        }

        public LLVMValueRef SyncFunction { get; }

        public LLVMBasicBlockRef SyncFunctionEntryBlock { get; }
    }
}
