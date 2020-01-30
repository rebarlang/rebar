using LLVMSharp;

namespace Rebar.RebarTarget.LLVM
{
    internal class FunctionCompileResult
    {
        public FunctionCompileResult(Module module)
        {
            Module = module;
        }

        public Module Module { get; }
    }
}
