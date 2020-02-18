using LLVMSharp;

namespace Rebar.RebarTarget.LLVM
{
    internal class FunctionCompileResult
    {
        public FunctionCompileResult(Module module, bool isYielding, string[] commonModuleDependencies)
        {
            Module = module;
            IsYielding = isYielding;
            CommonModuleDependencies = commonModuleDependencies;
        }

        public Module Module { get; }

        public bool IsYielding { get; }

        public string[] CommonModuleDependencies { get; }
    }
}
