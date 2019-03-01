using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;

namespace Rebar.Compiler.Nodes
{
    internal class MutatingBinaryPrimitive : DfirNode
    {
        public MutatingBinaryPrimitive(Node parentNode, BinaryPrimitiveOps operation) : base(parentNode)
        {
            Operation = operation;
            NIType inputType = operation.GetExpectedInputType();
            NIType inputMutableReferenceType = inputType.CreateMutableReference();
            NIType inputImmutableReferenceType = inputType.CreateImmutableReference();
            CreateTerminal(Direction.Input, inputMutableReferenceType, "x in");
            CreateTerminal(Direction.Input, inputImmutableReferenceType, "y in");
            CreateTerminal(Direction.Output, inputMutableReferenceType, "x out");
            CreateTerminal(Direction.Output, inputImmutableReferenceType, "y out");
        }

        private MutatingBinaryPrimitive(Node parentNode, MutatingBinaryPrimitive nodeToCopy, NodeCopyInfo nodeCopyInfo)
            : base(parentNode, nodeToCopy, nodeCopyInfo)
        {
            Operation = nodeToCopy.Operation;
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new MutatingBinaryPrimitive(newParentNode, this, copyInfo);
        }

        public BinaryPrimitiveOps Operation { get; }

        /// <inheritdoc />
        public override T AcceptVisitor<T>(IDfirNodeVisitor<T> visitor)
        {
            return visitor.VisitMutatingBinaryPrimitive(this);
        }
    }
}
