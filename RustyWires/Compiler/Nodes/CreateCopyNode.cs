using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

namespace RustyWires.Compiler.Nodes
{
    internal class CreateCopyNode : RustyWiresDfirNode
    {
        private readonly Terminal _refInTerminal, _refOutTerminal, _valueOutTerminal;

        public CreateCopyNode(Node parentNode) : base(parentNode)
        {
            var immutableReferenceType = PFTypes.Void.CreateImmutableReference();
            _refInTerminal = CreateTerminal(Direction.Input, immutableReferenceType, "ref in");
            _refOutTerminal = CreateTerminal(Direction.Output, immutableReferenceType, "ref out");
            _valueOutTerminal = CreateTerminal(Direction.Output, PFTypes.Void, "value");
        }

        private CreateCopyNode(Node parentNode, CreateCopyNode nodeToCopy, NodeCopyInfo nodeCopyInfo)
            : base(parentNode, nodeToCopy, nodeCopyInfo)
        {
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new CreateCopyNode(newParentNode, this, copyInfo);
        }

        /// <inheritdoc />
        public override T AcceptVisitor<T>(IRustyWiresDfirNodeVisitor<T> visitor)
        {
            return visitor.VisitCreateCopyNode(this);
        }
    }
}
