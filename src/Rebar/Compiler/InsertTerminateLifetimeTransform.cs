using System.Collections.Generic;
using System.Linq;
using NationalInstruments;
using NationalInstruments.Compiler;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler.Nodes;

namespace Rebar.Compiler
{
    internal class InsertTerminateLifetimeTransform : IDfirTransform
    {
        private readonly LifetimeVariableAssociation _lifetimeVariableAssociation;
        private readonly ITypeUnificationResultFactory _unificationResultFactory;

        public InsertTerminateLifetimeTransform(LifetimeVariableAssociation lifetimeVariableAssociation, ITypeUnificationResultFactory unificationResultFactory)
        {
            _lifetimeVariableAssociation = lifetimeVariableAssociation;
            _unificationResultFactory = unificationResultFactory;
        }

        public void Execute(DfirRoot dfirRoot, CompileCancellationToken cancellationToken)
        {
            var lifetimeGraphTree = dfirRoot.GetLifetimeGraphTree();
            BoundedLifetimeLiveVariableSet boundedLifetimeLiveVariableSet;
            while (_lifetimeVariableAssociation.TryGetBoundedLifetimeWithLiveVariables(out boundedLifetimeLiveVariableSet))
            {
                if (lifetimeGraphTree.IsDiagramLifetimeOfAnyLifetimeGraph(boundedLifetimeLiveVariableSet.Lifetime))
                {
                    // Since we assume there are no semantic errors at this point, just mark any remaining live variables
                    // in a diagram lifetime as consumed.
                    boundedLifetimeLiveVariableSet.LiveVariables.Select(l => l.Variable).ForEach(_lifetimeVariableAssociation.MarkVariableConsumed);
                    continue;
                }

                int inputVariableCount = boundedLifetimeLiveVariableSet.LiveVariables.Count();
                IEnumerable<VariableReference> interruptedVariables = _lifetimeVariableAssociation.GetVariablesInterruptedByLifetime(boundedLifetimeLiveVariableSet.Lifetime);
                int outputVariableCount = interruptedVariables.Count();

                Diagram startSearch = boundedLifetimeLiveVariableSet.LiveVariables.First().Terminal.ParentDiagram;
                LifetimeGraphIdentifier originGraphIdentifier = lifetimeGraphTree.GetBoundedLifetimeGraphIdentifier(boundedLifetimeLiveVariableSet.Lifetime);
                Diagram originDiagram = originGraphIdentifier.FindDiagramForGraphIdentifier(startSearch);

                LiveVariable[] liveVariables = boundedLifetimeLiveVariableSet.LiveVariables.ToArray();
                for (int i = 0; i < liveVariables.Length; ++i)
                {
                    LiveVariable liveVariable = liveVariables[i];
                    while (liveVariable.Terminal.ParentDiagram != originDiagram)
                    {
                        liveVariable = PullLiveVariableUpToNextHigherDiagram(liveVariable);
                    }
                    liveVariables[i] = liveVariable;
                }

                TerminateLifetimeNode terminateLifetime = CreateNodeFacadesHelpers.CreateTerminateLifetimeWithFacades(originDiagram, inputVariableCount, outputVariableCount);
                foreach (var pair in liveVariables.Zip(terminateLifetime.InputTerminals))
                {
                    // TODO: maybe assert that liveVariable.Terminal is unwired here?
                    LiveVariable liveVariable = pair.Key;
                    Terminal terminateLifetimeInputTerminal = pair.Value;
                    liveVariable.ConnectToTerminalAsInputAndUnifyVariables(terminateLifetimeInputTerminal, _unificationResultFactory);
                    _lifetimeVariableAssociation.MarkVariableConsumed(liveVariable.Variable);
                }
                foreach (var pair in interruptedVariables.Zip(terminateLifetime.OutputTerminals))
                {
                    VariableReference interruptedVariable = pair.Key;
                    Terminal terminateLifetimeOutputTerminal = pair.Value;
                    terminateLifetimeOutputTerminal.GetFacadeVariable().MergeInto(interruptedVariable);
                    _lifetimeVariableAssociation.MarkVariableLive(interruptedVariable, terminateLifetimeOutputTerminal);
                }
            }
        }

        private LiveVariable PullLiveVariableUpToNextHigherDiagram(LiveVariable liveVariable)
        {
            Tunnel outputTunnel = liveVariable.Terminal.ParentDiagram.ParentStructure.CreateTunnel(Direction.Output, TunnelMode.LastValue, PFTypes.Void, PFTypes.Void);
            Terminal inputTerminal = outputTunnel.InputTerminals[0],
                outputTerminal = outputTunnel.OutputTerminals[0];
            CreateNodeFacadesTransform.CreateTunnelNodeFacade(outputTunnel);

            Wire.Create(liveVariable.Terminal.ParentDiagram, liveVariable.Terminal, inputTerminal);
            AutoBorrowNodeFacade.GetNodeFacade(outputTunnel)[inputTerminal].UnifyWithConnectedWireTypeAsNodeInput(
                liveVariable.Variable,
                new TerminalTypeUnificationResults());
            _lifetimeVariableAssociation.MarkVariableConsumed(liveVariable.Variable);

            return new LiveVariable(outputTerminal.GetFacadeVariable(), outputTerminal);
        }
    }
}
