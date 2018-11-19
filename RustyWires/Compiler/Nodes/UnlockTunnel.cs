using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

namespace RustyWires.Compiler.Nodes
{
    internal class UnlockTunnel : RustyWiresBorderNode
    {
        public UnlockTunnel(Structure parentStructure) : base(parentStructure)
        {
            CreateStandardTerminals(Direction.Output, 1u, 1u, PFTypes.Void);
        }

        private UnlockTunnel(Structure parentStructure, UnlockTunnel toCopy, NodeCopyInfo copyInfo)
            : base(parentStructure, toCopy, copyInfo)
        {
            Node mappedTunnel;
            if (copyInfo.TryGetMappingFor(toCopy.AssociatedLockTunnel, out mappedTunnel))
            {
                AssociatedLockTunnel = (LockTunnel)mappedTunnel;
                AssociatedLockTunnel.AssociatedUnlockTunnel = this;
            }
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new UnlockTunnel((Structure)newParentNode, this, copyInfo);
        }

        public LockTunnel AssociatedLockTunnel { get; internal set; }

        /// <inheritdoc />
        public override T AcceptVisitor<T>(IRustyWiresDfirNodeVisitor<T> visitor)
        {
            return visitor.VisitUnlockTunnel(this);
        }
    }
}
