using NationalInstruments.CommonModel;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

namespace Rebar.Compiler.Nodes
{
    internal class DecomposeTupleNode : DfirNode
    {
        public DecomposeTupleNode(Diagram parent, int outputCount, DecomposeMode decomposeMode)
            : base(parent)
        {
            DecomposeMode = decomposeMode;
            CreateTerminal(Direction.Input, NITypes.Void, "in");
            for (int i = 0; i < outputCount; ++i)
            {
                CreateTerminal(Direction.Output, NITypes.Void, $"out_{i}");
            }
        }

        private DecomposeTupleNode(Node parentNode, DecomposeTupleNode copyFrom, NodeCopyInfo copyInfo)
            : base(parentNode, copyFrom, copyInfo)
        {
            DecomposeMode = copyFrom.DecomposeMode;
        }

        public DecomposeMode DecomposeMode { get; }

        public override T AcceptVisitor<T>(IDfirNodeVisitor<T> visitor)
        {
            return visitor.VisitDecomposeTupleNode(this);
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new DecomposeTupleNode(newParentNode, this, copyInfo);
        }
    }

    internal enum DecomposeMode
    {
        Move,

        Borrow,
    }
}
