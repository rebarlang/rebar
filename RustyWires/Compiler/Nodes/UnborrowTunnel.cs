using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

namespace RustyWires.Compiler.Nodes
{
    internal class UnborrowTunnel : RustyWiresBorderNode
    {
        public UnborrowTunnel(Structure parentStructure) : base(parentStructure)
        {
            CreateStandardTerminals(Direction.Output, 1u, 1u, PFTypes.Void);
        }

        private UnborrowTunnel(Structure parentStructure, UnborrowTunnel toCopy, NodeCopyInfo copyInfo)
            : base(parentStructure, toCopy, copyInfo)
        {
            Node mappedTunnel;
            if (copyInfo.TryGetMappingFor(toCopy.AssociatedBorrowTunnel, out mappedTunnel))
            {
                AssociatedBorrowTunnel = (BorrowTunnel)mappedTunnel;
                AssociatedBorrowTunnel.AssociatedUnborrowTunnel = this;
            }
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new UnborrowTunnel((Structure)newParentNode, this, copyInfo);
        }

        /// <inheritdoc />
        public override T AcceptVisitor<T>(IRustyWiresDfirNodeVisitor<T> visitor)
        {
            return visitor.VisitUnborrowTunnel(this);
        }

        public BorrowTunnel AssociatedBorrowTunnel { get; internal set; }
    }
}
