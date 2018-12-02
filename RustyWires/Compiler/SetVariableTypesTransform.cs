using System.Collections.Generic;
using System.Linq;
using NationalInstruments;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using RustyWires.Common;
using RustyWires.Compiler.Nodes;

namespace RustyWires.Compiler
{
    /// <summary>
    /// Sets the initial <see cref="NIType"/> and <see cref="Lifetime"/> of any <see cref="Variable"/>s associated
    /// with non-passthrough output terminals on each node. Can assume that all <see cref="Variable"/>s associated 
    /// with input terminals (passthrough and non-passthrough) have initial types and lifetimes set.
    /// </summary>
    internal class SetVariableTypesAndLifetimesTransform : VisitorTransformBase, IRustyWiresDfirNodeVisitor<bool>
    {
        protected override void VisitNode(Node node)
        {
            this.VisitRustyWiresNode(node);
        }

        protected override void VisitWire(Wire wire)
        {
            if (wire.SinkTerminals.HasMoreThan(1))
            {
                Variable sourceVariable = wire.SourceTerminal.GetVariable();
                if (sourceVariable != null)
                {
                    foreach (var sinkVariable in wire.SinkTerminals.Skip(1).Select(VariableSetExtensions.GetVariable))
                    {
                        sinkVariable?.SetTypeAndLifetime(sourceVariable.Type, sourceVariable.Lifetime);
                    }
                }
            }
        }

        protected override void VisitBorderNode(BorderNode borderNode)
        {
            this.VisitRustyWiresNode(borderNode);
        }

        public bool VisitBorrowTunnel(BorrowTunnel borrowTunnel)
        {
            Terminal inputTerminal = borrowTunnel.Terminals.ElementAt(0),
                outputTerminal = borrowTunnel.Terminals.ElementAt(1);
            Variable inputVariable = inputTerminal.GetVariable();
            NIType outputUnderlyingType = inputVariable.GetUnderlyingTypeOrVoid();
            NIType outputType = borrowTunnel.BorrowMode == Common.BorrowMode.Mutable
                ? outputUnderlyingType.CreateMutableReference()
                : outputUnderlyingType.CreateImmutableReference();

            Lifetime sourceLifetime = inputVariable?.Lifetime ?? Lifetime.Empty;
            Lifetime outputLifetime = outputTerminal.GetVariableSet().DefineLifetimeThatOutlastsDiagram();
            outputTerminal.GetVariable()?.SetTypeAndLifetime(outputType, outputLifetime);
            return true;
        }

        public bool VisitConstant(Constant constant)
        {
            constant.OutputTerminal.GetVariable()?.SetTypeAndLifetime(constant.DataType, Lifetime.Static);
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
                NIType underlyingType = inputVariable.Type.GetUnderlyingTypeFromRustyWiresType();
                cellType = inputVariable.Type.IsMutableValueType()
                    ? underlyingType.CreateLockingCell()
                    : underlyingType.CreateNonLockingCell();
            }
            else
            {
                cellType = PFTypes.Void.CreateNonLockingCell();
            }
            cellOutTerminal.GetVariable()?.SetTypeAndLifetime(cellType.CreateMutableValue(), Lifetime.Unbounded);
            return true;
        }

        public bool VisitCreateMutableCopyNode(CreateMutableCopyNode createMutableCopyNode)
        {
            NIType outputType = createMutableCopyNode.InputTerminals.ElementAt(0).GetVariable().GetUnderlyingTypeOrVoid().CreateMutableValue();
            createMutableCopyNode.OutputTerminals.ElementAt(1).GetVariable()?.SetTypeAndLifetime(outputType, Lifetime.Unbounded);
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
            NIType outputUnderlyingType = inputVariable.GetUnderlyingTypeOrVoid();
            NIType outputType = explicitBorrowNode.BorrowMode == Nodes.BorrowMode.OwnerToImmutable
                ? outputUnderlyingType.CreateImmutableReference()
                : outputUnderlyingType.CreateMutableReference();

            Lifetime sourceLifetime = inputVariable?.Lifetime ?? Lifetime.Empty;
            Lifetime outputLifetime = outputTerminal.GetVariableSet().DefineLifetimeThatIsBoundedByDiagram(inputVariable.ToEnumerable());
            outputTerminal.GetVariable()?.SetTypeAndLifetime(outputType, outputLifetime);
            return true;
        }

        public bool VisitFreezeNode(FreezeNode freezeNode)
        {
            Terminal valueInTerminal = freezeNode.Terminals.ElementAt(0);
            Terminal valueOutTerminal = freezeNode.Terminals.ElementAt(1);
            NIType underlyingType = valueInTerminal.GetVariable().GetUnderlyingTypeOrVoid();
            valueOutTerminal.GetVariable()?.SetTypeAndLifetime(underlyingType.CreateImmutableValue(), Lifetime.Unbounded);
            return true;
        }

        public bool VisitImmutablePassthroughNode(ImmutablePassthroughNode immutablePassthroughNode)
        {
            return true;
        }

        public bool VisitLockTunnel(LockTunnel lockTunnel)
        {
            Terminal inputTerminal = lockTunnel.Terminals.ElementAt(0),
                outputTerminal = lockTunnel.Terminals.ElementAt(1);
            Variable inputVariable = inputTerminal.GetVariable();
            NIType inputUnderlyingType = inputVariable.GetUnderlyingTypeOrVoid();
            NIType outputUnderlyingType = inputUnderlyingType.IsLockingCellType()
                ? inputUnderlyingType.GetUnderlyingTypeFromLockingCellType()
                : PFTypes.Void;

            Lifetime sourceLifetime = inputVariable?.Lifetime ?? Lifetime.Empty;
            Lifetime outputLifetime = outputTerminal.GetVariableSet().DefineLifetimeThatOutlastsDiagram();
            outputTerminal.GetVariable()?.SetTypeAndLifetime(outputUnderlyingType.CreateMutableReference(), outputLifetime);
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
            NIType input1UnderlyingType = refInTerminal1.GetVariable().GetUnderlyingTypeOrVoid();
            NIType input2UnderlyingType = refInTerminal2.GetVariable().GetUnderlyingTypeOrVoid();
            NIType outputUnderlyingType = input1UnderlyingType == expectedInputUnderlyingType 
                && input2UnderlyingType == expectedInputUnderlyingType 
                ? expectedInputUnderlyingType
                : PFTypes.Void;
            resultOutTerminal.GetVariable()?.SetTypeAndLifetime(outputUnderlyingType.CreateMutableValue(), Lifetime.Unbounded);
            return true;
        }

        public bool VisitPureUnaryPrimitive(PureUnaryPrimitive pureUnaryPrimitive)
        {
            Terminal refInTerminal = pureUnaryPrimitive.Terminals.ElementAt(0),
                resultOutTerminal = pureUnaryPrimitive.Terminals.ElementAt(2);
            NIType expectedInputUnderlyingType = pureUnaryPrimitive.Operation.GetExpectedInputType();
            NIType inputUnderlyingType = refInTerminal.GetVariable().GetUnderlyingTypeOrVoid();
            NIType outputUnderlyingType = inputUnderlyingType == expectedInputUnderlyingType ? expectedInputUnderlyingType : PFTypes.Void;
            resultOutTerminal.GetVariable()?.SetTypeAndLifetime(outputUnderlyingType.CreateMutableValue(), Lifetime.Unbounded);
            return true;
        }

        public bool VisitSelectReferenceNode(SelectReferenceNode selectReferenceNode)
        {
            Terminal refInTerminal1 = selectReferenceNode.Terminals.ElementAt(0),
                refInTerminal2 = selectReferenceNode.Terminals.ElementAt(1),
                refOutTerminal = selectReferenceNode.Terminals.ElementAt(6);
            Variable input1Variable = refInTerminal1.GetVariable();
            Variable input2Variable = refInTerminal2.GetVariable();
            NIType input1UnderlyingType = input1Variable.GetUnderlyingTypeOrVoid();
            NIType input2UnderlyingType = input2Variable.GetUnderlyingTypeOrVoid();
            NIType outputUnderlyingType = input1UnderlyingType == input2UnderlyingType ? input1UnderlyingType : PFTypes.Void;

            Lifetime inputLifetime1 = input1Variable?.Lifetime ?? Lifetime.Empty;
            Lifetime inputLifetime2 = input2Variable?.Lifetime ?? Lifetime.Empty;
            Lifetime commonLifetime = refInTerminal1.GetVariableSet().ComputeCommonLifetime(inputLifetime1, inputLifetime2);
            refOutTerminal.GetVariable()?.SetTypeAndLifetime(outputUnderlyingType.CreateImmutableReference(), commonLifetime);
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
                IEnumerable<Variable> variablesMatchingLifetime = variableSet.Variables.Where(v => v.Lifetime == singleLifetime);
                terminateLifetimeNode.RequiredInputCount = variablesMatchingLifetime.Count();
                if (inputVariables.Count() != terminateLifetimeNode.RequiredInputCount)
                {
                    errorState = TerminateLifetimeErrorState.NotAllVariablesInLifetimeConnected;
                }
                decomposedVariables = variableSet.GetVariablesInterruptedByLifetime(singleLifetime);
                terminateLifetimeNode.RequiredOutputCount = decomposedVariables.Count();
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
                    variableSet.MergeVariables(originalOutputVariable, decomposedVariable);
                }
                else
                {
                    variableSet.AddTerminalToNewVariable(outputTerminal);
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
            if (outputVariable != null)
            {
                if (inputVariable != null)
                {
                    // if input is unbounded/static, then output is unbounded/static
                    // if input is from outer diagram, then output is a lifetime that outlasts the inner diagram
                    // if input is from inner diagram and outlasts the inner diagram, we should be able to determine 
                    //    which outer diagram lifetime it came from
                    // otherwise, output is empty/error
                    Lifetime inputLifetime = inputVariable.Lifetime, outputLifetime;
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
                    outputVariable.SetTypeAndLifetime(inputVariable.Type, outputLifetime);
                }
                else
                {
                    outputVariable.SetTypeAndLifetime(PFTypes.Void.CreateImmutableValue(), Lifetime.Unbounded);
                }
            }
            return true;
        }

        public bool VisitUnborrowTunnel(UnborrowTunnel unborrowTunnel)
        {
            // Do nothing; the output terminal's variable is the same as the associated BorrowTunnel's input variable
            return true;
        }

        public bool VisitUnlockTunnel(UnlockTunnel unlockTunnel)
        {
            // Do nothing; the output terminal's variable is the same as the associated LockTunnel's input variable
            return true;
        }
    }
}
