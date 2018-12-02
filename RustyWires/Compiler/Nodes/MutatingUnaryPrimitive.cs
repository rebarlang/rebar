﻿using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using RustyWires.Common;

namespace RustyWires.Compiler.Nodes
{
    internal class MutatingUnaryPrimitive : RustyWiresDfirNode
    {
        public MutatingUnaryPrimitive(Node parentNode, UnaryPrimitiveOps operation) : base(parentNode)
        {
            Operation = operation;
            NIType inputType = operation.GetExpectedInputType();
            NIType inputMutableReferenceType = inputType.CreateMutableReference();
            CreateTerminal(Direction.Input, inputMutableReferenceType, "x in");
            CreateTerminal(Direction.Output, inputMutableReferenceType, "x out");
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
