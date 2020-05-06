using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

namespace Rebar.Compiler.Nodes
{
    internal class BorrowTunnel : BorderNode, IBeginLifetimeTunnel
    {
        public BorrowTunnel(Structure parentStructure, Common.BorrowMode borrowMode) : base(parentStructure)
        {
            CreateStandardTerminals(NationalInstruments.CommonModel.Direction.Input, 1u, 1u, NITypes.Void);
            BorrowMode = borrowMode;
        }

        private BorrowTunnel(Structure parentStructure, BorrowTunnel toCopy, NodeCopyInfo copyInfo)
            : base(parentStructure, toCopy, copyInfo)
        {
            BorrowMode = toCopy.BorrowMode;
            Node mappedTunnel;
            if (copyInfo.TryGetMappingFor(toCopy.TerminateLifetimeTunnel, out mappedTunnel))
            {
                TerminateLifetimeTunnel = (TerminateLifetimeTunnel)mappedTunnel;
                TerminateLifetimeTunnel.BeginLifetimeTunnel = this;
            }
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new BorrowTunnel((Structure)newParentNode, this, copyInfo);
        }

        public Common.BorrowMode BorrowMode { get; }

        public TerminateLifetimeTunnel TerminateLifetimeTunnel { get; set; }

        /// <inheritdoc />
        public override T AcceptVisitor<T>(IDfirNodeVisitor<T> visitor)
        {
            return visitor.VisitBorrowTunnel(this);
        }
    }
}
