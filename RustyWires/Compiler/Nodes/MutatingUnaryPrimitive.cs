using System.Collections.Generic;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using RustyWires.Common;

namespace RustyWires.Compiler.Nodes
{
    internal class MutatingUnaryPrimitive : RustyWiresDfirNode
    {
        public MutatingUnaryPrimitive(Node parentNode, UnaryPrimitiveOps operation) : base(parentNode)
        {
            NIType intMutableReferenceType = PFTypes.Int32.CreateMutableReference();
            CreateTerminal(Direction.Input, intMutableReferenceType, "x in");
            CreateTerminal(Direction.Output, intMutableReferenceType, "x out");
        }

        private MutatingUnaryPrimitive(Node parentNode, MutatingUnaryPrimitive nodeToCopy, NodeCopyInfo nodeCopyInfo)
            : base(parentNode, nodeToCopy, nodeCopyInfo)
        {
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new MutatingUnaryPrimitive(newParentNode, this, copyInfo);
        }

        public UnaryPrimitiveOps Operation { get; }

        /// <inheritdoc />
        public override T AcceptVisitor<T>(IRustyWiresDfirNodeVisitor<T> visitor)
        {
            return visitor.VisitMutatingUnaryPrimitive(this);
        }
    }
}
