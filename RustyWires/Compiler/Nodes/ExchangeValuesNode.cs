using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using RustyWires.Common;

namespace RustyWires.Compiler.Nodes
{
    internal class ExchangeValuesNode : RustyWiresDfirNode
    {
        public ExchangeValuesNode(Node parentNode) : base(parentNode)
        {
            NIType mutableReferenceType = PFTypes.Void.CreateMutableReference();
            CreateTerminal(Direction.Input, mutableReferenceType, "value in 1");
            CreateTerminal(Direction.Input, mutableReferenceType, "value in 2");
            CreateTerminal(Direction.Output, mutableReferenceType, "value out 1");
            CreateTerminal(Direction.Output, mutableReferenceType, "value out 2");
        }

        private ExchangeValuesNode(Node parentNode, ExchangeValuesNode nodeToCopy, NodeCopyInfo nodeCopyInfo)
            : base(parentNode, nodeToCopy, nodeCopyInfo)
        {
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new ExchangeValuesNode(newParentNode, this, copyInfo);
        }

        /// <inheritdoc />
        public override T AcceptVisitor<T>(IRustyWiresDfirNodeVisitor<T> visitor)
        {
            return visitor.VisitExchangeValuesNode(this);
        }
    }
}
