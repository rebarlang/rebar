using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;

namespace Rebar.Compiler.Nodes
{
    internal class ExplicitBorrowNode : DfirNode
    {
        public ExplicitBorrowNode(Node parentNode, BorrowMode borrowMode) : base(parentNode)
        {
            BorrowMode = borrowMode;
            NIType inputType = PFTypes.Void, outputType;
            switch (borrowMode)
            {
                case BorrowMode.Mutable:
                    outputType = PFTypes.Void.CreateMutableReference();
                    break;
                default:
                    outputType = PFTypes.Void.CreateImmutableReference();
                    break;
            }
            InputTerminal = CreateTerminal(Direction.Input, inputType, "in");
            OutputTerminal = CreateTerminal(Direction.Output, outputType, "out");
        }

        private ExplicitBorrowNode(Node parentNode, ExplicitBorrowNode copyFrom, NodeCopyInfo copyInfo)
            : base(parentNode, copyFrom, copyInfo)
        {
            BorrowMode = copyFrom.BorrowMode;
            InputTerminal = InputTerminals.First();
            OutputTerminal = OutputTerminals.First();
        }

        public BorrowMode BorrowMode { get; }

        public Terminal InputTerminal { get; }

        public Terminal OutputTerminal { get; }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new ExplicitBorrowNode(newParentNode, this, copyInfo);
        }

        /// <inheritdoc />
        public override T AcceptVisitor<T>(IDfirNodeVisitor<T> visitor)
        {
            return visitor.VisitExplicitBorrowNode(this);
        }
    }
}
