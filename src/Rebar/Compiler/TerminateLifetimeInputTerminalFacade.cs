using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            TrueVariable = terminal.GetVariableSet().CreateNewVariable(default(TypeVariableReference));
            _unificationState = unificationState;
        }

        public override VariableReference FacadeVariable => TrueVariable;

        public override VariableReference TrueVariable { get; }

        public override void UnifyWithConnectedWireTypeAsNodeInput(VariableReference wireFacadeVariable, TerminalTypeUnificationResults unificationResults)
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
        private Dictionary<VariableReference, bool> _variablesToTerminate;

        public TerminateLifetimeUnificationState(Diagram parentDiagram)
        {
            _parentDiagram = parentDiagram;
        }

        public Lifetime CommonLifetime { get; private set; }

        public void UpdateFromConnectedInput(VariableReference connectedInput)
        {
            // update the state of the associated TerminateLifetimeNode
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
                    // TODO: this does not account for Variables in singleLifetime that have already been consumed
                    _variablesToTerminate = new Dictionary<VariableReference, bool>();
                    // Problem: accessing VariableReference.Lifetime here is invalid for any Variables we haven't visited yet.
                    foreach (VariableReference variable in _parentDiagram.GetVariableSet().GetUniqueVariableReferences())
                    {
                        // HACK
                        try
                        {
                            if (variable.Lifetime != inputLifetime)
                            {
                                continue;
                            }
                        }
                        catch (ArgumentException)
                        {
                            continue;
                        }
                        _variablesToTerminate[variable] = false;
                    }
                    RemoveVariableFromTerminationSet(connectedInput);
                    break;
                case State.VariablesInLifetimeRemaining:
                case State.AllVariablesInLifetimeTerminated:
                    if (inputLifetime == CommonLifetime)
                    {
                        RemoveVariableFromTerminationSet(connectedInput);
                    }
                    else
                    {
                        _state = State.InputLifetimeIsNotUnique;
                        CommonLifetime = null;
                        // _variablesToTerminate = null;
                    }
                    break;
                case State.LifetimeCannotBeTerminated:
                case State.InputLifetimeIsNotUnique:
                    break;
            }
        }

        private void RemoveVariableFromTerminationSet(VariableReference variable)
        {
            if (_variablesToTerminate.ContainsKey(variable))
            {
                _variablesToTerminate[variable] = true;
            }
            if (_variablesToTerminate.All(pair => pair.Value))
            {
                _state = State.AllVariablesInLifetimeTerminated;
            }
        }

        public void UpdateTerminateLifetimeOutputs(TerminateLifetimeNode terminateLifetimeNode, LifetimeVariableAssociation lifetimeVariableAssociation)
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
            if (CommonLifetime != null)
            {
                int requiredInputCount = _variablesToTerminate.Count();
                terminateLifetimeNode.RequiredInputCount = requiredInputCount;
                IEnumerable<VariableReference> interruptedVariables = lifetimeVariableAssociation.GetVariablesInterruptedByLifetime(CommonLifetime);
                int outputCount = interruptedVariables.Count();
                terminateLifetimeNode.RequiredOutputCount = outputCount;
                terminateLifetimeNode.UpdateTerminals(requiredInputCount, outputCount);

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
        }
    }
}
