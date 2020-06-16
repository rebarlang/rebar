using NationalInstruments.DataTypes;

namespace Rebar.RebarTarget.LLVM.CodeGen
{
    /// <summary>
    /// Represents reading the field value of a variant value, given its address.
    /// </summary>
    internal sealed class GetVariantFieldValue : CodeGenElement
    {
        public GetVariantFieldValue(int variantAddressIndex, int fieldValueIndex, NIType fieldType)
        {
            VariantAddressIndex = variantAddressIndex;
            FieldValueIndex = fieldValueIndex;
            FieldType = fieldType;
        }

        public int VariantAddressIndex { get; }

        public int FieldValueIndex { get; }

        public NIType FieldType { get; }

        /// <inheritdoc />
        public override T AcceptVisitor<T>(ICodeGenElementVisitor<T> visitor)
        {
            return visitor.VisitGetVariantFieldValue(this);
        }
    }
}
