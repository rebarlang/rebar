using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;

namespace Rebar.Compiler.Nodes
{
    /// <summary>
    /// DFIR representation of the <see cref="SourceModel.VectorInsert"/> node.
    /// </summary>
    internal class VectorInsertNode : DfirNode
    {
        public VectorInsertNode(Node parentNode)
            : base(parentNode)
        {
            NIType vectorType = PFTypes.Int32.CreateVector();
            CreateTerminal(Direction.Input, vectorType.CreateMutableReference(), "vector in");
            CreateTerminal(Direction.Input, PFTypes.Int32.CreateImmutableReference(), "index in");
            CreateTerminal(Direction.Input, PFTypes.Int32, "element in");
            CreateTerminal(Direction.Output, vectorType.CreateMutableReference(), "vector out");
            CreateTerminal(Direction.Output, PFTypes.Int32.CreateImmutableReference(), "index out");
        }

        private VectorInsertNode(Node parentNode, VectorInsertNode nodeToCopy, NodeCopyInfo nodeCopyInfo)
            : base(parentNode, nodeToCopy, nodeCopyInfo)
        {
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new VectorInsertNode(newParentNode, this, copyInfo);
        }

        public override T AcceptVisitor<T>(IDfirNodeVisitor<T> visitor)
        {
            return visitor.VisitVectorInsertNode(this);
        }
    }
}
