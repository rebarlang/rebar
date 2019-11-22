using System.Collections.Generic;
using System.Linq;
using NationalInstruments;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler.Nodes;

namespace Rebar.Compiler
{
    internal class TerminateLifetimeInputTerminalFacade : TerminalFacade
    {
        private readonly TerminateLifetimeUnificationState _unificationState;

        public TerminateLifetimeInputTerminalFacade(Terminal terminal, TerminateLifetimeUnificationState unificationState) : base(terminal)
        {
            TrueVariable = terminal.CreateNewVariable();
            _unificationState = unificationState;
        }

        public override VariableReference FacadeVariable => TrueVariable;

        public override VariableReference TrueVariable { get; }

        public override void UnifyWithConnectedWireTypeAsNodeInput(VariableReference wireFacadeVariable, ITypeUnificationResultFactory unificationResultFactory)
        {
            TrueVariable.MergeInto(wireFacadeVariable);
            _unificationState.UpdateFromConnectedInput(wireFacadeVariable);
        }
    }

    internal sealed class TerminateLifetimeUnificationState
    {
        private enum State
        {
            NoVariableSeen,
            VariablesInLifetimeRemaining,
            AllVariablesInLifetimeTerminated,
            InputLifetimeIsNotUnique,
            LifetimeCannotBeTerminated
        }

        private readonly Diagram _parentDiagram;
        private State _state = State.NoVariableSeen;
        private List<VariableReference> _inputVariables = new List<VariableReference>();

        public TerminateLifetimeUnificationState(Diagram parentDiagram)
        {
            _parentDiagram = parentDiagram;
        }

        public Lifetime CommonLifetime { get; private set; }

        public void UpdateFromConnectedInput(VariableReference connectedInput)
        {
            _inputVariables.Add(connectedInput);
            Lifetime inputLifetime = connectedInput.Lifetime;
            switch (_state)
            {
                case State.NoVariableSeen:
                    if (!inputLifetime.IsBounded
                        || inputLifetime.DoesOutlastDiagram(_parentDiagram)
                        || inputLifetime.IsDiagramLifetime(_parentDiagram))
                    {
                        _state = State.LifetimeCannotBeTerminated;
                        return;
                    }

                    _state = State.VariablesInLifetimeRemaining;
                    CommonLifetime = inputLifetime;
                    break;
                case State.VariablesInLifetimeRemaining:
                case State.AllVariablesInLifetimeTerminated:
                    if (inputLifetime != CommonLifetime)
                    {
                        _state = State.InputLifetimeIsNotUnique;
                        CommonLifetime = null;
                    }
                    break;
                case State.LifetimeCannotBeTerminated:
                case State.InputLifetimeIsNotUnique:
                    break;
            }
        }

        public void FinalizeTerminateLifetimeInputs(TerminateLifetimeNode terminateLifetimeNode, LifetimeVariableAssociation lifetimeVariableAssociation)
        {
            if (CommonLifetime != null && _state == State.VariablesInLifetimeRemaining)
            {
                // assume that none of the lifetimes of our inputs are going to change after this point
                foreach (var inputVariable in _inputVariables)
                {
                    lifetimeVariableAssociation.MarkVariableConsumed(inputVariable);
                }
                List<VariableReference> unconsumedVariables = _parentDiagram
                    .GetVariableSet()
                    .GetUniqueVariableReferences()
                    .Where(variable => variable.Lifetime == CommonLifetime && !lifetimeVariableAssociation.IsVariableConsumed(variable))
                    .ToList();
                if (unconsumedVariables.Count == 0)
                {
                    _state = State.AllVariablesInLifetimeTerminated;
                }

                int requiredInputCount = _inputVariables.Count + unconsumedVariables.Count;
                terminateLifetimeNode.RequiredInputCount = requiredInputCount;
                terminateLifetimeNode.UpdateInputTerminals(requiredInputCount);
            }
            UpdateTerminateLifetimeErrorState(terminateLifetimeNode);
        }

        private void UpdateTerminateLifetimeErrorState(TerminateLifetimeNode terminateLifetimeNode)
        {
            switch (_state)
            {
                case State.NoVariableSeen:
                case State.AllVariablesInLifetimeTerminated:
                    terminateLifetimeNode.ErrorState = TerminateLifetimeErrorState.NoError;
                    break;
                case State.VariablesInLifetimeRemaining:
                    terminateLifetimeNode.ErrorState = TerminateLifetimeErrorState.NotAllVariablesInLifetimeConnected;
                    break;
                case State.InputLifetimeIsNotUnique:
                    terminateLifetimeNode.ErrorState = TerminateLifetimeErrorState.InputLifetimesNotUnique;
                    break;
                case State.LifetimeCannotBeTerminated:
                    terminateLifetimeNode.ErrorState = TerminateLifetimeErrorState.InputLifetimeCannotBeTerminated;
                    break;
            }
        }

        public void UpdateTerminateLifetimeOutputs(TerminateLifetimeNode terminateLifetimeNode, LifetimeVariableAssociation lifetimeVariableAssociation)
        {
            if (CommonLifetime != null)
            {
                IEnumerable<VariableReference> interruptedVariables = lifetimeVariableAssociation.GetVariablesInterruptedByLifetime(CommonLifetime);
                int outputCount = interruptedVariables.Count();
                terminateLifetimeNode.RequiredOutputCount = outputCount;
                terminateLifetimeNode.UpdateOutputTerminals(outputCount);

                foreach (var outputTerminalPair in terminateLifetimeNode.OutputTerminals.Zip(interruptedVariables))
                {
                    Terminal outputTerminal = outputTerminalPair.Key;
                    VariableReference decomposedVariable = outputTerminalPair.Value;
                    if (decomposedVariable.IsValid)
                    {
                        outputTerminal.GetFacadeVariable().MergeInto(decomposedVariable);
                    }
                }
            }
            else
            {
                foreach (Terminal outputTerminal in terminateLifetimeNode.OutputTerminals)
                {
                    outputTerminal.GetFacadeVariable().MergeInto(outputTerminal.CreateNewVariableForUnwiredTerminal());
                }
            }
        }
    }
}
