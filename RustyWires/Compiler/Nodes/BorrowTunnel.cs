using System;
using System.Linq;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

namespace RustyWires.Compiler.Nodes
{
    internal class BorrowTunnel : RustyWiresBorderNode
    {
        public BorrowTunnel(Structure parentStructure, Common.BorrowMode borrowMode) : base(parentStructure)
        {
            CreateStandardTerminals(Direction.Input, 1u, 1u, PFTypes.Void);
            BorrowMode = borrowMode;
        }

        private BorrowTunnel(Structure parentStructure, BorrowTunnel toCopy, NodeCopyInfo copyInfo)
            : base(parentStructure, toCopy, copyInfo)
        {
            BorrowMode = toCopy.BorrowMode;
            Node mappedTunnel;
            if (copyInfo.TryGetMappingFor(toCopy.AssociatedUnborrowTunnel, out mappedTunnel))
            {
                AssociatedUnborrowTunnel = (UnborrowTunnel)mappedTunnel;
                AssociatedUnborrowTunnel.AssociatedBorrowTunnel = this;
            }
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new BorrowTunnel((Structure)newParentNode, this, copyInfo);
        }

        public Common.BorrowMode BorrowMode { get; }

        public UnborrowTunnel AssociatedUnborrowTunnel { get; internal set; }

        /// <inheritdoc />
        public override T AcceptVisitor<T>(IRustyWiresDfirNodeVisitor<T> visitor)
        {
            return visitor.VisitBorrowTunnel(this);
        }
    }
}
