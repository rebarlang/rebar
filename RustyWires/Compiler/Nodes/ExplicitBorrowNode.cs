using System.Collections.Generic;
using System.Linq;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

namespace RustyWires.Compiler.Nodes
{
    internal class ExplicitBorrowNode : RustyWiresDfirNode
    {
        public ExplicitBorrowNode(Node parentNode, BorrowMode borrowMode) : base(parentNode)
        {
            BorrowMode = borrowMode;
            NIType inputType, outputType;
            switch (borrowMode)
            {
                case BorrowMode.OwnerToMutable:
                    inputType = PFTypes.Void;
                    outputType = PFTypes.Void.CreateMutableReference();
                    break;
                case BorrowMode.OwnerToImmutable:
                    inputType = PFTypes.Void;
                    outputType = PFTypes.Void.CreateImmutableReference();
                    break;
                default:
                    inputType = PFTypes.Void.CreateMutableReference();
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
        }

        public BorrowMode BorrowMode { get; }

        public Terminal InputTerminal { get; }

        public Terminal OutputTerminal { get; }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new ExplicitBorrowNode(newParentNode, this, copyInfo);
        }

        /// <inheritdoc />
        public override IEnumerable<PassthroughTerminalPair> PassthroughTerminalPairs => Enumerable.Empty<PassthroughTerminalPair>();

        /// <inheritdoc />
        public override void SetOutputVariableTypesAndLifetimes()
        {
            VariableSet variableSet = DfirRoot.GetVariableSet();
            Terminal inputTerminal = Terminals.ElementAt(0);
            Terminal outputTerminal = Terminals.ElementAt(1);
            Variable inputVariable = variableSet.GetVariableForTerminal(inputTerminal);
            NIType outputUnderlyingType = inputVariable.GetUnderlyingTypeOrVoid();
            NIType outputType = BorrowMode == BorrowMode.OwnerToImmutable
                ? outputUnderlyingType.CreateImmutableReference()
                : outputUnderlyingType.CreateMutableReference();
            Variable outputVariable = variableSet.GetVariableForTerminal(outputTerminal);
            outputVariable?.SetType(outputType);

            LifetimeSet lifetimeSet = DfirRoot.GetLifetimeSet();
            Lifetime sourceLifetime = inputVariable?.Lifetime ?? lifetimeSet.EmptyLifetime;
            Lifetime outputLifetime = lifetimeSet.DefineLifetime(
                LifetimeCategory.Node,
                this,
                sourceLifetime.IsEmpty ? null : sourceLifetime);
            outputVariable?.SetLifetime(outputLifetime);
        }

        /// <inheritdoc />
        public override void CheckVariableUsages()
        {
            VariableUsageValidator validator = DfirRoot.GetVariableSet().GetValidatorForTerminal(Terminals[0]);
        }
    }

    internal enum BorrowMode
    {
        OwnerToMutable,
        OwnerToImmutable,
        MutableToImmutable
    }
}
