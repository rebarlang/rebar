using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;

namespace Rebar.Compiler.Nodes
{
    internal class VectorCreateNode : DfirNode
    {
        public VectorCreateNode(Node parentNode) : base(parentNode)
        {
            CreateTerminal(Direction.Output, PFTypes.Int32.CreateVector(), "vector");
        }

        private VectorCreateNode(Node parentNode, VectorCreateNode nodeToCopy, NodeCopyInfo nodeCopyInfo)
            : base(parentNode, nodeToCopy, nodeCopyInfo)
        {
        }

        /// <inheritdoc />
        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new VectorCreateNode(newParentNode, this, copyInfo);
        }

        /// <inheritdoc />
        public override T AcceptVisitor<T>(IDfirNodeVisitor<T> visitor)
        {
            return visitor.VisitVectorCreateNode(this);
        }
    }
}
