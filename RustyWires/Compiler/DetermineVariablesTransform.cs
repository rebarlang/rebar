using System.Collections.Generic;
using System.Linq;
using NationalInstruments.Dfir;
using RustyWires.Compiler.Nodes;

namespace RustyWires.Compiler
{
    internal class DetermineVariablesTransform : VisitorTransformBase
    {
        private readonly VariableSet _variableSet = new VariableSet();

        protected override void VisitDiagram(Diagram diagram)
        {
            diagram.DfirRoot.SetVariableSet(_variableSet);
        }

        protected override void VisitWire(Wire wire)
        {
            Terminal sourceTerminal = wire.SourceTerminal;
            Terminal connectedTerminal = sourceTerminal.ConnectedTerminal;
            if (connectedTerminal != null)
            {
                Variable sourceVariable = _variableSet.GetVariableForTerminal(connectedTerminal);
                _variableSet.AddTerminalToVariable(sourceVariable, wire.SourceTerminal);
                bool reuseSource = true;
                foreach (Terminal sinkTerminal in wire.SinkTerminals)
                {
                    if (reuseSource)
                    {
                        _variableSet.AddTerminalToVariable(sourceVariable, sinkTerminal);
                        reuseSource = false;
                    }
                    else
                    {
                        _variableSet.AddTerminalToNewVariable(sinkTerminal);
                    }
                }
            }
            else
            {
                // source is not connected somehow; not sure what to do here
            }
        }

        protected override void VisitNode(Node node)
        {
            List<Terminal> nonPassthroughInputs = new List<Terminal>(node.InputTerminals), nonPassthroughOutputs = new List<Terminal>(node.OutputTerminals);
            var passthroughTerminalsNode = node as IPassthroughTerminalsNode;
            var passthroughTerminalPairs = passthroughTerminalsNode != null
                ? passthroughTerminalsNode.PassthroughTerminalPairs
                : Enumerable.Empty<PassthroughTerminalPair>();
            foreach (var pair in passthroughTerminalPairs)
            {
                nonPassthroughInputs.Remove(pair.InputTerminal);
                nonPassthroughOutputs.Remove(pair.OutputTerminal);
            }

            // TODO: if there are any non-passthrough inputs with reference types that are connected to wires with owned values, create a frame
            // with borrow tunnels, then update passthroughTerminalPairs
            foreach (var pair in passthroughTerminalPairs)
            {
                LinkPassthroughTerminalPair(pair.InputTerminal, pair.OutputTerminal);
            }

            foreach (var nonPassthroughInput in nonPassthroughInputs)
            {
                PullInputTerminalVariable(nonPassthroughInput);
            }

            foreach (var nonPassthroughOutput in nonPassthroughOutputs)
            {
                _variableSet.AddTerminalToNewVariable(nonPassthroughOutput);
            }
        }

        private Variable PullInputTerminalVariable(Terminal inputTerminal)
        {
            var connectedTerminal = inputTerminal.ConnectedTerminal;
            if (connectedTerminal != null)
            {
                Variable variable = _variableSet.GetVariableForTerminal(connectedTerminal);
                _variableSet.AddTerminalToVariable(variable, inputTerminal);
                return variable;
            }
            return null;
        }

        private void LinkPassthroughTerminalPair(Terminal inputTerminal, Terminal outputTerminal)
        {
            var inputVariable = PullInputTerminalVariable(inputTerminal);
            if (inputVariable != null)
            {
                _variableSet.AddTerminalToVariable(inputVariable, outputTerminal);
            }
            else
            {
                // the input on a passthrough is not wired, but the output is
                // for now, create a new variable for the output
                _variableSet.AddTerminalToNewVariable(outputTerminal);
            }
        }

        protected override void VisitBorderNode(BorderNode borderNode)
        {
            var simpleTunnel = borderNode as Tunnel;
            var borrowTunnel = borderNode as BorrowTunnel;
            var lockTunnel = borderNode as LockTunnel;
            var unborrowTunnel = borderNode as UnborrowTunnel;
            var unlockTunnel = borderNode as UnlockTunnel;
            if (simpleTunnel != null)
            {
                // for now, treat the wires inside and outside the structure as the same variable, since the tunnel is a move
                if (simpleTunnel.Direction == Direction.Input)
                {
                    LinkPassthroughTerminalPair(simpleTunnel.GetOuterTerminal(), simpleTunnel.GetInnerTerminal());
                }
                else
                {
                    LinkPassthroughTerminalPair(simpleTunnel.GetInnerTerminal(), simpleTunnel.GetOuterTerminal());
                }
            }
            else if (borrowTunnel != null || lockTunnel != null)
            {
                Terminal innerTerminal = borderNode.GetInnerTerminal(0, 0);
                _variableSet.AddTerminalToNewVariable(innerTerminal);
            }
            else if (unborrowTunnel != null)
            {
                LinkPassthroughTerminalPair(unborrowTunnel.AssociatedBorrowTunnel.GetOuterTerminal(0), unborrowTunnel.GetOuterTerminal(0));
            }
            else if (unlockTunnel != null)
            {
                LinkPassthroughTerminalPair(unlockTunnel.AssociatedLockTunnel.GetOuterTerminal(0), unlockTunnel.GetOuterTerminal(0));
            }
        }
    }
}
