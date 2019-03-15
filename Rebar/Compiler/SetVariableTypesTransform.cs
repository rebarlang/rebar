using System;
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
    /// Sets the initial <see cref="NIType"/> and <see cref="Lifetime"/> of any <see cref="Variable"/>s associated
    /// with non-passthrough output terminals on each node. Can assume that all <see cref="Variable"/>s associated 
    /// with input terminals (passthrough and non-passthrough) have initial types and lifetimes set.
    /// </summary>
    internal class SetVariableTypesAndLifetimesTransform : VisitorTransformBase, IDfirNodeVisitor<bool>
    {
        protected override void VisitNode(Node node)
        {
            this.VisitRebarNode(node);
        }

        protected override void VisitWire(Wire wire)
        {
            if (!wire.SinkTerminals.HasMoreThan(1))
            {
                return;
            }
            Terminal sourceTerminal;
            wire.TryGetSourceTerminal(out sourceTerminal);
            Variable sourceVariable = sourceTerminal?.GetVariable();
            if (sourceVariable == null)
            {
                return;
            }
            foreach (var sinkVariable in wire.SinkTerminals.Skip(1).Select(VariableSetExtensions.GetVariable))
            {
                sinkVariable?.SetTypeAndLifetime(sourceVariable.Type, sourceVariable.Lifetime);
            }
        }

        protected override void VisitBorderNode(NationalInstruments.Dfir.BorderNode borderNode)
        {
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
            Variable inputVariable = inputTerminal.GetVariable();
            NIType outputUnderlyingType = inputVariable.GetTypeOrVoid();
            NIType outputType = borrowTunnel.BorrowMode == Common.BorrowMode.Mutable
                ? outputUnderlyingType.CreateMutableReference()
                : outputUnderlyingType.CreateImmutableReference();

            Lifetime sourceLifetime = inputVariable?.Lifetime ?? Lifetime.Empty;
            Lifetime outputLifetime = outputTerminal.GetVariableSet().DefineLifetimeThatOutlastsDiagram();
            outputTerminal.GetVariable()?.SetTypeAndLifetime(
                outputType,
                outputLifetime);
            return true;
        }

        public bool VisitConstant(Constant constant)
        {
            constant.OutputTerminal.GetVariable()?.SetTypeAndLifetime(
                constant.DataType,
                Lifetime.Static);
            return true;
        }

        public bool VisitCreateCellNode(CreateCellNode createCellNode)
        {
            Terminal valueInTerminal = createCellNode.Terminals.ElementAt(0);
            Terminal cellOutTerminal = createCellNode.Terminals.ElementAt(1);
            NIType cellType;
            Variable inputVariable = valueInTerminal.GetVariable();
            if (inputVariable != null)
            {
                NIType underlyingType = inputVariable.Type;
                cellType = inputVariable.Mutable    // TODO: Locking and non-Locking cells should be created
                    // by different nodes
                    ? underlyingType.CreateLockingCell()
                    : underlyingType.CreateNonLockingCell();
            }
            else
            {
                cellType = PFTypes.Void.CreateNonLockingCell();
            }
            cellOutTerminal.GetVariable()?.SetTypeAndLifetime(
                cellType,
                Lifetime.Unbounded);
            return true;
        }

        public bool VisitCreateCopyNode(CreateCopyNode createCopyNode)
        {
            Variable inputVariable = createCopyNode.InputTerminals.ElementAt(0).GetVariable();
            NIType outputType = inputVariable.GetTypeOrVoid().GetTypeOrReferentType();
            Variable outputVariable = createCopyNode.OutputTerminals.ElementAt(1).GetVariable();
            outputVariable?.SetTypeAndLifetime(
                outputType,
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
            Terminal inputTerminal = explicitBorrowNode.Terminals.ElementAt(0);
            Terminal outputTerminal = explicitBorrowNode.Terminals.ElementAt(1);
            Variable inputVariable = inputTerminal.GetVariable();
            NIType outputUnderlyingType = inputVariable.GetTypeOrVoid();
            NIType outputType = explicitBorrowNode.BorrowMode == Nodes.BorrowMode.OwnerToImmutable
                ? outputUnderlyingType.CreateImmutableReference()
                : outputUnderlyingType.CreateMutableReference();

            Lifetime sourceLifetime = inputVariable?.Lifetime ?? Lifetime.Empty;
            Lifetime outputLifetime = outputTerminal.GetVariableSet().DefineLifetimeThatIsBoundedByDiagram(inputVariable.ToEnumerable());
            outputTerminal.GetVariable()?.SetTypeAndLifetime(
                outputType,
                outputLifetime);
            return true;
        }

        public bool VisitImmutablePassthroughNode(ImmutablePassthroughNode immutablePassthroughNode)
        {
            return true;
        }

        public bool VisitIterateTunnel(IterateTunnel iterateTunnel)
        {
            Variable inputVariable = iterateTunnel.Terminals.ElementAt(0).GetVariable();
            NIType outputType;
            NIType inputType = inputVariable.GetTypeOrVoid();
            if (!inputType.TryDestructureIteratorType(out outputType)
                && !inputType.TryDestructureVectorType(out outputType))
            {
                outputType = PFTypes.Void;
            }
            Terminal outputTerminal = iterateTunnel.Terminals.ElementAt(1);
            outputTerminal.GetVariable()?.SetTypeAndLifetime(
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
            Variable inputVariable = inputTerminal.GetVariable();
            NIType inputUnderlyingType = inputVariable.GetTypeOrVoid().GetTypeOrReferentType();
            NIType outputUnderlyingType = inputUnderlyingType.IsLockingCellType()
                ? inputUnderlyingType.GetUnderlyingTypeFromLockingCellType()
                : PFTypes.Void;

            Lifetime sourceLifetime = inputVariable?.Lifetime ?? Lifetime.Empty;
            Lifetime outputLifetime = outputTerminal.GetVariableSet().DefineLifetimeThatOutlastsDiagram();
            outputTerminal.GetVariable()?.SetTypeAndLifetime(
                outputUnderlyingType.CreateMutableReference(),
                outputLifetime);
            return true;
        }

        public bool VisitLoopConditionTunnel(LoopConditionTunnel loopConditionTunnel)
        {
            Terminal inputTerminal = loopConditionTunnel.Terminals.ElementAt(0),
                outputTerminal = loopConditionTunnel.Terminals.ElementAt(1);
            Variable inputVariable = inputTerminal.GetVariable();
            if (!inputTerminal.IsConnected)
            {
                inputVariable.SetTypeAndLifetime(PFTypes.Boolean, Lifetime.Unbounded);
            }
            NIType outputType = PFTypes.Boolean.CreateMutableReference();

            Lifetime outputLifetime = outputTerminal.GetVariableSet().DefineLifetimeThatOutlastsDiagram();
            outputTerminal.GetVariable()?.SetTypeAndLifetime(outputType, outputLifetime);
            return true;
        }

        public bool VisitMutablePassthroughNode(MutablePassthroughNode mutablePassthroughNode)
        {
            return true;
        }

        public bool VisitMutatingBinaryPrimitive(MutatingBinaryPrimitive mutatingBinaryPrimitive)
        {
            return true;
        }

        public bool VisitOutputNode(OutputNode outputNode)
        {
            return true;
        }

        public bool VisitMutatingUnaryPrimitive(MutatingUnaryPrimitive mutatingUnaryPrimitive)
        {
            return true;
        }

        public bool VisitPureBinaryPrimitive(PureBinaryPrimitive pureBinaryPrimitive)
        {
            Terminal refInTerminal1 = pureBinaryPrimitive.Terminals.ElementAt(0),
                refInTerminal2 = pureBinaryPrimitive.Terminals.ElementAt(1),
                resultOutTerminal = pureBinaryPrimitive.Terminals.ElementAt(4);
            NIType expectedInputUnderlyingType = pureBinaryPrimitive.Operation.GetExpectedInputType();
            NIType input1UnderlyingType = refInTerminal1.GetVariable().GetTypeOrVoid().GetTypeOrReferentType();
            NIType input2UnderlyingType = refInTerminal2.GetVariable().GetTypeOrVoid().GetTypeOrReferentType();
            NIType outputUnderlyingType = input1UnderlyingType == expectedInputUnderlyingType 
                && input2UnderlyingType == expectedInputUnderlyingType
                ? expectedInputUnderlyingType
                : PFTypes.Void;
            resultOutTerminal.GetVariable()?.SetTypeAndLifetime(
                outputUnderlyingType,
                Lifetime.Unbounded);
            return true;
        }

        public bool VisitPureUnaryPrimitive(PureUnaryPrimitive pureUnaryPrimitive)
        {
            Terminal refInTerminal = pureUnaryPrimitive.Terminals.ElementAt(0),
                resultOutTerminal = pureUnaryPrimitive.Terminals.ElementAt(2);
            NIType expectedInputUnderlyingType = pureUnaryPrimitive.Operation.GetExpectedInputType();
            NIType inputUnderlyingType = refInTerminal.GetVariable().GetTypeOrVoid().GetTypeOrReferentType();
            NIType outputUnderlyingType = inputUnderlyingType == expectedInputUnderlyingType ? expectedInputUnderlyingType : PFTypes.Void;
            resultOutTerminal.GetVariable()?.SetTypeAndLifetime(
                outputUnderlyingType,
                Lifetime.Unbounded);
            return true;
        }

        public bool VisitRangeNode(RangeNode rangeNode)
        {
            Variable outputVariable = rangeNode.Terminals.ElementAt(2).GetVariable();
            outputVariable?.SetTypeAndLifetime(PFTypes.Int32.CreateIterator(), Lifetime.Unbounded);
            return true;
        }

        public bool VisitSelectReferenceNode(SelectReferenceNode selectReferenceNode)
        {
            Terminal refInTerminal1 = selectReferenceNode.Terminals.ElementAt(1),
                refInTerminal2 = selectReferenceNode.Terminals.ElementAt(2),
                refOutTerminal = selectReferenceNode.Terminals.ElementAt(4);
            Variable input1Variable = refInTerminal1.GetVariable();
            Variable input2Variable = refInTerminal2.GetVariable();
            NIType input1UnderlyingType = input1Variable.GetTypeOrVoid().GetTypeOrReferentType();
            NIType input2UnderlyingType = input2Variable.GetTypeOrVoid().GetTypeOrReferentType();
            NIType outputUnderlyingType = input1UnderlyingType == input2UnderlyingType ? input1UnderlyingType : PFTypes.Void;

            var variableSet = selectReferenceNode.ParentDiagram.GetVariableSet();

            bool outputReferenceIsMutable;
            Lifetime commonLifetime;
            ComputeOutputTypeAndLifetimeForMutabilityPolymorphicNode(
                out outputReferenceIsMutable,
                out commonLifetime,
                variableSet,
                input1Variable,
                input2Variable);
            NIType outputReferenceType = outputReferenceIsMutable
                ? outputUnderlyingType.CreateMutableReference()
                : outputUnderlyingType.CreateImmutableReference();
            refOutTerminal.GetVariable().SetTypeAndLifetime(outputReferenceType, commonLifetime);
            return true;
        }

        public bool VisitSomeConstructorNode(SomeConstructorNode someConstructorNode)
        {
            Variable valueInVariable = someConstructorNode.Terminals.ElementAt(0).GetVariable(),
                optionOutVariable = someConstructorNode.Terminals.ElementAt(1).GetVariable();
            NIType optionUnderlyingType = PFTypes.Void;
            Lifetime optionLifetime = Lifetime.Unbounded;
            if (valueInVariable != null)
            {
                optionUnderlyingType = valueInVariable.Type;
                optionLifetime = valueInVariable.Lifetime;
            }

            optionOutVariable?.SetTypeAndLifetime(
                optionUnderlyingType.CreateOption(),
                optionLifetime);
            return true;
        }

        public bool VisitTerminateLifetimeNode(TerminateLifetimeNode terminateLifetimeNode)
        {
            VariableSet variableSet = terminateLifetimeNode.ParentDiagram.GetVariableSet();
            IEnumerable<Variable> inputVariables = terminateLifetimeNode.InputTerminals.Select(VariableSetExtensions.GetVariable).Where(v => v != null);
            IEnumerable<Lifetime> inputLifetimes = inputVariables.Select(v => v.Lifetime).Distinct();
            Lifetime singleLifetime;

            IEnumerable<Variable> decomposedVariables = Enumerable.Empty<Variable>();
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
                IEnumerable<Variable> variablesMatchingLifetime = variableSet.Variables.Where(v => v.Lifetime == singleLifetime);
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

            var decomposedVariablesConcat = decomposedVariables.Concat(Enumerable.Repeat<Variable>(null, int.MaxValue));
            foreach (var outputTerminalPair in terminateLifetimeNode.OutputTerminals.Zip(decomposedVariablesConcat))
            {
                Terminal outputTerminal = outputTerminalPair.Key;
                Variable decomposedVariable = outputTerminalPair.Value;
                if (decomposedVariable != null)
                {
                    Variable originalOutputVariable = variableSet.GetVariableForTerminal(outputTerminal);
                    if (originalOutputVariable != null)
                    {
                        variableSet.MergeVariables(originalOutputVariable, decomposedVariable);
                    }
                    else
                    {
                        outputTerminal.AddTerminalToVariable(decomposedVariable);
                    }
                }
                else
                {
                    variableSet.AddTerminalToNewVariable(outputTerminal, false);
                }
            }
            return true;
        }

        public bool VisitTunnel(Tunnel tunnel)
        {
            Terminal inputTerminal = tunnel.Direction == Direction.Input ? tunnel.GetOuterTerminal() : tunnel.GetInnerTerminal();
            Terminal outputTerminal = tunnel.Direction == Direction.Input ? tunnel.GetInnerTerminal() : tunnel.GetOuterTerminal();
            Variable inputVariable = inputTerminal.GetVariable(),
                outputVariable = outputTerminal.GetVariable();
            var parentFrame = tunnel.ParentStructure as Frame;
            bool executesConditionally = parentFrame != null && DoesFrameExecuteConditionally(parentFrame);
            bool wrapOutputInOption = tunnel.Direction == Direction.Output && executesConditionally;

            Lifetime outputLifetime;
            NIType outputType;
            if (outputVariable != null)
            {
                outputType = PFTypes.Void;
                outputLifetime = Lifetime.Unbounded;
                if (inputVariable != null)
                {
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
                }

                // If outputType is already an Option value type, then don't re-wrap it.
                if (wrapOutputInOption && !outputType.IsOptionType())
                {
                    outputType = outputType.CreateOption();
                }
                outputVariable.SetTypeAndLifetime(
                    outputType,
                    outputLifetime);
            }
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
            Variable inputVariable = inputTerminal.GetVariable(),
                outputVariable = outputTerminal.GetVariable();
            if (outputVariable != null)
            {
                if (inputVariable != null)
                {
                    NIType optionType = inputVariable.Type;
                    NIType optionValueType;
                    if (optionType.TryDestructureOptionType(out optionValueType))
                    {
                        Lifetime outputLifetime = inputVariable.Lifetime.IsBounded
                            ? outputTerminal.GetVariableSet().DefineLifetimeThatOutlastsDiagram()
                            : inputVariable.Lifetime; // TODO: this can't be right if it's supposed to be in the  inner variable set
                        outputVariable.SetTypeAndLifetime(
                            optionValueType,
                            outputLifetime);
                        return true;
                    }
                }

                outputVariable.SetTypeAndLifetime(
                    PFTypes.Void,
                    Lifetime.Unbounded);
            }
            return true;
        }

        public bool VisitVectorCreateNode(VectorCreateNode vectorCreateNode)
        {
            Variable outputVariable = vectorCreateNode.Terminals.ElementAt(0).GetVariable();
            outputVariable?.SetTypeAndLifetime(PFTypes.Int32.CreateVector(), Lifetime.Unbounded);
            return true;
        }

        /// <summary>
        /// Given some number of <see cref="Variable"/>s representing inputs to a node that is polymorphic in the
        /// mutability of the references it accepts for those inputs, computes whether the corresponding node output
        /// references should be mutable and what their lifetime should be.
        /// </summary>
        /// <param name="outputIsMutableReference">Whether any output references corresponding to the inputs can be mutable.</param>
        /// <param name="outputLifetime">The lifetime to associate with output references corresponding to the inputs.</param>
        /// <param name="variables">The input variables to compute the output information for.</param>
        private static void ComputeOutputTypeAndLifetimeForMutabilityPolymorphicNode(out bool outputIsMutableReference, out Lifetime outputLifetime, VariableSet variableSet, params Variable[] variables)
        {
            if (variables.Length < 1)
            {
                throw new ArgumentException("Expected at least one variable.", "variables");
            }
            // 1. If all inputs are mutable references with the same bounded lifetime, then
            // output a mutable reference in that lifetime
            Lifetime firstLifetime = variables[0].Lifetime ?? Lifetime.Empty;
            if (variables.All(v => v.GetTypeOrVoid().IsMutableReferenceType())
                && variables.All(v => v.Lifetime == firstLifetime))
            {
                outputIsMutableReference = true;
                outputLifetime = firstLifetime;
            }
            // 2. If all inputs can be borrowed into mutable references, then output a mutable
            // reference in a new diagram-bounded lifetime
            else if (variables.All(v => v.GetTypeOrVoid().IsMutableReferenceType() || v.Mutable))
            {
                outputIsMutableReference = true;
                outputLifetime = variableSet.DefineLifetimeThatIsBoundedByDiagram(variables);
            }
            // 3. If all inputs are immutable references with the same bounded lifetime, then
            // output an immutable reference in that lifetime
            else if (variables.All(v => v.GetTypeOrVoid().IsImmutableReferenceType())
                && variables.All(v => v.Lifetime == firstLifetime))
            {
                outputIsMutableReference = false;
                outputLifetime = firstLifetime;
            }
            // 4. Otherwise, output an immutable reference in a new diagram-bounded lifetime.
            else
            {
                outputIsMutableReference = false;
                outputLifetime = variableSet.DefineLifetimeThatIsBoundedByDiagram(variables);
            }
        }
    }
}
