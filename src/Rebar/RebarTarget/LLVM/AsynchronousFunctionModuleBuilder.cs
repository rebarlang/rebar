using LLVMSharp;

namespace Rebar.RebarTarget.LLVM
{
    internal class AsynchronousFunctionModuleBuilder : FunctionModuleBuilder
    {
        public AsynchronousFunctionModuleBuilder(Module module)
            : base(module)
        {
        }
    }
}
