using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

namespace Rebar.Compiler.Nodes
{
    internal sealed class VariantMatchStructureSelector : BorderNode
    {
        public VariantMatchStructureSelector(VariantMatchStructure parentVariantMatchStructure)
            : base(parentVariantMatchStructure)
        {
            CreateStandardTerminals(NationalInstruments.CommonModel.Direction.Input, 1u, 1u, NITypes.Void);
        }

        private VariantMatchStructureSelector(VariantMatchStructure parentStructure, VariantMatchStructureSelector toCopy, NodeCopyInfo copyInfo)
            : base(parentStructure, toCopy, copyInfo)
        {
        }

        /// <inheritdoc />
        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new VariantMatchStructureSelector((VariantMatchStructure)newParentNode, this, copyInfo);
        }

        /// <inheritdoc />
        public override T AcceptVisitor<T>(IDfirNodeVisitor<T> visitor)
        {
            return visitor.VisitVariantMatchStructureSelector(this);
        }
    }
}
