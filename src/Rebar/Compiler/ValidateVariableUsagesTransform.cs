using System.Linq;
using NationalInstruments;
using NationalInstruments.Compiler.SemanticAnalysis;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using NationalInstruments.FeatureToggles;
using Rebar.Common;
using Rebar.Compiler.Nodes;

namespace Rebar.Compiler
{
    /// <summary>
    /// Checks that all <see cref="VariableReference"/> usages associated with input terminals on each node are correct.
    /// Can assume that all <see cref="VariableReference"/>s associated with input terminals have initial types and lifetimes set.
    /// </summary>
    internal class ValidateVariableUsagesTransform : VisitorTransformBase, IDfirNodeVisitor<bool>
    {
        private readonly TerminalTypeUnificationResults _typeUnificationResults;

        public ValidateVariableUsagesTransform(TerminalTypeUnificationResults typeUnificationResults)
        {
            _typeUnificationResults = typeUnificationResults;
        }

        protected override void VisitNode(Node node)
        {
            this.VisitRebarNode(node);
        }

        bool IDfirNodeVisitor<bool>.VisitWire(Wire wire)
        {
            VisitWire(wire);
            return true;
        }

        protected override void VisitWire(Wire wire)
        {
            Terminal sourceTerminal;
            if (wire.TryGetSourceTerminal(out sourceTerminal))
            {
                _typeUnificationResults.SetMessagesOnTerminal(sourceTerminal);
                VariableReference sourceVariable = sourceTerminal.GetFacadeVariable();
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

        protected override void VisitBorderNode(NationalInstruments.Dfir.BorderNode borderNode)
        {
            this.VisitRebarNode(borderNode);
        }

        public bool VisitBorrowTunnel(BorrowTunnel borrowTunnel)
        {
            var validator = borrowTunnel.Terminals[0].GetValidator();
            if (borrowTunnel.BorrowMode == BorrowMode.Mutable)
            {
                validator.TestVariableIsMutableType();
            }
            return true;
        }

        public bool VisitBuildTupleNode(BuildTupleNode buildTupleNode)
        {
            buildTupleNode.InputTerminals.ForEach(ValidateRequiredInputTerminal);
            return true;
        }

        public bool VisitConstant(Constant constant)
        {
            // This node has no inputs.
            if (constant.DataType.IsInteger() && !constant.DataType.IsSupportedIntegerType())
            {
                constant.SetDfirMessage(Messages.FeatureNotEnabled);
            }
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitDataAccessor(DataAccessor dataAccessor)
        {
            if (dataAccessor.DataItem.ConnectorPaneIndex == -1)
            {
                // TODO: possibly move this error to a different transform
                dataAccessor.SetDfirMessage(Messages.ParameterNotOnConnectorPane);
            }
            if (dataAccessor.Terminal.Direction == Direction.Input)
            {
                ValidateRequiredInputTerminal(dataAccessor.Terminal);
                // TODO: eventually we want to allow reference types
                dataAccessor.Terminal.GetValidator().TestVariableIsOwnedType();
            }
            if (!RebarFeatureToggles.IsParametersAndCallsEnabled)
            {
                dataAccessor.SetDfirMessage(Messages.FeatureNotEnabled);
            }
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitDecomposeTupleNode(DecomposeTupleNode decomposeTupleNode)
        {
            ValidateRequiredInputTerminal(decomposeTupleNode.InputTerminals[0]);
            return true;
        }

        public bool VisitDropNode(DropNode dropNode)
        {
            VariableUsageValidator validator = dropNode.Terminals[0].GetValidator();
            validator.TestVariableIsOwnedType();
            return true;
        }

        public bool VisitExplicitBorrowNode(ExplicitBorrowNode explicitBorrowNode)
        {
            foreach (var inputTerminal in explicitBorrowNode.InputTerminals)
            {
                VariableUsageValidator validator = inputTerminal.GetValidator();
            }
            return true;
        }

        public bool VisitFunctionalNode(FunctionalNode functionalNode)
        {
            VisitFunctionSignatureNode(functionalNode, functionalNode.Signature);
            if (functionalNode.RequiredFeatureToggles.Any(feature => !FeatureToggleSupport.IsFeatureEnabled(feature)))
            {
                functionalNode.SetDfirMessage(Messages.FeatureNotEnabled);
            }
            return true;
        }

        public bool VisitIterateTunnel(IterateTunnel iterateTunnel)
        {
            ValidateRequiredInputTerminal(iterateTunnel.InputTerminals[0]);
            return true;
        }

        public bool VisitLockTunnel(LockTunnel lockTunnel)
        {
            ValidateRequiredInputTerminal(lockTunnel.InputTerminals[0]);
            return true;
        }

        public bool VisitLoopConditionTunnel(LoopConditionTunnel loopConditionTunnel)
        {
            ValidateOptionalInputTerminal(loopConditionTunnel.InputTerminals[0]);
            return true;
        }

        public bool VisitMethodCallNode(MethodCallNode methodCallNode)
        {
            VisitFunctionSignatureNode(methodCallNode, methodCallNode.Signature);
            if (!RebarFeatureToggles.IsParametersAndCallsEnabled)
            {
                methodCallNode.SetDfirMessage(Messages.FeatureNotEnabled);
            }
            if (methodCallNode.TargetName.IsEmpty)
            {
                methodCallNode.SetDfirMessage(new DfirMessage(
                    NationalInstruments.SourceModel.MessageSeverity.Error,
                    SemanticAnalysisMessageCategories.Connection,
                    AllModelsOfComputationErrorMessages.NoValidMethodCallTarget));
            }
            return true;
        }

        public bool VisitOptionPatternStructureSelector(OptionPatternStructureSelector optionPatternStructureSelector)
        {
            ValidateRequiredInputTerminal(optionPatternStructureSelector.InputTerminals[0]);
            return true;
        }

        public bool VisitTerminateLifetimeNode(TerminateLifetimeNode terminateLifetimeNode)
        {
            foreach (var inputTerminal in terminateLifetimeNode.InputTerminals)
            {
                VariableUsageValidator validator = new VariableUsageValidator(inputTerminal, false);
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
            foreach (Terminal inputTerminal in tunnel.InputTerminals)
            {
                ValidateRequiredInputTerminal(inputTerminal);
            }
            return true;
        }

        public bool VisitTerminateLifetimeTunnel(TerminateLifetimeTunnel unborrowTunnel)
        {
            // This node has no inputs.
            return true;
        }

        public bool VisitUnwrapOptionTunnel(UnwrapOptionTunnel unwrapOptionTunnel)
        {
            ValidateRequiredInputTerminal(unwrapOptionTunnel.InputTerminals[0]);
            return true;
        }

        private void ValidateRequiredInputTerminal(Terminal inputTerminal)
        {
            if (inputTerminal.TestRequiredTerminalConnected())
            {
                _typeUnificationResults.SetMessagesOnTerminal(inputTerminal);
            }
        }

        private void ValidateOptionalInputTerminal(Terminal inputTerminal)
        {
            _typeUnificationResults.SetMessagesOnTerminal(inputTerminal);
        }

        private void VisitFunctionSignatureNode(Node node, NIType nodeFunctionSignature)
        {
            node.InputTerminals.ForEach(ValidateRequiredInputTerminal);
            // TODO: for functions with more than one data type parameter, it would be better
            // to report TypeNotDetermined on an output only if all inputs that use the same
            // type parameter(s) are connected.
            if (node.InputTerminals.All(terminal => terminal.IsConnected))
            {
                Signature signature = Signatures.GetSignatureForNIType(nodeFunctionSignature);
                foreach (var outputTerminalPair in node.OutputTerminals.Zip(signature.Outputs)
                    .Where(pair => !pair.Value.IsPassthrough))
                {
                    VariableReference outputVariable = outputTerminalPair.Key.GetTrueVariable();
                    if (outputVariable.TypeVariableReference.IsOrContainsTypeVariable)
                    {
                        outputTerminalPair.Key.SetDfirMessage(Messages.TypeNotDetermined);
                    }
                }
            }
        }
    }
}
