using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using RustyWires.Common;

namespace RustyWires.Compiler.Nodes
{
    internal class PureBinaryPrimitive : RustyWiresDfirNode
    {
        public PureBinaryPrimitive(Node parentNode, BinaryPrimitiveOps operation) : base(parentNode)
        {
            Operation = operation;
            NIType inputType = operation.GetExpectedInputType();
            NIType inputReferenceType = inputType.CreateImmutableReference();
            CreateTerminal(Direction.Input, inputReferenceType, "x in");
            CreateTerminal(Direction.Input, inputReferenceType, "y in");
            CreateTerminal(Direction.Output, inputReferenceType, "x out");
            CreateTerminal(Direction.Output, inputReferenceType, "y out");
            CreateTerminal(Direction.Output, inputType, "result");
        }

        private PureBinaryPrimitive(Node parentNode, PureBinaryPrimitive nodeToCopy, NodeCopyInfo nodeCopyInfo)
            : base(parentNode, nodeToCopy, nodeCopyInfo)
        {
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new PureBinaryPrimitive(newParentNode, this, copyInfo);
        }

        public BinaryPrimitiveOps Operation { get; }

        /// <inheritdoc />
        public override T AcceptVisitor<T>(IRustyWiresDfirNodeVisitor<T> visitor)
        {
            return visitor.VisitPureBinaryPrimitive(this);
        }
    }
}
