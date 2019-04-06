using System.Collections.Generic;
using System.Linq;
using NationalInstruments;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler.Nodes;

namespace Rebar.Compiler
{
    /// <summary>
    /// Sets the initial <see cref="NIType"/> and <see cref="Lifetime"/> of any <see cref="VariableReference"/>s associated
    /// with non-passthrough output terminals on each node. Can assume that all <see cref="VariableReference"/>s associated 
    /// with input terminals (passthrough and non-passthrough) have initial types and lifetimes set.
    /// </summary>
    internal class SetVariableTypesAndLifetimesTransform : VisitorTransformBase, IDfirNodeVisitor<bool>
    {
        protected override void VisitNode(Node node)
        {
            AutoBorrowNodeFacade nodeFacade = AutoBorrowNodeFacade.GetNodeFacade(node);
            nodeFacade.UpdateInputsFromFacadeTypes();

            this.VisitRebarNode(node);
        }

        protected override void VisitWire(Wire wire)
        {
            // Merge together all connected wire and node terminals
            foreach (var wireTerminal in wire.Terminals)
            {
                var connectedNodeTerminal = wireTerminal.ConnectedTerminal;
                if (connectedNodeTerminal != null)
                {
                    if (wireTerminal.Direction == Direction.Input)
                    {
                        wireTerminal.GetFacadeVariable().MergeInto(connectedNodeTerminal.GetFacadeVariable());
                    }
                    else
                    {
                        connectedNodeTerminal.GetFacadeVariable().MergeInto(wireTerminal.GetFacadeVariable());
                    }
                }
            }

            // If source is available and there are copied sinks, set source variable type and lifetime on copied sinks
            if (!wire.SinkTerminals.HasMoreThan(1))
            {
                return;
            }
            Terminal sourceTerminal;
            wire.TryGetSourceTerminal(out sourceTerminal);
            VariableReference? sourceVariable = sourceTerminal?.GetFacadeVariable();
            if (sourceVariable == null)
            {
                return;
            }
            foreach (var sinkVariable in wire.SinkTerminals.Skip(1).Select(VariableExtensions.GetFacadeVariable))
            {
                sinkVariable.SetTypeAndLifetime(sourceVariable.Value.Type, sourceVariable.Value.Lifetime);
            }
        }

        protected override void VisitBorderNode(NationalInstruments.Dfir.BorderNode borderNode)
        {
            AutoBorrowNodeFacade nodeFacade = AutoBorrowNodeFacade.GetNodeFacade(borderNode);
            nodeFacade.UpdateInputsFromFacadeTypes();

            this.VisitRebarNode(borderNode);
        }

        public bool VisitAssignNode(AssignNode assignNode)
        {
            // This node does not create any new variables.
            return true;
        }

        public bool VisitBorrowTunnel(BorrowTunnel borrowTunnel)
        {
            Terminal inputTerminal = borrowTunnel.Terminals.ElementAt(0),
                outputTerminal = borrowTunnel.Terminals.ElementAt(1);
            NIType outputUnderlyingType = inputTerminal.GetTrueVariable().Type;
            NIType outputType = borrowTunnel.BorrowMode == BorrowMode.Mutable
                ? outputUnderlyingType.CreateMutableReference()
                : outputUnderlyingType.CreateImmutableReference();

            Lifetime outputLifetime = outputTerminal.GetVariableSet().DefineLifetimeThatOutlastsDiagram();
            outputTerminal.GetTrueVariable().SetTypeAndLifetime(
                outputType,
                outputLifetime);
            return true;
        }

        public bool VisitConstant(Constant constant)
        {
            constant.OutputTerminal.GetTrueVariable().SetTypeAndLifetime(constant.DataType, Lifetime.Unbounded);
            return true;
        }

        public bool VisitCreateCellNode(CreateCellNode createCellNode)
        {
            Terminal valueInTerminal = createCellNode.Terminals.ElementAt(0);
            Terminal cellOutTerminal = createCellNode.Terminals.ElementAt(1);
            VariableReference inputVariable = valueInTerminal.GetTrueVariable();
            NIType underlyingType = inputVariable.Type;
            NIType cellType = inputVariable.Mutable    // TODO: Locking and non-Locking cells should be created
                // by different nodes
                ? underlyingType.CreateLockingCell()
                : underlyingType.CreateNonLockingCell();
            cellOutTerminal.GetTrueVariable().SetTypeAndLifetime(
                cellType,
                Lifetime.Unbounded);
            return true;
        }

        public bool VisitDropNode(DropNode dropNode)
        {
            return true;
        }

        public bool VisitExchangeValuesNode(ExchangeValuesNode exchangeValuesNode)
        {
            return true;
        }

        public bool VisitExplicitBorrowNode(ExplicitBorrowNode explicitBorrowNode)
        {
            int inputCount = explicitBorrowNode.InputTerminals.Count;
            IEnumerable<VariableReference> inputVariables = explicitBorrowNode.InputTerminals.Select(VariableExtensions.GetTrueVariable);
            IEnumerable<NIType> outputTypes = inputVariables
                .Select(inputVariable => GetBorrowedOutputType(inputVariable, explicitBorrowNode.BorrowMode, explicitBorrowNode.AlwaysCreateReference));

            Lifetime firstLifetime = inputVariables.First().Lifetime;
            Lifetime outputLifetime = explicitBorrowNode.AlwaysBeginLifetime 
                || !((firstLifetime?.IsBounded ?? false) && inputVariables.All(inputVariable => inputVariable.Lifetime == firstLifetime))
                ? explicitBorrowNode.ParentDiagram.GetVariableSet().DefineLifetimeThatIsBoundedByDiagram(inputVariables)
                : firstLifetime;

            // TODO: when necessary, mark the output lifetime as being a supertype of any of the bounded input lifetimes
            foreach (var pair in explicitBorrowNode.OutputTerminals.Zip(outputTypes))
            {
                Terminal outputTerminal = pair.Key;
                NIType outputType = pair.Value;
                outputTerminal.GetTrueVariable().SetTypeAndLifetime(outputType, outputLifetime);
            }
            return true;
        }

        private NIType GetBorrowedOutputType(VariableReference inputVariable, BorrowMode borrowMode, bool alwaysCreateReference)
        {
            NIType outputUnderlyingType = alwaysCreateReference
                ? inputVariable.Type
                : (inputVariable.Type.GetTypeOrReferentType());
            return borrowMode == BorrowMode.Immutable
                ? outputUnderlyingType.CreateImmutableReference()
                : outputUnderlyingType.CreateMutableReference();
        }

        public bool VisitFunctionalNode(FunctionalNode functionalNode)
        {
            // type propagation: figure out substitutions for any type parameters based on inputs
            var genericParameters = functionalNode.Signature.GetGenericParameters();
            Dictionary<NIType, NIType> substitutions = new Dictionary<NIType, NIType>();
            Signature signature = Signatures.GetSignatureForNIType(functionalNode.Signature);
            foreach (var inputPair in functionalNode.InputTerminals.Zip(signature.Inputs))
            {
                Terminal input = inputPair.Key;
                SignatureTerminal signatureInput = inputPair.Value;
                if (signatureInput.SignatureType.IsImmutableReferenceType() || signatureInput.SignatureType.IsMutableReferenceType())
                {
                    NIType genericParameter = signatureInput.SignatureType.GetGenericParameters().ElementAt(0);
                    if (genericParameter.IsGenericParameter())
                    {
                        NIType referentType = input.GetTrueVariable().Type.GetReferentType();
                        substitutions[genericParameter] = referentType;
                    }
                }
            }

            // SetTypeAndLifetime for any output parameters based on type parameter substitutions
            foreach (var outputPair in functionalNode.OutputTerminals.Zip(signature.Outputs))
            {
                SignatureTerminal signatureOutput = outputPair.Value;
                if (signatureOutput.IsPassthrough)
                {
                    continue;
                }
                Terminal output = outputPair.Key;
                NIType outputType = NIType.Unset;
                if (signatureOutput.SignatureType.IsGenericParameter())
                {
                    substitutions.TryGetValue(signatureOutput.SignatureType, out outputType);
                }
                else
                {
                    outputType = signatureOutput.SignatureType;
                }
                if (!outputType.IsRebarReferenceType())
                {
                    output.GetTrueVariable().SetTypeAndLifetime(outputType, Lifetime.Unbounded);
                    continue;
                }
                throw new System.NotImplementedException();
            }
            return true;
        }

        public bool VisitIterateTunnel(IterateTunnel iterateTunnel)
        {
            VariableReference inputVariable = iterateTunnel.Terminals.ElementAt(0).GetTrueVariable();
            NIType outputType;
            NIType inputType = inputVariable.Type.GetReferentType();
            if (!inputType.TryDestructureIteratorType(out outputType))
            {
                outputType = PFTypes.Void;
            }
            Terminal outputTerminal = iterateTunnel.Terminals.ElementAt(1);
            outputTerminal.GetTrueVariable().SetTypeAndLifetime(
                outputType,
                outputType.IsRebarReferenceType()
                    ? outputTerminal.GetVariableSet().DefineLifetimeThatOutlastsDiagram()
                    : Lifetime.Unbounded);
            return true;
        }

        public bool VisitLockTunnel(LockTunnel lockTunnel)
        {
            Terminal inputTerminal = lockTunnel.Terminals.ElementAt(0),
                outputTerminal = lockTunnel.Terminals.ElementAt(1);
            NIType outputUnderlyingType;
            if (!inputTerminal.GetTrueVariable().Type.GetReferentType().TryDestructureLockingCellType(out outputUnderlyingType))
            {
                outputUnderlyingType = PFTypes.Void;
            }

            Lifetime outputLifetime = outputTerminal.GetVariableSet().DefineLifetimeThatOutlastsDiagram();
            outputTerminal.GetTrueVariable().SetTypeAndLifetime(
                outputUnderlyingType.CreateMutableReference(),
                outputLifetime);
            return true;
        }

        public bool VisitLoopConditionTunnel(LoopConditionTunnel loopConditionTunnel)
        {
            Terminal inputTerminal = loopConditionTunnel.Terminals.ElementAt(0),
                outputTerminal = loopConditionTunnel.Terminals.ElementAt(1);
            VariableReference inputVariable = inputTerminal.GetTrueVariable();
            if (!inputTerminal.IsConnected)
            {
                inputVariable.SetTypeAndLifetime(PFTypes.Boolean, Lifetime.Unbounded);
            }
            NIType outputType = PFTypes.Boolean.CreateMutableReference();

            Lifetime outputLifetime = outputTerminal.GetVariableSet().DefineLifetimeThatOutlastsDiagram();
            outputTerminal.GetTrueVariable().SetTypeAndLifetime(outputType, outputLifetime);
            return true;
        }

        public bool VisitSelectReferenceNode(SelectReferenceNode selectReferenceNode)
        {
            Terminal refInTerminal1 = selectReferenceNode.Terminals.ElementAt(1),
                refInTerminal2 = selectReferenceNode.Terminals.ElementAt(2),
                refOutTerminal = selectReferenceNode.Terminals.ElementAt(4);
            VariableReference input1Variable = refInTerminal1.GetTrueVariable();
            refOutTerminal.GetTrueVariable().SetTypeAndLifetime(input1Variable.Type, input1Variable.Lifetime);
            return true;
        }

        public bool VisitSomeConstructorNode(SomeConstructorNode someConstructorNode)
        {
            VariableReference valueInVariable = someConstructorNode.Terminals.ElementAt(0).GetTrueVariable(),
                optionOutVariable = someConstructorNode.Terminals.ElementAt(1).GetTrueVariable();
            NIType optionUnderlyingType = valueInVariable.Type;
            Lifetime optionLifetime = valueInVariable.Lifetime;

            optionOutVariable.SetTypeAndLifetime(
                optionUnderlyingType.CreateOption(),
                optionLifetime);
            return true;
        }

        public bool VisitTerminateLifetimeNode(TerminateLifetimeNode terminateLifetimeNode)
        {
            VariableSet variableSet = terminateLifetimeNode.ParentDiagram.GetVariableSet();
            IEnumerable<VariableReference> inputVariables = terminateLifetimeNode.InputTerminals.Select(VariableExtensions.GetTrueVariable);
            IEnumerable<Lifetime> inputLifetimes = inputVariables.Select(v => v.Lifetime).Distinct();
            Lifetime singleLifetime;

            IEnumerable<VariableReference> decomposedVariables = Enumerable.Empty<VariableReference>();
            TerminateLifetimeErrorState errorState = TerminateLifetimeErrorState.NoError;
            if (inputLifetimes.HasMoreThan(1))
            {
                errorState = TerminateLifetimeErrorState.InputLifetimesNotUnique;
            }
            else if ((singleLifetime = inputLifetimes.FirstOrDefault()) == null)
            {
                // this means no inputs were wired, which is an error, but we should report it as unwired inputs
                // in CheckVariableUsages below
                errorState = TerminateLifetimeErrorState.NoError;
            }
            else if (singleLifetime.DoesOutlastDiagram || !singleLifetime.IsBounded)
            {
                errorState = TerminateLifetimeErrorState.InputLifetimeCannotBeTerminated;
            }
            else
            {
                errorState = TerminateLifetimeErrorState.NoError;
                // TODO: this does not account for Variables in singleLifetime that have already been consumed
                IEnumerable<VariableReference> variablesMatchingLifetime = variableSet.GetUniqueVariableReferences().Where(v => v.Lifetime == singleLifetime);
                int requiredInputCount = variablesMatchingLifetime.Count();
                terminateLifetimeNode.RequiredInputCount = requiredInputCount;
                if (inputVariables.Count() != terminateLifetimeNode.RequiredInputCount)
                {
                    errorState = TerminateLifetimeErrorState.NotAllVariablesInLifetimeConnected;
                }
                decomposedVariables = variableSet.GetVariablesInterruptedByLifetime(singleLifetime);
                int outputCount = decomposedVariables.Count();
                terminateLifetimeNode.RequiredOutputCount = outputCount;

                terminateLifetimeNode.UpdateTerminals(requiredInputCount, outputCount);
            }
            terminateLifetimeNode.ErrorState = errorState;

            var decomposedVariablesConcat = decomposedVariables.Concat(Enumerable.Repeat<VariableReference>(new VariableReference(), int.MaxValue));
            foreach (var outputTerminalPair in terminateLifetimeNode.OutputTerminals.Zip(decomposedVariablesConcat))
            {
                Terminal outputTerminal = outputTerminalPair.Key;
                VariableReference decomposedVariable = outputTerminalPair.Value;
                outputTerminal.GetFacadeVariable().MergeInto(decomposedVariable);
            }
            return true;
        }

        public bool VisitTunnel(Tunnel tunnel)
        {
            Terminal inputTerminal = tunnel.Direction == Direction.Input ? tunnel.GetOuterTerminal() : tunnel.GetInnerTerminal();
            Terminal outputTerminal = tunnel.Direction == Direction.Input ? tunnel.GetInnerTerminal() : tunnel.GetOuterTerminal();
            VariableReference inputVariable = inputTerminal.GetTrueVariable(),
                outputVariable = outputTerminal.GetTrueVariable();
            var parentFrame = tunnel.ParentStructure as Frame;
            bool executesConditionally = parentFrame != null && DoesFrameExecuteConditionally(parentFrame);
            bool wrapOutputInOption = tunnel.Direction == Direction.Output && executesConditionally;

            Lifetime outputLifetime = Lifetime.Unbounded;
            NIType outputType = PFTypes.Void;
            // if input is unbounded/static, then output is unbounded/static
            // if input is from outer diagram, then output is a lifetime that outlasts the inner diagram
            // if input is from inner diagram and outlasts the inner diagram, we should be able to determine 
            //    which outer diagram lifetime it came from
            // otherwise, output is empty/error
            Lifetime inputLifetime = inputVariable.Lifetime;
            if (!inputLifetime.IsBounded)
            {
                outputLifetime = inputLifetime;
            }
            else if (tunnel.Direction == Direction.Input)
            {
                outputLifetime = outputTerminal.GetVariableSet().DefineLifetimeThatOutlastsDiagram();
            }
            // else if (inputLifetime outlasts inner diagram) { outputLifetime = outer diagram origin of inputLifetime; }
            else
            {
                outputLifetime = Lifetime.Empty;
            }
            outputType = inputVariable.Type;

            // If outputType is already an Option value type, then don't re-wrap it.
            if (wrapOutputInOption && !outputType.IsOptionType())
            {
                outputType = outputType.CreateOption();
            }
            outputVariable.SetTypeAndLifetime(
                outputType,
                outputLifetime);
            return true;
        }

        private bool DoesFrameExecuteConditionally(Frame frame)
        {
            // TODO: handle multi-frame flat sequence structures
            return frame.BorderNodes.OfType<UnwrapOptionTunnel>().Any();
        }

        public bool VisitTerminateLifetimeTunnel(TerminateLifetimeTunnel unborrowTunnel)
        {
            // Do nothing; the output terminal's variable is the same as the associated BorrowTunnel's input variable
            return true;
        }

        public bool VisitUnwrapOptionTunnel(UnwrapOptionTunnel unwrapOptionTunnel)
        {
            Terminal inputTerminal = unwrapOptionTunnel.Direction == Direction.Input ? unwrapOptionTunnel.GetOuterTerminal(0) : unwrapOptionTunnel.GetInnerTerminal(0, 0);
            Terminal outputTerminal = unwrapOptionTunnel.Direction == Direction.Input ? unwrapOptionTunnel.GetInnerTerminal(0, 0) : unwrapOptionTunnel.GetOuterTerminal(0);
            VariableReference inputVariable = inputTerminal.GetTrueVariable(),
                outputVariable = outputTerminal.GetTrueVariable();
            NIType optionType = inputVariable.Type;
            NIType optionValueType;
            if (optionType.TryDestructureOptionType(out optionValueType))
            {
                Lifetime outputLifetime = inputVariable.Lifetime.IsBounded
                    // TODO: when necessary, mark this lifetime as being related to the outer diagram lifetime
                    ? outputTerminal.GetVariableSet().DefineLifetimeThatOutlastsDiagram()
                    : inputVariable.Lifetime;
                outputVariable.SetTypeAndLifetime(
                    optionValueType,
                    outputLifetime);
                return true;
            }

            outputVariable.SetTypeAndLifetime(
                PFTypes.Void,
                Lifetime.Unbounded);
            return true;
        }
    }
}
