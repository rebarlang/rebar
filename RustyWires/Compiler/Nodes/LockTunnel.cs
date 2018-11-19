using System.Linq;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

namespace RustyWires.Compiler.Nodes
{
    internal class LockTunnel : RustyWiresBorderNode
    {
        public LockTunnel(Structure parentStructure) : base(parentStructure)
        {
            CreateStandardTerminals(Direction.Input, 1u, 1u, PFTypes.Void);
        }

        private LockTunnel(Structure parentStructure, LockTunnel toCopy, NodeCopyInfo copyInfo)
            : base(parentStructure, toCopy, copyInfo)
        {
            Node mappedTunnel;
            if (copyInfo.TryGetMappingFor(toCopy.AssociatedUnlockTunnel, out mappedTunnel))
            {
                AssociatedUnlockTunnel = (UnlockTunnel)mappedTunnel;
                AssociatedUnlockTunnel.AssociatedLockTunnel = this;
            }
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new LockTunnel((Structure)newParentNode, this, copyInfo);
        }

        public UnlockTunnel AssociatedUnlockTunnel { get; internal set; }

        /// <inheritdoc />
        public override T AcceptVisitor<T>(IRustyWiresDfirNodeVisitor<T> visitor)
        {
            return visitor.VisitLockTunnel(this);
        }
    }
}
