using LLVMSharp;

namespace Rebar.RebarTarget.LLVM
{
    internal class FunctionCompileResult
    {
        public FunctionCompileResult(Module module, bool isYielding, bool mayPanic)
        {
            Module = module;
            IsYielding = isYielding;
            MayPanic = mayPanic;
        }

        public Module Module { get; }

        public bool IsYielding { get; }

        public bool MayPanic { get; }
    }
}
