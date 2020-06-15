using NationalInstruments.DataTypes;

namespace Rebar.RebarTarget.LLVM.CodeGen
{
    /// <summary>
    /// Represents building a variant value from an input field value.
    /// </summary>
    internal sealed class BuildVariant : CodeGenElement
    {
        public BuildVariant(int fieldValueIndex, int variantAddressIndex, int fieldIndex)
        {
            FieldValueIndex = fieldValueIndex;
            VariantAddressIndex = variantAddressIndex;
            FieldIndex = fieldIndex;
        }

        public int FieldValueIndex { get; }

        public int VariantAddressIndex { get; }

        public int FieldIndex { get; }

        /// <inheritdoc />
        public override T AcceptVisitor<T>(ICodeGenElementVisitor<T> visitor)
        {
            return visitor.VisitBuildVariant(this);
        }
    }
}
