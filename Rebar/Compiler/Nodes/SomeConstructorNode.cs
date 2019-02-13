using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;

namespace Rebar.Compiler.Nodes
{
    /// <summary>
    /// DFIR representation for the <see cref="SourceModel.SomeConstructorNode"/>.
    /// </summary>
    internal class SomeConstructorNode : DfirNode
    {
        public SomeConstructorNode(Node parentNode) : base(parentNode)
        {
            CreateTerminal(Direction.Input, PFTypes.Void, "value in");
            CreateTerminal(Direction.Output, PFTypes.Void.CreateOption(), "Some value out");
        }

        private SomeConstructorNode(Node parentNode, SomeConstructorNode nodeToCopy, NodeCopyInfo nodeCopyInfo)
            : base(parentNode, nodeToCopy, nodeCopyInfo)
        {
        }

        /// <inheritdoc />
        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new SomeConstructorNode(newParentNode, this, copyInfo);
        }

        /// <inheritdoc />
        public override T AcceptVisitor<T>(IDfirNodeVisitor<T> visitor)
        {
            return visitor.VisitSomeConstructorNode(this);
        }
    }
}
