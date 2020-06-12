using LLVMSharp;

namespace Rebar.RebarTarget.LLVM.CodeGen
{
    /// <summary>
    /// Represents a random-access storage for intermediate values produced and consumed by the code generated from <see cref="CodeGenElement"/>s.
    /// </summary>
    internal interface ICodeGenValueStorage
    {
        LLVMValueRef this[int index] { get; set; }
    }
}
