using System.Collections.Generic;
using System.Linq;
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
            NIType intReferenceType = PFTypes.Int32.CreateImmutableReference();
            NIType intOwnedType = PFTypes.Int32.CreateMutableValue();
            CreateTerminal(Direction.Input, intReferenceType, "x in");
            CreateTerminal(Direction.Input, intReferenceType, "y in");
            CreateTerminal(Direction.Output, intReferenceType, "x out");
            CreateTerminal(Direction.Output, intReferenceType, "y out");
            CreateTerminal(Direction.Output, intOwnedType, "result");
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
