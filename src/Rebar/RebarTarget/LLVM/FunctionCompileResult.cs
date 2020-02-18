using LLVMSharp;

namespace Rebar.RebarTarget.LLVM
{
    internal class FunctionCompileResult
    {
        public FunctionCompileResult(Module module, bool isYielding)
        {
            Module = module;
            IsYielding = isYielding;
        }

        public Module Module { get; }

        public bool IsYielding { get; }
    }
}
