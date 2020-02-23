using LLVMSharp;

namespace Rebar.RebarTarget.LLVM
{
    internal class FunctionCompileResult
    {
        public FunctionCompileResult(Module module, bool isYielding)
        {
            Module = module;
            IsYielding = isYielding;
            MayPanic = false;
        }

        public Module Module { get; }

        public bool IsYielding { get; }

        public bool MayPanic { get; }
    }
}
