namespace Rebar.RebarTarget.LLVM.CodeGen
{
    /// <summary>
    /// Represents reading the tag value of a variant value given its address.
    /// </summary>
    internal sealed class GetVariantTagValue : CodeGenElement
    {
        public GetVariantTagValue(int variantAddressIndex, int tagIndex)
        {
            VariantAddressIndex = variantAddressIndex;
            TagIndex = tagIndex;
        }

        public int VariantAddressIndex { get; }

        public int TagIndex { get; }

        /// <inheritdoc />
        public override T AcceptVisitor<T>(ICodeGenElementVisitor<T> visitor)
        {
            return visitor.VisitGetVariantTagValue(this);
        }
    }
}
