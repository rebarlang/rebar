using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

namespace RustyWires.Compiler.Nodes
{
    internal class TerminateLifetimeTunnel : RustyWiresBorderNode
    {
        public TerminateLifetimeTunnel(Structure parentStructure) : base(parentStructure)
        {
            CreateStandardTerminals(Direction.Output, 1u, 1u, PFTypes.Void);
        }

        private TerminateLifetimeTunnel(Structure parentStructure, TerminateLifetimeTunnel toCopy, NodeCopyInfo copyInfo)
            : base(parentStructure, toCopy, copyInfo)
        {
            Node mappedTunnel;
            if (copyInfo.TryGetMappingFor((RustyWiresBorderNode)toCopy.BeginLifetimeTunnel, out mappedTunnel))
            {
                BeginLifetimeTunnel = (IBeginLifetimeTunnel)mappedTunnel;
                BeginLifetimeTunnel.TerminateLifetimeTunnel = this;
            }
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new TerminateLifetimeTunnel((Structure)newParentNode, this, copyInfo);
        }

        /// <inheritdoc />
        public override T AcceptVisitor<T>(IRustyWiresDfirNodeVisitor<T> visitor)
        {
            return visitor.VisitTerminateLifetimeTunnel(this);
        }

        public IBeginLifetimeTunnel BeginLifetimeTunnel { get; internal set; }
    }
}
