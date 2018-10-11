using System.Collections.Generic;
using System.Linq;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

namespace RustyWires.Compiler
{
    internal class ExplicitUnborrowNode : RustyWiresDfirNode
    {
        public ExplicitUnborrowNode(Node parentNode, BorrowMode borrowMode) : base(parentNode)
        {
            BorrowMode = borrowMode;
            NIType inputType, outputType;
            switch (borrowMode)
            {
                case BorrowMode.OwnerToMutable:
                    inputType = PFTypes.Void.CreateMutableReference();
                    outputType = PFTypes.Void;
                    break;
                case BorrowMode.OwnerToImmutable:
                    inputType = PFTypes.Void.CreateImmutableReference();
                    outputType = PFTypes.Void;
                    break;
                default:
                    inputType = PFTypes.Void.CreateImmutableReference();
                    outputType = PFTypes.Void.CreateMutableReference();
                    break;
            }
            InputTerminal = CreateTerminal(Direction.Input, inputType, "in");
            OutputTerminal = CreateTerminal(Direction.Output, outputType, "out");
        }

        public BorrowMode BorrowMode { get; }

        public Terminal InputTerminal { get; }

        public Terminal OutputTerminal { get; }

        /// <inheritdoc />
        public override IEnumerable<PassthroughTerminalPair> PassthroughTerminalPairs => Enumerable.Empty<PassthroughTerminalPair>();

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new ExplicitUnborrowNode(newParentNode, BorrowMode);
        }
    }
}
