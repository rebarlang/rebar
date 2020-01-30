using LLVMSharp;

namespace Rebar.RebarTarget.LLVM
{
    internal class FunctionCompileResult
    {
        public FunctionCompileResult(Module module, string[] commonModuleDependencies)
        {
            Module = module;
            CommonModuleDependencies = commonModuleDependencies;
        }

        public Module Module { get; }

        public string[] CommonModuleDependencies { get; }
    }
}
