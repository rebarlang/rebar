using NationalInstruments.Dfir;
using NationalInstruments.DataTypes;

namespace RustyWires.Compiler.Nodes
{
    /// <summary>
    /// DFIR representation of <see cref="SourceModel.LoopIterateTunnel"/>.
    /// </summary>
    internal class IterateTunnel : RustyWiresBorderNode, IBeginLifetimeTunnel
    {
        public IterateTunnel(Structure parentStructure) : base(parentStructure)
        {
            CreateStandardTerminals(Direction.Input, 1u, 1u, PFTypes.Void);
        }

        private IterateTunnel(Structure parentStructure, IterateTunnel toCopy, NodeCopyInfo copyInfo)
            : base(parentStructure, toCopy, copyInfo)
        {
            Node mappedTunnel;
            if (copyInfo.TryGetMappingFor(toCopy.TerminateLifetimeTunnel, out mappedTunnel))
            {
                TerminateLifetimeTunnel = (TerminateLifetimeTunnel)mappedTunnel;
                TerminateLifetimeTunnel.BeginLifetimeTunnel = this;
            }
        }

        /// <inheritdoc />
        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new IterateTunnel((Structure)newParentNode, this, copyInfo);
        }

        /// <inheritdoc />
        public TerminateLifetimeTunnel TerminateLifetimeTunnel { get; set; }

        /// <inheritdoc />
        public override T AcceptVisitor<T>(IRustyWiresDfirNodeVisitor<T> visitor)
        {
            return visitor.VisitIterateTunnel(this);
        }
    }
}
