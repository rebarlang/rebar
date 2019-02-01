using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using RustyWires.Common;

namespace RustyWires.Compiler.Nodes
{
    internal class PureUnaryPrimitive : RustyWiresDfirNode
    {
        public PureUnaryPrimitive(Node parentNode, UnaryPrimitiveOps operation) : base(parentNode)
        {
            Operation = operation;
            NIType inputType = operation.GetExpectedInputType();
            NIType inputReferenceType = inputType.CreateImmutableReference();
            CreateTerminal(Direction.Input, inputReferenceType, "x in");
            CreateTerminal(Direction.Output, inputReferenceType, "x out");
            CreateTerminal(Direction.Output, inputType, "result");
        }

        private PureUnaryPrimitive(Node parentNode, PureUnaryPrimitive nodeToCopy, NodeCopyInfo nodeCopyInfo)
            : base(parentNode, nodeToCopy, nodeCopyInfo)
        {
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new PureUnaryPrimitive(newParentNode, this, copyInfo);
        }

        public UnaryPrimitiveOps Operation { get; }

        /// <inheritdoc />
        public override T AcceptVisitor<T>(IRustyWiresDfirNodeVisitor<T> visitor)
        {
            return visitor.VisitPureUnaryPrimitive(this);
        }
    }
}
