using System;
using System.Linq;
using NationalInstruments;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using RustyWires.Compiler.Nodes;

namespace RustyWires.Compiler
{
    /// <summary>
    /// Checks that all <see cref="Variable"/> usages associated with input terminals on each node are correct.
    /// Can assume that all <see cref="Variable"/>s associated with input terminals have initial types and lifetimes set.
    /// </summary>
    internal class ValidateVariableUsagesTransform : VisitorTransformBase, IRustyWiresDfirNodeVisitor<bool>
    {
        protected override void VisitNode(Node node)
        {
            this.VisitRustyWiresNode(node);
        }

        protected override void VisitWire(Wire wire)
        {
            Variable sourceVariable = wire.SourceTerminal.GetVariable();
            if (wire.SinkTerminals.HasMoreThan(1) && sourceVariable != null && !WireTypeMayFork(sourceVariable.Type))
            {
                wire.SetDfirMessage(RustyWiresMessages.WireCannotFork);
            }
        }

        private bool WireTypeMayFork(NIType wireType)
        {
            if (wireType.IsMutableValueType() || wireType.IsImmutableValueType())
            {
                return CanShallowCopyDataType(wireType.GetUnderlyingTypeFromRustyWiresType());
            }

            if (wireType.IsMutableReferenceType())
            {
                return false;
            }

            if (wireType.IsImmutableReferenceType())
            {
                return true;
            }

            return false;
        }

        private bool CanShallowCopyDataType(NIType dataType)
        {
            return dataType.IsNumeric();
        }

        protected override void VisitBorderNode(BorderNode borderNode)
        {
            this.VisitRustyWiresNode(borderNode);
        }

        public bool VisitBorrowTunnel(BorrowTunnel borrowTunnel)
        {
            var validator = borrowTunnel.Terminals[0].GetValidator();
            if (borrowTunnel.BorrowMode == Common.BorrowMode.Mutable)
            {
                validator.TestVariableIsMutableType();
            }
            return true;
        }

        public bool VisitConstant(Constant constant)
        {
            // This node has no inputs.
            return true;
        }

        public bool VisitCreateCellNode(CreateCellNode createCellNode)
        {
            VariableUsageValidator validator = createCellNode.Terminals[0].GetValidator();
            validator.TestVariableIsOwnedType();
            return true;
        }

        public bool VisitCreateMutableCopyNode(CreateMutableCopyNode createMutableCopyNode)
        {
            VariableUsageValidator validator = createMutableCopyNode.InputTerminals.ElementAt(0).GetValidator();
            // TODO: check that the underlying type can be copied
            return true;
        }

        public bool VisitDropNode(DropNode dropNode)
        {
            VariableUsageValidator validator = dropNode.Terminals[0].GetValidator();
            validator.TestVariableIsOwnedType();
            return true;
        }

        public bool VisitExchangeValuesNode(ExchangeValuesNode exchangeValuesNode)
        {
            VariableUsageValidator validator1 = exchangeValuesNode.Terminals[0].GetValidator();
            validator1.TestVariableIsMutableType();
            VariableUsageValidator validator2 = exchangeValuesNode.Terminals[1].GetValidator();
            validator2.TestVariableIsMutableType();
            // TODO: ensure that lifetimes of exchanged values and references are compatible
            return true;
        }

        public bool VisitExplicitBorrowNode(ExplicitBorrowNode explicitBorrowNode)
        {
            VariableUsageValidator validator = explicitBorrowNode.Terminals[0].GetValidator();
            return true;
        }

        public bool VisitFreezeNode(FreezeNode freezeNode)
        {
            VariableUsageValidator validator = freezeNode.Terminals[0].GetValidator();
            validator.TestVariableIsOwnedType();
            validator.TestVariableIsMutableType();
            return true;
        }

        public bool VisitImmutablePassthroughNode(ImmutablePassthroughNode immutablePassthroughNode)
        {
            VariableUsageValidator validator = immutablePassthroughNode.InputTerminals.ElementAt(0).GetValidator();
            return true;
        }

        public bool VisitLockTunnel(LockTunnel lockTunnel)
        {
            VariableUsageValidator validator = lockTunnel.Terminals[0].GetValidator();
            // TODO: report error if variable type !.IsLockingCellType()
            return true;
        }

        public bool VisitMutablePassthroughNode(MutablePassthroughNode mutablePassthroughNode)
        {
            VariableUsageValidator validator = mutablePassthroughNode.Terminals[0].GetValidator();
            validator.TestVariableIsMutableType();
            return true;
        }

        public bool VisitMutatingBinaryPrimitive(MutatingBinaryPrimitive mutatingBinaryPrimitive)
        {
            VariableUsageValidator validator1 = mutatingBinaryPrimitive.Terminals[0].GetValidator();
            validator1.TestExpectedUnderlyingType(PFTypes.Int32);
            VariableUsageValidator validator2 = mutatingBinaryPrimitive.Terminals[1].GetValidator();
            validator2.TestExpectedUnderlyingType(PFTypes.Int32);
            return true;
        }

        public bool VisitMutatingUnaryPrimitive(MutatingUnaryPrimitive mutatingUnaryPrimitive)
        {
            VariableUsageValidator validator = mutatingUnaryPrimitive.Terminals[0].GetValidator();
            validator.TestExpectedUnderlyingType(PFTypes.Int32);
            return true;
        }

        public bool VisitPureBinaryPrimitive(PureBinaryPrimitive pureBinaryPrimitive)
        {
            VariableUsageValidator validator1 = pureBinaryPrimitive.Terminals[0].GetValidator();
            validator1.TestExpectedUnderlyingType(PFTypes.Int32);
            VariableUsageValidator validator2 = pureBinaryPrimitive.Terminals[1].GetValidator();
            validator2.TestExpectedUnderlyingType(PFTypes.Int32);
            return true;
        }

        public bool VisitPureUnaryPrimitive(PureUnaryPrimitive pureUnaryPrimitive)
        {
            VariableUsageValidator validator = pureUnaryPrimitive.Terminals[0].GetValidator();
            validator.TestExpectedUnderlyingType(PFTypes.Int32);
            return true;
        }

        public bool VisitSelectReferenceNode(SelectReferenceNode selectReferenceNode)
        {
            VariableUsageValidator validator1 = selectReferenceNode.Terminals[0].GetValidator();
            VariableUsageValidator validator2 = selectReferenceNode.Terminals[1].GetValidator();
            return true;
        }

        public bool VisitTerminateLifetimeNode(TerminateLifetimeNode terminateLifetimeNode)
        {
            foreach (var inputTerminal in terminateLifetimeNode.InputTerminals)
            {
                VariableUsageValidator validator = new VariableUsageValidator(inputTerminal.GetVariable(), inputTerminal, false);
            }

            switch (terminateLifetimeNode.ErrorState)
            {
                case TerminateLifetimeErrorState.InputLifetimesNotUnique:
                    terminateLifetimeNode.SetDfirMessage(RustyWiresMessages.TerminateLifetimeInputLifetimesNotUnique);
                    break;
                case TerminateLifetimeErrorState.InputLifetimeCannotBeTerminated:
                    terminateLifetimeNode.SetDfirMessage(RustyWiresMessages.TerminateLifetimeInputLifetimeCannotBeTerminated);
                    break;
                case TerminateLifetimeErrorState.NotAllVariablesInLifetimeConnected:
                    terminateLifetimeNode.SetDfirMessage(RustyWiresMessages.TerminateLifetimeNotAllVariablesInLifetimeConnected);
                    break;
            }
            return true;
        }

        public bool VisitTunnel(Tunnel tunnel)
        {
            return true;
        }

        public bool VisitUnborrowTunnel(UnborrowTunnel unborrowTunnel)
        {
            // This node has no inputs.
            return true;
        }

        public bool VisitUnlockTunnel(UnlockTunnel unlockTunnel)
        {
            // This node has no inputs.
            return true;
        }
    }
}
