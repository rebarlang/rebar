using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

namespace Rebar.Compiler.Nodes
{
    internal class LoopConditionTunnel : BorderNode, IBeginLifetimeTunnel
    {
        public LoopConditionTunnel(Loop parentLoop) : base(parentLoop)
        {
            CreateStandardTerminals(NationalInstruments.CommonModel.Direction.Input, 1u, 1u, NITypes.Void);
        }

        private LoopConditionTunnel(Structure parentStructure, LoopConditionTunnel toCopy, NodeCopyInfo copyInfo)
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
            return new LoopConditionTunnel((Structure)newParentNode, this, copyInfo);
        }

        /// <inheritdoc />
        public override T AcceptVisitor<T>(IDfirNodeVisitor<T> visitor)
        {
            return visitor.VisitLoopConditionTunnel(this);
        }

        public TerminateLifetimeTunnel TerminateLifetimeTunnel { get; set; }
    }
}
