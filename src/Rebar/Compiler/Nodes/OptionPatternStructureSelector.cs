using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

namespace Rebar.Compiler.Nodes
{
    internal class OptionPatternStructureSelector : BorderNode
    {
        public OptionPatternStructureSelector(OptionPatternStructure parentOptionPatternStructure) : base(parentOptionPatternStructure)
        {
            CreateStandardTerminals(NationalInstruments.CommonModel.Direction.Input, 1u, 1u, NITypes.Void);
        }

        private OptionPatternStructureSelector(Structure parentStructure, OptionPatternStructureSelector toCopy, NodeCopyInfo copyInfo)
            : base(parentStructure, toCopy, copyInfo)
        {
        }

        /// <inheritdoc />
        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new OptionPatternStructureSelector((Structure)newParentNode, this, copyInfo);
        }

        /// <inheritdoc />
        public override T AcceptVisitor<T>(IDfirNodeVisitor<T> visitor)
        {
            return visitor.VisitOptionPatternStructureSelector(this);
        }
    }
}
