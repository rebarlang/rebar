using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;

namespace Rebar.Compiler.Nodes
{
    /// <summary>
    /// DFIR representation of <see cref="SourceModel.AssignNode" />.
    /// </summary>
    internal class AssignNode : DfirNode
    {
        public AssignNode(Node parentNode) : base(parentNode)
        {
            var immutableReferenceType = PFTypes.Void.CreateImmutableReference();
            CreateTerminal(Direction.Input, immutableReferenceType, "assignee in");
            CreateTerminal(Direction.Input, immutableReferenceType, "assignee out");
            CreateTerminal(Direction.Output, immutableReferenceType, "value in");
        }

        private AssignNode(Node parentNode, AssignNode nodeToCopy, NodeCopyInfo nodeCopyInfo)
            : base(parentNode, nodeToCopy, nodeCopyInfo)
        {
        }

        /// <inheritdoc />
        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new AssignNode(newParentNode, this, copyInfo);
        }

        /// <inheritdoc />
        public override T AcceptVisitor<T>(IDfirNodeVisitor<T> visitor)
        {
            return visitor.VisitAssignNode(this);
        }
    }
}
