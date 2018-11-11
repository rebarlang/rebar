using System.Collections.Generic;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using RustyWires.Common;

namespace RustyWires.Compiler.Nodes
{
    internal class MutatingBinaryPrimitive : RustyWiresDfirNode
    {
        public MutatingBinaryPrimitive(Node parentNode, BinaryPrimitiveOps operation) : base(parentNode)
        {
            Operation = operation;
            NIType intMutableReferenceType = PFTypes.Int32.CreateMutableReference();
            NIType intImmutableReferenceType = PFTypes.Int32.CreateImmutableReference();
            CreateTerminal(Direction.Input, intMutableReferenceType, "x in");
            CreateTerminal(Direction.Input, intImmutableReferenceType, "y in");
            CreateTerminal(Direction.Output, intMutableReferenceType, "x out");
            CreateTerminal(Direction.Output, intImmutableReferenceType, "y out");
        }

        private MutatingBinaryPrimitive(Node parentNode, MutatingBinaryPrimitive nodeToCopy, NodeCopyInfo nodeCopyInfo)
            : base(parentNode, nodeToCopy, nodeCopyInfo)
        {
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new MutatingBinaryPrimitive(newParentNode, this, copyInfo);
        }

        public BinaryPrimitiveOps Operation { get; }

        /// <inheritdoc />
        public override T AcceptVisitor<T>(IRustyWiresDfirNodeVisitor<T> visitor)
        {
            return visitor.VisitMutatingBinaryPrimitive(this);
        }
    }
}
