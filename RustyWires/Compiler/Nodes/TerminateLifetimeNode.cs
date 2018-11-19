using System.Collections.Generic;
using System.Linq;
using NationalInstruments.Dfir;
using NationalInstruments.DataTypes;

namespace RustyWires.Compiler.Nodes
{
    internal class TerminateLifetimeNode : RustyWiresDfirNode
    {
        public TerminateLifetimeNode(Node parentNode, int inputs, int outputs) : base(parentNode)
        {
            var immutableReferenceType = PFTypes.Void.CreateImmutableReference();
            for (int i = 0; i < inputs; ++i)
            {
                CreateTerminal(Direction.Input, immutableReferenceType, "inner lifetime");
            }
            for (int i = 0; i < outputs; ++i)
            {
                CreateTerminal(Direction.Output, immutableReferenceType, "outer lifetime");
            }
        }

        private TerminateLifetimeNode(Node parentNode, TerminateLifetimeNode nodeToCopy, NodeCopyInfo nodeCopyInfo)
            : base(parentNode, nodeToCopy, nodeCopyInfo)
        {
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new TerminateLifetimeNode(newParentNode, this, copyInfo);
        }

        public TerminateLifetimeErrorState ErrorState { get; set; }

        public int? RequiredInputCount { get; set; }

        public int? RequiredOutputCount { get; set; }

        /// <inheritdoc />
        public override T AcceptVisitor<T>(IRustyWiresDfirNodeVisitor<T> visitor)
        {
            return visitor.VisitTerminateLifetimeNode(this);
        }
    }

    internal enum TerminateLifetimeErrorState
    {
        InputLifetimesNotUnique,

        InputLifetimeCannotBeTerminated,

        NotAllVariablesInLifetimeConnected,

        NoError,
    }
}
