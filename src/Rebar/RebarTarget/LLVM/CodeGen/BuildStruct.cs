using LLVMSharp;

namespace Rebar.RebarTarget.LLVM.CodeGen
{
    /// <summary>
    /// Represents building a struct value from a range of input values.
    /// </summary>
    internal sealed class BuildStruct : CodeGenElement
    {
        public BuildStruct(LLVMTypeRef structType, int[] inputIndices, int outputIndex)
        {
            StructType = structType;
            InputIndices = inputIndices;
            OutputIndex = outputIndex;
        }

        public LLVMTypeRef StructType { get; }

        public int[] InputIndices { get; }

        public int OutputIndex { get; }

        public override string ToString() => $"BuildStruct {{{string.Join(", ", InputIndices)}}} -> {OutputIndex}";

        public override T AcceptVisitor<T>(ICodeGenElementVisitor<T> visitor)
        {
            return visitor.VisitBuildStruct(this);
        }
    }
}
