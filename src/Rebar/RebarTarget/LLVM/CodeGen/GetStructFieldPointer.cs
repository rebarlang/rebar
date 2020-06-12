namespace Rebar.RebarTarget.LLVM.CodeGen
{
    /// <summary>
    /// Represents outputing a field offset pointer from an input pointer.
    /// </summary>
    internal sealed class GetStructFieldPointer : CodeGenElement
    {
        public GetStructFieldPointer(int fieldIndex, int inputIndex, int outputIndex)
        {
            FieldIndex = fieldIndex;
            InputIndex = inputIndex;
            OutputIndex = outputIndex;
        }

        public int FieldIndex { get; }

        public int InputIndex { get; }

        public int OutputIndex { get; }

        public override string ToString() => $"GetStructFieldPointer &{InputIndex}.{FieldIndex} -> {OutputIndex}";

        public override T AcceptVisitor<T>(ICodeGenElementVisitor<T> visitor)
        {
            return visitor.VisitGetStructFieldPointer(this);
        }
    }
}
