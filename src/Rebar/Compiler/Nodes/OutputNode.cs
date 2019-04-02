using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;

namespace Rebar.Compiler.Nodes
{
    /// <summary>
    /// DFIR representation for the <see cref="SourceModel.Output"/> node.
    /// </summary>
    internal class OutputNode : DfirNode
    {
        public OutputNode(Node parentNode)
            : base(parentNode)
        {
            var immutableReferenceType = PFTypes.Void.CreateImmutableReference();
            CreateTerminal(Direction.Input, immutableReferenceType, "value in");
            CreateTerminal(Direction.Output, immutableReferenceType, "value out");
        }

        private OutputNode(Node newParentNode, OutputNode nodeToCopy, NodeCopyInfo copyInfo)
            : base(newParentNode, nodeToCopy, copyInfo)
        {
        }

        /// <inheritdoc />
        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new OutputNode(newParentNode, this, copyInfo);
        }

        /// <inheritdoc />
        public override T AcceptVisitor<T>(IDfirNodeVisitor<T> visitor)
        {
            return visitor.VisitOutputNode(this);
        }
    }
}
