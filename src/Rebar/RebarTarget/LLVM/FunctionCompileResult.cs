using LLVMSharp;

namespace Rebar.RebarTarget.LLVM
{
    internal class FunctionCompileResult
    {
        public FunctionCompileResult(Module module, bool isYielding, string[] commonModuleDependencies)
        {
            Module = module;
            IsYielding = isYielding;
            MayPanic = false;
            CommonModuleDependencies = commonModuleDependencies;
        }

        public Module Module { get; }

        public bool IsYielding { get; }

        public bool MayPanic { get; }

        public string[] CommonModuleDependencies { get; }
    }
}
