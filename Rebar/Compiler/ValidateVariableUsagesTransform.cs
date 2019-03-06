﻿using System.Linq;
using NationalInstruments;
using NationalInstruments.Compiler.SemanticAnalysis;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler.Nodes;

namespace Rebar.Compiler
{
    /// <summary>
    /// Checks that all <see cref="Variable"/> usages associated with input terminals on each node are correct.
    /// Can assume that all <see cref="Variable"/>s associated with input terminals have initial types and lifetimes set.
    /// </summary>
    internal class ValidateVariableUsagesTransform : VisitorTransformBase, IDfirNodeVisitor<bool>
    {
        protected override void VisitNode(Node node)
        {
            this.VisitRebarNode(node);
        }

        protected override void VisitWire(Wire wire)
        {
            Terminal sourceTerminal;
            if (wire.TryGetSourceTerminal(out sourceTerminal))
            {
                Variable sourceVariable = sourceTerminal.GetVariable();
                if (wire.SinkTerminals.HasMoreThan(1) && sourceVariable != null && !WireTypeMayFork(sourceVariable.Type))
                {
                    wire.SetDfirMessage(Messages.WireCannotFork);
                }
                if (wire.Terminals.Any(t => !t.IsConnected))
                {
                    wire.SetDfirMessage(WireSpecificUserMessages.LooseEnds);
                }
            }
            else
            {
                wire.SetDfirMessage(WireSpecificUserMessages.NoSource);
            }
        }

        internal static bool WireTypeMayFork(NIType wireType)
        {
            if (wireType.IsImmutableReferenceType())
            {
                return true;
            }

            if (wireType.IsNumeric() || wireType.IsBoolean())
            {
                return true;
            }

            NIType optionValueType;
            if (wireType.TryDestructureOptionType(out optionValueType))
            {
                return WireTypeMayFork(optionValueType);
            }

            return false;
        }

        protected override void VisitBorderNode(NationalInstruments.Dfir.BorderNode borderNode)
        {
            this.VisitRebarNode(borderNode);
        }

        public bool VisitAssignNode(AssignNode assignNode)
        {
            VariableUsageValidator assigneeValidator = assignNode.InputTerminals[0].GetValidator();
            assigneeValidator.TestVariableIsMutableType();
            VariableUsageValidator valueValidator = assignNode.InputTerminals[1].GetValidator();
            valueValidator.TestVariableIsOwnedType();
            assigneeValidator.TestSameUnderlyingTypeAs(valueValidator);
            return true;
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

        public bool VisitCreateCopyNode(CreateCopyNode createCopyNode)
        {
            VariableUsageValidator validator = createCopyNode.InputTerminals.ElementAt(0).GetValidator();
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

        public bool VisitImmutablePassthroughNode(ImmutablePassthroughNode immutablePassthroughNode)
        {
            VariableUsageValidator validator = immutablePassthroughNode.InputTerminals.ElementAt(0).GetValidator();
            return true;
        }

        public bool VisitIterateTunnel(IterateTunnel iterateTunnel)
        {
            VariableUsageValidator validator = iterateTunnel.Terminals[0].GetValidator();
            validator.TestUnderlyingType(
                type => type.IsIteratorType() || type.IsVectorType(),
                PFTypes.Void.CreateIterator());

            NIType underlyingType = iterateTunnel.Terminals[0].GetVariable().GetTypeOrVoid().GetUnderlyingTypeFromRebarType();
            if (underlyingType.IsIteratorType())
            {
                validator.TestVariableIsMutableType();
            }
            return true;
        }

        public bool VisitLockTunnel(LockTunnel lockTunnel)
        {
            VariableUsageValidator validator = lockTunnel.Terminals[0].GetValidator();
            validator.TestUnderlyingType(DataTypes.IsLockingCellType, PFTypes.Void.CreateLockingCell());
            return true;
        }

        public bool VisitLoopConditionTunnel(LoopConditionTunnel loopConditionTunnel)
        {
            Terminal inputTerminal = loopConditionTunnel.InputTerminals.ElementAt(0);
            var validator = new VariableUsageValidator(inputTerminal.GetVariable(), inputTerminal, true, false);
            validator.TestVariableIsOwnedType();
            validator.TestExpectedUnderlyingType(PFTypes.Boolean);
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
            NIType expectedInputUnderlyingType = mutatingBinaryPrimitive.Operation.GetExpectedInputType();
            VariableUsageValidator validator1 = mutatingBinaryPrimitive.Terminals[0].GetValidator();
            validator1.TestExpectedUnderlyingType(expectedInputUnderlyingType);
            validator1.TestVariableIsMutableType();
            VariableUsageValidator validator2 = mutatingBinaryPrimitive.Terminals[1].GetValidator();
            validator2.TestExpectedUnderlyingType(expectedInputUnderlyingType);
            return true;
        }

        public bool VisitMutatingUnaryPrimitive(MutatingUnaryPrimitive mutatingUnaryPrimitive)
        {
            NIType expectedInputUnderlyingType = mutatingUnaryPrimitive.Operation.GetExpectedInputType();
            VariableUsageValidator validator = mutatingUnaryPrimitive.Terminals[0].GetValidator();
            validator.TestExpectedUnderlyingType(expectedInputUnderlyingType);
            validator.TestVariableIsMutableType();
            return true;
        }

        public bool VisitOutputNode(OutputNode outputNode)
        {
            VariableUsageValidator validator = outputNode.Terminals[0].GetValidator();
            // For now, only outputing i32 is supported
            validator.TestExpectedUnderlyingType(PFTypes.Int32);
            if (!RebarFeatureToggles.IsOutputNodeEnabled)
            {
                outputNode.SetDfirMessage(Messages.FeatureNotEnabled);
            }
            return true;
        }

        public bool VisitPureBinaryPrimitive(PureBinaryPrimitive pureBinaryPrimitive)
        {
            NIType expectedInputUnderlyingType = pureBinaryPrimitive.Operation.GetExpectedInputType();
            VariableUsageValidator validator1 = pureBinaryPrimitive.Terminals[0].GetValidator();
            validator1.TestExpectedUnderlyingType(expectedInputUnderlyingType);
            VariableUsageValidator validator2 = pureBinaryPrimitive.Terminals[1].GetValidator();
            validator2.TestExpectedUnderlyingType(expectedInputUnderlyingType);
            return true;
        }

        public bool VisitPureUnaryPrimitive(PureUnaryPrimitive pureUnaryPrimitive)
        {
            NIType expectedInputUnderlyingType = pureUnaryPrimitive.Operation.GetExpectedInputType();
            VariableUsageValidator validator = pureUnaryPrimitive.Terminals[0].GetValidator();
            validator.TestExpectedUnderlyingType(expectedInputUnderlyingType);
            return true;
        }

        public bool VisitRangeNode(RangeNode rangeNode)
        {
            VariableUsageValidator validator = rangeNode.Terminals[0].GetValidator();
            validator.TestExpectedUnderlyingType(PFTypes.Int32);
            validator.TestVariableIsOwnedType();
            validator = rangeNode.Terminals[1].GetValidator();
            validator.TestExpectedUnderlyingType(PFTypes.Int32);
            validator.TestVariableIsOwnedType();
            return true;
        }

        public bool VisitSelectReferenceNode(SelectReferenceNode selectReferenceNode)
        {
            VariableUsageValidator validator1 = selectReferenceNode.Terminals[0].GetValidator();
            VariableUsageValidator validator2 = selectReferenceNode.Terminals[1].GetValidator();
            validator2.TestSameUnderlyingTypeAs(validator1);
            VariableUsageValidator selectorValidator = selectReferenceNode.Terminals[2].GetValidator();
            selectorValidator.TestExpectedUnderlyingType(PFTypes.Boolean);
            return true;
        }

        public bool VisitSomeConstructorNode(SomeConstructorNode someConstructorNode)
        {
            VariableUsageValidator validator = someConstructorNode.Terminals[0].GetValidator();
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
                    terminateLifetimeNode.SetDfirMessage(Messages.TerminateLifetimeInputLifetimesNotUnique);
                    break;
                case TerminateLifetimeErrorState.InputLifetimeCannotBeTerminated:
                    terminateLifetimeNode.SetDfirMessage(Messages.TerminateLifetimeInputLifetimeCannotBeTerminated);
                    break;
                case TerminateLifetimeErrorState.NotAllVariablesInLifetimeConnected:
                    terminateLifetimeNode.SetDfirMessage(Messages.TerminateLifetimeNotAllVariablesInLifetimeConnected);
                    break;
            }
            return true;
        }

        public bool VisitTunnel(Tunnel tunnel)
        {
            return true;
        }

        public bool VisitTerminateLifetimeTunnel(TerminateLifetimeTunnel unborrowTunnel)
        {
            // This node has no inputs.
            return true;
        }

        public bool VisitUnwrapOptionTunnel(UnwrapOptionTunnel unwrapOptionTunnel)
        {
            if (unwrapOptionTunnel.Direction != Direction.Input)
            {
                // TODO: report an error; this tunnel can only be an input
                return true;
            }
            VariableUsageValidator validator = unwrapOptionTunnel.GetOuterTerminal(0).GetValidator();
            validator.TestVariableIsOwnedType();
            validator.TestUnderlyingType(t => t.IsOptionType(), PFTypes.Void.CreateOption());
            return true;
        }

        public bool VisitVectorCreateNode(VectorCreateNode vectorCreateNode)
        {
            if (!RebarFeatureToggles.IsVectorAndSliceTypesEnabled)
            {
                vectorCreateNode.SetDfirMessage(Messages.FeatureNotEnabled);
            }
            return true;
        }
    }
}