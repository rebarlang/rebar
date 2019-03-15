using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;

namespace Rebar.Compiler.Nodes
{
    internal class SelectReferenceNode : DfirNode
    {
        public SelectReferenceNode(Node parentNode) : base(parentNode)
        {
            var immutableReferenceType = PFTypes.Void.CreateImmutableReference();
            var booleanImmutableReferenceType = PFTypes.Boolean.CreateImmutableReference();
            CreateTerminal(Direction.Input, booleanImmutableReferenceType, "selector in");
            CreateTerminal(Direction.Input, immutableReferenceType, "ref in 1");
            CreateTerminal(Direction.Input, immutableReferenceType, "ref in 2");
            CreateTerminal(Direction.Output, booleanImmutableReferenceType, "selector out");
            CreateTerminal(Direction.Output, immutableReferenceType, "selected ref out");
        }

        private SelectReferenceNode(Node parentNode, SelectReferenceNode nodeToCopy, NodeCopyInfo nodeCopyInfo)
            : base(parentNode, nodeToCopy, nodeCopyInfo)
        {
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new SelectReferenceNode(newParentNode, this, copyInfo);
        }

        /// <inheritdoc />
        public override T AcceptVisitor<T>(IDfirNodeVisitor<T> visitor)
        {
            return visitor.VisitSelectReferenceNode(this);
        }
    }
}
