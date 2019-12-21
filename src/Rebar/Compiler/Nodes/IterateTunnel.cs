using NationalInstruments.Dfir;
using NationalInstruments.DataTypes;
using Rebar.Common;

namespace Rebar.Compiler.Nodes
{
    /// <summary>
    /// DFIR representation of <see cref="SourceModel.LoopIterateTunnel"/>.
    /// </summary>
    internal class IterateTunnel : BorderNode, IBeginLifetimeTunnel
    {
        public IterateTunnel(Structure parentStructure) : base(parentStructure)
        {
            CreateStandardTerminals(Direction.Input, 1u, 1u, PFTypes.Void);
        }

        private IterateTunnel(Structure parentStructure, IterateTunnel toCopy, NodeCopyInfo copyInfo)
            : base(parentStructure, toCopy, copyInfo)
        {
            IteratorNextFunctionType = toCopy.IteratorNextFunctionType;
            Node mappedTunnel;
            if (copyInfo.TryGetMappingFor(toCopy.TerminateLifetimeTunnel, out mappedTunnel))
            {
                TerminateLifetimeTunnel = (TerminateLifetimeTunnel)mappedTunnel;
                TerminateLifetimeTunnel.BeginLifetimeTunnel = this;
            }
        }

        internal FunctionType IteratorNextFunctionType { get; set; }

        /// <inheritdoc />
        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new IterateTunnel((Structure)newParentNode, this, copyInfo);
        }

        /// <inheritdoc />
        public TerminateLifetimeTunnel TerminateLifetimeTunnel { get; set; }

        /// <inheritdoc />
        public override T AcceptVisitor<T>(IDfirNodeVisitor<T> visitor)
        {
            return visitor.VisitIterateTunnel(this);
        }

        public string IntermediateValueName => $"iterateIntermediate{UniqueId}";
    }
}
