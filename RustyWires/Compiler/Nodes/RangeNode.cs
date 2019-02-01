using NationalInstruments.Dfir;
using NationalInstruments.DataTypes;

namespace RustyWires.Compiler.Nodes
{
    /// <summary>
    /// DFIR representation of <see cref="SourceModel.Range"/>.
    /// </summary>
    internal class RangeNode : RustyWiresDfirNode
    {
        public RangeNode(Node parentNode) : base(parentNode)
        {
            CreateTerminal(Direction.Input, PFTypes.Int32, "lower bound");
            CreateTerminal(Direction.Input, PFTypes.Int32, "upper bound");
            CreateTerminal(Direction.Output, PFTypes.Int32.CreateIterator(), "iterator");
        }

        private RangeNode(Node parentNode, RangeNode nodeToCopy, NodeCopyInfo nodeCopyInfo)
            : base(parentNode, nodeToCopy, nodeCopyInfo)
        {
        }

        /// <inheritdoc />
        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new RangeNode(newParentNode, this, copyInfo);
        }

        /// <inheritdoc />
        public override T AcceptVisitor<T>(IRustyWiresDfirNodeVisitor<T> visitor)
        {
            return visitor.VisitRangeNode(this);
        }
    }
}
