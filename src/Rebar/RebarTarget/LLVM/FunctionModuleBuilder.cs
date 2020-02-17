using LLVMSharp;

namespace Rebar.RebarTarget.LLVM
{
    internal abstract class FunctionModuleBuilder
    {
        protected FunctionModuleBuilder(Module module)
        {
            Module = module;
        }

        public Module Module { get; }
    }
}
