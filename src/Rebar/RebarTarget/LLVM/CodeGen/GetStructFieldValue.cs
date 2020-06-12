namespace Rebar.RebarTarget.LLVM.CodeGen
{
    /// <summary>
    /// Represents outputing a field value from an input struct value.
    /// </summary>
    internal sealed class GetStructFieldValue : CodeGenElement
    {
        public GetStructFieldValue(int fieldIndex, int inputIndex, int outputIndex)
        {
            FieldIndex = fieldIndex;
            InputIndex = inputIndex;
            OutputIndex = outputIndex;
        }

        public int FieldIndex { get; }

        public int InputIndex { get; }

        public int OutputIndex { get; }

        public override string ToString() => $"GetStructFieldValue {InputIndex}.{FieldIndex} -> {OutputIndex}";

        public override T AcceptVisitor<T>(ICodeGenElementVisitor<T> visitor)
        {
            return visitor.VisitGetStructFieldValue(this);
        }
    }
}
