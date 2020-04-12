using LLVMSharp;

namespace Rebar.RebarTarget.LLVM
{
    internal class FunctionCompileResult
    {
        public FunctionCompileResult(ContextFreeModule module, bool isYielding, bool mayPanic)
        {
            Module = module;
            IsYielding = isYielding;
            MayPanic = mayPanic;
        }

        public ContextFreeModule Module { get; }

        public bool IsYielding { get; }

        public bool MayPanic { get; }
    }
}
