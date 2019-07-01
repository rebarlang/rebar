using System.Collections.Generic;
using System.Linq;
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

        public InsertTerminateLifetimeTransform(LifetimeVariableAssociation lifetimeVariableAssociation)
        {
            _lifetimeVariableAssociation = lifetimeVariableAssociation;
        }

        public void Execute(DfirRoot dfirRoot, CompileCancellationToken cancellationToken)
        {
            var lifetimeGraphTree = dfirRoot.GetLifetimeGraphTree();
            BoundedLifetimeLiveVariableSet boundedLifetimeLiveVariableSet;
            while (_lifetimeVariableAssociation.TryGetBoundedLifetimeWithLiveVariables(out boundedLifetimeLiveVariableSet))
            {
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

                TerminateLifetimeNode terminateLifetime = TerminateLifetimeNodeHelpers.CreateTerminateLifetimeWithFacades(originDiagram, inputVariableCount, outputVariableCount);
                int inputIndex = 0;
                foreach (LiveVariable liveVariable in liveVariables)
                {
                    // TODO: maybe assert that liveVariable.Terminal is unwired here?
                    Terminal terminateLifetimeInputTerminal = terminateLifetime.InputTerminals[inputIndex];
                    Wire.Create(originDiagram, liveVariable.Terminal, terminateLifetimeInputTerminal);
                    terminateLifetimeInputTerminal.GetFacadeVariable().MergeInto(liveVariable.Variable);
                    _lifetimeVariableAssociation.MarkVariableConsumed(liveVariable.Variable);

                    ++inputIndex;
                }
                int outputIndex = 0;
                foreach (VariableReference interruptedVariable in interruptedVariables)
                {
                    Terminal terminateLifetimeOutputTerminal = terminateLifetime.OutputTerminals[outputIndex];
                    terminateLifetimeOutputTerminal.GetFacadeVariable().MergeInto(interruptedVariable);
                    _lifetimeVariableAssociation.MarkVariableLive(interruptedVariable, terminateLifetimeOutputTerminal);

                    ++outputIndex;
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
            inputTerminal.GetFacadeVariable().MergeInto(liveVariable.Variable);
            _lifetimeVariableAssociation.MarkVariableConsumed(liveVariable.Variable);

            return new LiveVariable(outputTerminal.GetFacadeVariable(), outputTerminal);
        }
    }
}
