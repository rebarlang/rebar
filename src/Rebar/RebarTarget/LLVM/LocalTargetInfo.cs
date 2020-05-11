using LLVMSharp;

namespace Rebar.RebarTarget.LLVM
{
    public static class LocalTargetInfo
    {
        static LocalTargetInfo()
        {
            var module = new Module("m");
            module.CreateMCJITCompilerForModule();

            TargetData = LLVMSharp.LLVM.CreateTargetData(module.GetDataLayout());
        }

        public static LLVMTargetDataRef TargetData { get; }
    }
}
