using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using RustyWires.Common;

namespace RustyWires.Compiler.Nodes
{
    internal class MutablePassthroughNode : RustyWiresDfirNode
    {
        private readonly Terminal _inputTerminal, _outputTerminal;

        public MutablePassthroughNode(Node parentNode) : base(parentNode)
        {
            var mutableReferenceType = PFTypes.Void.CreateMutableReference();
            _inputTerminal = CreateTerminal(Direction.Input, mutableReferenceType, "ref in");
            _outputTerminal = CreateTerminal(Direction.Output, mutableReferenceType, "ref out");
        }

        private MutablePassthroughNode(Node parentNode, MutablePassthroughNode nodeToCopy, NodeCopyInfo nodeCopyInfo)
            : base(parentNode, nodeToCopy, nodeCopyInfo)
        {
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new MutablePassthroughNode(newParentNode, this, copyInfo);
        }

        /// <inheritdoc />
        public override T AcceptVisitor<T>(IRustyWiresDfirNodeVisitor<T> visitor)
        {
            return visitor.VisitMutablePassthroughNode(this);
        }
    }
}
