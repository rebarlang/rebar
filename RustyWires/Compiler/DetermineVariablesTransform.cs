using System.Collections.Generic;
using System.Linq;
using NationalInstruments.Dfir;
using RustyWires.Compiler.Nodes;

namespace RustyWires.Compiler
{
    internal class DetermineVariablesTransform : VisitorTransformBase
    {
        private VariableSet CurrentDiagramVariableSet { get; set; }

        protected override void VisitDiagram(Diagram diagram)
        {
            diagram.SetVariableSet(new VariableSet());
        }

        protected override void VisitWire(Wire wire)
        {
            Terminal connectedTerminal = wire.SourceTerminal.ConnectedTerminal;
            if (connectedTerminal != null)
            {
                Variable sourceVariable = connectedTerminal.GetVariable();
                wire.SourceTerminal.AddTerminalToVariable(sourceVariable);
                bool reuseSource = true;
                foreach (Terminal sinkTerminal in wire.SinkTerminals)
                {
                    if (reuseSource)
                    {
                        sinkTerminal.AddTerminalToVariable(sourceVariable);
                        reuseSource = false;
                    }
                    else
                    {
                        sinkTerminal.AddTerminalToNewVariable();
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
                nonPassthroughOutput.AddTerminalToNewVariable();
            }
        }

        private Variable PullInputTerminalVariable(Terminal inputTerminal)
        {
            var connectedTerminal = inputTerminal.ConnectedTerminal;
            if (connectedTerminal != null)
            {
                Variable variable = connectedTerminal.GetVariable();
                inputTerminal.AddTerminalToVariable(variable);
                return variable;
            }
            return null;
        }

        private void LinkPassthroughTerminalPair(Terminal inputTerminal, Terminal outputTerminal)
        {
            Variable inputVariable = PullInputTerminalVariable(inputTerminal);
            if (inputVariable != null)
            {
                outputTerminal.AddTerminalToVariable(inputVariable);
            }
            else
            {
                // the input on a passthrough is not wired, but the output is
                // for now, create a new variable for the output
                outputTerminal.AddTerminalToNewVariable();
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
                Terminal inputTerminal = simpleTunnel.Direction == Direction.Input ? simpleTunnel.GetOuterTerminal() : simpleTunnel.GetInnerTerminal();
                Terminal outputTerminal = simpleTunnel.Direction == Direction.Input ? simpleTunnel.GetInnerTerminal() : simpleTunnel.GetOuterTerminal();
                PullInputTerminalVariable(inputTerminal);
                outputTerminal.AddTerminalToNewVariable();
            }
            else if (borrowTunnel != null || lockTunnel != null)
            {
                PullInputTerminalVariable(borderNode.GetOuterTerminal(0));
                borderNode.GetInnerTerminal(0, 0).AddTerminalToNewVariable();
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
