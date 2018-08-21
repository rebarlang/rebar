using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NationalInstruments.Compiler;
using NationalInstruments.Compiler.SemanticAnalysis;
using NationalInstruments.Dfir;

namespace RustyWires.Compiler
{
    internal class AssociateValuesAndReferencesTransform : IDfirTransform
    {
        private struct WireSet
        {
            public bool IsOwned { get; }

            public int Id { get; }

            public List<Wire> Wires { get; }

            public WireSet(int id, bool isOwned)
            {
                Id = id;
                IsOwned = isOwned;
                Wires = new List<Wire>();
            }
        }

        private readonly Dictionary<Wire, WireSet> _wireSets = new Dictionary<Wire, WireSet>();
        private readonly Dictionary<Terminal, WireSet> _terminalWireSets = new Dictionary<Terminal, WireSet>();
        private int _setCount = 0;

        private WireSet CreateNewWireSet(bool isOwnedValue)
        {
            return new WireSet(_setCount++, true);
        }

        private WireSet AddWireToNewValueSet(Wire wire)
        {
            WireSet set = CreateNewWireSet(true);
            set.Wires.Add(wire);
            _wireSets[wire] = set;
            return set;
        }

        private WireSet AddTerminalToNewValueSet(Terminal terminal)
        {
            WireSet set = CreateNewWireSet(true);
            _terminalWireSets[terminal] = set;
            return set;
        }

        private WireSet AddTerminalToNewReferenceSet(Terminal terminal)
        {
            WireSet set = CreateNewWireSet(false);
            _terminalWireSets[terminal] = set;
            return set;
        }

        public void Execute(DfirRoot dfirRoot, CompileCancellationToken cancellationToken)
        {
            // Assumes that all wires have exactly one source and one sink.
            VisitDiagram(dfirRoot.BlockDiagram);
        }

        private void VisitDiagram(Diagram diagram)
        {
            foreach (var node in diagram.Nodes.ToList())
            {
                var structure = node as Structure;
                var wire = node as Wire;
                if (structure != null)
                {
                    VisitStructure(structure);
                }
                else if (wire != null)
                {
                    VisitWire(wire);
                }
                else
                {
                    VisitNode(node);
                }
            }
        }

        private void VisitWire(Wire wire)
        {
            Terminal sourceTerminal = wire.SourceTerminal;
            Terminal connectedTerminal = sourceTerminal.ConnectedTerminal;
            if (connectedTerminal != null)
            {
                WireSet wireSet = _terminalWireSets[connectedTerminal];
                wireSet.Wires.Add(wire);
                _wireSets[wire] = wireSet;
            }
            else
            {
                // source is not connected somehow; not sure what to do here
            }
        }

        private void VisitNode(Node node)
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
                var inputWire = pair.InputTerminal.GetWireIfConnected();
                var outputWire = pair.OutputTerminal.GetWireIfConnected();
                if (inputWire != null)
                {
                    WireSet wireSet = _wireSets[inputWire];
                    _terminalWireSets[pair.InputTerminal] = wireSet;
                    _terminalWireSets[pair.OutputTerminal] = wireSet;
                    if (outputWire == null)
                    {
                        // the output is not wired; we could create a DropNode here
                    }
                }
                else if (outputWire != null)
                {
                    // the input on a passthrough is not wired, but the output is
                    // for now, create a new reference wire set for the output
                    AddTerminalToNewReferenceSet(pair.OutputTerminal);
                }
            }

            foreach (var nonPassthroughOutput in nonPassthroughOutputs)
            {
                bool isOwnedValue = nonPassthroughOutput.DataType.GetTypePermissiveness() == TypePermissiveness.Owner;
                if (isOwnedValue)
                {
                    AddTerminalToNewValueSet(nonPassthroughOutput);
                    if (nonPassthroughOutput.GetWireIfConnected() == null)
                    {
                        // the output is not wired; we could create a DropNode here
                    }
                }
                else
                {
                    AddTerminalToNewReferenceSet(nonPassthroughOutput);
                }
            }
        }

        private void VisitStructure(Structure structure)
        {
            // throw new NotImplementedException();
        }
    }
}
