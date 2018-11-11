using System;
using System.Collections.Generic;
using System.Linq;
using NationalInstruments;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using NationalInstruments.MocCommon.SourceModel;
using NationalInstruments.SourceModel;
using NationalInstruments.Compiler;
using NationalInstruments.VI.DfirBuilder;
using RustyWires.SourceModel;
using RustyWires.Compiler.Nodes;
using BorderNode = NationalInstruments.SourceModel.BorderNode;
using DataAccessor = NationalInstruments.MocCommon.SourceModel.DataAccessor;
using DataItem = NationalInstruments.MocCommon.SourceModel.DataItem;
using Diagram = NationalInstruments.SourceModel.Diagram;
using Node = NationalInstruments.SourceModel.Node;
using PropertyNode = NationalInstruments.MocCommon.SourceModel.PropertyNode;
using Structure = NationalInstruments.SourceModel.Structure;
using Terminal = NationalInstruments.SourceModel.Terminal;
using Tunnel = NationalInstruments.SourceModel.Tunnel;
using Wire = NationalInstruments.SourceModel.Wire;

namespace RustyWires.Compiler
{
    public class RustyWiresDfirBuilder : IRustyWiresFunctionVisitor
    {
        private NationalInstruments.Dfir.Diagram _currentDiagram = null;
        private DfirModelMap _map = new DfirModelMap();
        private List<Wire> _modelWires = new List<Wire>();

        public DfirRoot CreatedDfirRoot { get; }

        internal DfirModelMap DfirModelMap => _map;

        public RustyWiresDfirBuilder()
        {
            CreatedDfirRoot = DfirRoot.Create();
        }

        public void VisitElement(Element element)
        {
            throw new NotImplementedException();
        }

        public void VisitMergeScript(MergeScript mergeScript)
        {
            throw new NotImplementedException();
        }

        public void VisitConfigurableNode(ConfigurableNode configurableNode)
        {
            throw new NotImplementedException();
        }

        public void VisitNode(Node node)
        {
            throw new NotImplementedException();
        }

        public void VisitSourceFile(SourceFileBase sourceFile)
        {
            throw new NotImplementedException();
        }

        public void VisitWire(Wire wire)
        {
            _modelWires.Add(wire);
        }

        public void VisitManhattanWire(ManhattanWire manhattanWire)
        {
            VisitWire(manhattanWire);
        }

        public void VisitStructure(Structure structure)
        {
            var flatSequence = structure as RustyWiresFlatSequence;
            if (flatSequence != null)
            {
                var firstDiagram = structure.NestedDiagrams.First();
                var flatSequenceFrame = Frame.Create(_currentDiagram);
                _map.AddMapping(firstDiagram, flatSequenceFrame.Diagram);
                flatSequenceFrame.Diagram.SetSourceModelId(firstDiagram);

                foreach (BorderNode tunnel in flatSequence.BorderNodes)
                {
                    NationalInstruments.Dfir.BorderNode dfirBorderNode = null;
                    var rustyWiresFlatSequenceSimpleTunnel = tunnel as RustyWiresFlatSequenceSimpleTunnel;
                    var borrowTunnel = tunnel as SourceModel.BorrowTunnel;
                    var unborrowTunnel = tunnel as SourceModel.UnborrowTunnel;
                    var lockTunnel = tunnel as SourceModel.LockTunnel;
                    var unlockTunnel = tunnel as SourceModel.UnlockTunnel;
                    if (borrowTunnel != null)
                    {
                        var borrowDfir = new Nodes.BorrowTunnel(flatSequenceFrame, borrowTunnel.BorrowMode);
                        var unborrowDfir = new Nodes.UnborrowTunnel(flatSequenceFrame);
                        borrowDfir.AssociatedUnborrowTunnel = unborrowDfir;
                        unborrowDfir.AssociatedBorrowTunnel = borrowDfir;
                        dfirBorderNode = borrowDfir;
                        _map.AddMapping(tunnel, dfirBorderNode);
                    }
                    else if (unborrowTunnel != null)
                    {
                        var borrowDfir = (Nodes.BorrowTunnel)_map.GetDfirForModel(unborrowTunnel.BorrowTunnel);
                        dfirBorderNode = borrowDfir.AssociatedUnborrowTunnel;
                        _map.AddMapping(tunnel, dfirBorderNode);
                    }
                    else if (lockTunnel != null)
                    {
                        var lockDfir = new Nodes.LockTunnel(flatSequenceFrame);
                        var unlockDfir = new Nodes.UnlockTunnel(flatSequenceFrame);
                        lockDfir.AssociatedUnlockTunnel = unlockDfir;
                        unlockDfir.AssociatedLockTunnel = lockDfir;
                        dfirBorderNode = lockDfir;
                        _map.AddMapping(tunnel, dfirBorderNode);
                    }
                    else if (unlockTunnel != null)
                    {
                        var lockDfir = (Nodes.LockTunnel)_map.GetDfirForModel(unlockTunnel.LockTunnel);
                        dfirBorderNode = lockDfir.AssociatedUnlockTunnel;
                        _map.AddMapping(tunnel, dfirBorderNode);
                    }
                    else if (rustyWiresFlatSequenceSimpleTunnel != null)
                    {
                        var tunnelDfir = flatSequenceFrame.CreateTunnel(
                            VIDfirBuilder.TranslateDirection(rustyWiresFlatSequenceSimpleTunnel.PrimaryOuterTerminal.Direction),
                            TunnelMode.LastValue,
                            rustyWiresFlatSequenceSimpleTunnel.PrimaryOuterTerminal.DataType,
                            rustyWiresFlatSequenceSimpleTunnel.PrimaryInnerTerminals.First().DataType);
                        dfirBorderNode = tunnelDfir;
                        _map.AddMapping(rustyWiresFlatSequenceSimpleTunnel, dfirBorderNode);
                    }

                    int i = 0;
                    foreach (var terminal in tunnel.OuterTerminals)
                    {
                        MapTerminalAndType(terminal, dfirBorderNode.GetOuterTerminal(i));
                        ++i;
                    }
                    i = 0;
                    foreach (var terminal in tunnel.InnerTerminals)
                    {
                        MapTerminalAndType(terminal, dfirBorderNode.GetInnerTerminal(0, i));
                        ++i;
                    }
                }

                firstDiagram.AcceptVisitor(this);
            }
        }

        public void VisitDiagram(Diagram diagram)
        {
            var savedDiagram = _currentDiagram;
            _currentDiagram = (NationalInstruments.Dfir.Diagram)_map.GetDfirForModel(diagram);

            foreach (var node in diagram.Nodes)
            {
                node.AcceptVisitor(this);
            }

            foreach (var wire in diagram.Wires)
            {
                wire.AcceptVisitor(this);
            }

            _currentDiagram = savedDiagram;
        }

        public void VisitBorderNode(BorderNode borderNode)
        {
            throw new NotImplementedException();
        }

        public void VisitContent(Content content)
        {
            throw new NotImplementedException();
        }

        public void VisitDefinition(Definition definition)
        {
            throw new NotImplementedException();
        }

        public void VisitRootDiagram(RootDiagram rootDiagram)
        {
            VisitDiagram(rootDiagram);
        }

        public void VisitTargetScope(ITargetScope scope)
        {
            throw new NotImplementedException();
        }

        public void VisitNestedDiagram(NestedDiagram nestedDiagram)
        {
            VisitDiagram(nestedDiagram);
        }

        public void VisitWireJoint(WireJoint wireJoint)
        {
        }

        public void VisitManhattanWireJoint(ManhattanWireJoint manhattanWireJoint)
        {
        }

        public void VisitWireSegment(WireSegment wireSegment)
        {
        }

        public void VisitTerminal(Terminal terminal)
        {
            throw new NotImplementedException();
        }

        public void VisitWireable(Wireable wireable)
        {
            throw new NotImplementedException();
        }

        public void VisitConnectable(Connectable connectable)
        {
            throw new NotImplementedException();
        }

        public void VisitWireableTerminal(WireableTerminal wireableTerminal)
        {
            throw new NotImplementedException();
        }

        public void VisitWireTerminal(WireTerminal wireTerminal)
        {
            throw new NotImplementedException();
        }

        public void VisitBorderNodeTerminal(BorderNodeTerminal borderNodeTerminal)
        {
            throw new NotImplementedException();
        }

        public void VisitTunnel(Tunnel tunnel)
        {
            throw new NotImplementedException();
        }

        public void VisitSimpleTunnel(SimpleTunnel simpleTunnel)
        {
            throw new NotImplementedException();
        }

        public void VisitSimpleStructure(SimpleStructure simpleStructure)
        {
            throw new NotImplementedException();
        }

        public void VisitNodeTerminal(NodeTerminal nodeTerminal)
        {
            throw new NotImplementedException();
        }

        public void VisitConfigurableTerminal(ConfigurableTerminal configurableTerminal)
        {
            throw new NotImplementedException();
        }

        public void VisitIcon(Icon icon)
        {
            throw new NotImplementedException();
        }

        public void VisitDiagramDecoration(DiagramDecoration decoration)
        {
            throw new NotImplementedException();
        }

        public ElementVisitorBaseCalling BaseCalling { get; }

        public void VisitDataAccessor(DataAccessor dataAccessor)
        {
            throw new NotImplementedException();
        }

        public void VisitDataItem(DataItem dataItem)
        {
            throw new NotImplementedException();
        }

        public void VisitLiteral(ILiteralModel literal)
        {
            NIType dataType = literal.DataType;
            if (dataType.IsRWReferenceType())
            {
                dataType = dataType.GetUnderlyingTypeFromRustyWiresType();
            }
            Constant constant = Constant.Create(_currentDiagram, literal.Data, dataType.CreateImmutableReference());
            _map.AddMapping((Content)literal, constant);
            _map.AddMapping(literal.OutputTerminal, constant.Terminals.ElementAt(0));
        }

        public void VisitMethodCall(MocCommonMethodCall callStatic)
        {
            throw new NotImplementedException();
        }

        public void VisitPropertyNode(PropertyNode propertyNode)
        {
            throw new NotImplementedException();
        }

        public void VisitFunctionDefinition(FunctionDefinition functionDefinition)
        {
            throw new NotImplementedException();
        }

        public void VisitDataflowFunctionDefinition(DataflowFunctionDefinition definition)
        {
            throw new NotImplementedException();
        }

        public void VisitDropNode(SourceModel.DropNode node)
        {
            var dropDfir = new Nodes.DropNode(_currentDiagram);
            _map.AddMapping(node, dropDfir);
            _map.AddMapping(node.Terminals.ElementAt(0), dropDfir.Terminals.ElementAt(0));
        }

        public void VisitImmutablePassthroughNode(SourceModel.ImmutablePassthroughNode node)
        {
            var immutablePassthroughDfir = new Nodes.ImmutablePassthroughNode(_currentDiagram);
            _map.AddMapping(node, immutablePassthroughDfir);
            _map.AddMapping(node.Terminals.ElementAt(0), immutablePassthroughDfir.Terminals.ElementAt(0));
            _map.AddMapping(node.Terminals.ElementAt(1), immutablePassthroughDfir.Terminals.ElementAt(1));
        }

        public void VisitMutablePassthroughNode(SourceModel.MutablePassthroughNode node)
        {
            var mutablePassthroughDfir = new Nodes.MutablePassthroughNode(_currentDiagram);
            _map.AddMapping(node, mutablePassthroughDfir);
            _map.AddMapping(node.Terminals.ElementAt(0), mutablePassthroughDfir.Terminals.ElementAt(0));
            _map.AddMapping(node.Terminals.ElementAt(1), mutablePassthroughDfir.Terminals.ElementAt(1));
        }

        public void VisitTerminateLifetimeNode(TerminateLifetime node)
        {
            var terminateLifetimeDfir = new TerminateLifetimeNode(_currentDiagram, node.InputTerminals.Count(), node.OutputTerminals.Count());
            _map.AddMapping(node, terminateLifetimeDfir);
            foreach (var pair in node.Terminals.Zip(terminateLifetimeDfir.Terminals))
            {
                _map.AddMapping(pair.Key, pair.Value);
            }
        }

        public void VisitSelectReferenceNode(SourceModel.SelectReferenceNode node)
        {
            var selectReferenceDfir = new Nodes.SelectReferenceNode(_currentDiagram);
            _map.AddMapping(node, selectReferenceDfir);
            _map.AddMapping(node.Terminals.ElementAt(0), selectReferenceDfir.Terminals.ElementAt(0));
            _map.AddMapping(node.Terminals.ElementAt(1), selectReferenceDfir.Terminals.ElementAt(1));
            _map.AddMapping(node.Terminals.ElementAt(2), selectReferenceDfir.Terminals.ElementAt(2));
        }

        public void VisitCreateMutableCopyNode(SourceModel.CreateMutableCopyNode node)
        {
            var createMutableCopyDfir = new Nodes.CreateMutableCopyNode(_currentDiagram);
            _map.AddMapping(node, createMutableCopyDfir);
            _map.AddMapping(node.Terminals.ElementAt(0), createMutableCopyDfir.Terminals.ElementAt(0));
            _map.AddMapping(node.Terminals.ElementAt(1), createMutableCopyDfir.Terminals.ElementAt(1));
            _map.AddMapping(node.Terminals.ElementAt(2), createMutableCopyDfir.Terminals.ElementAt(2));
        }

        public void VisitExchangeValuesNode(ExchangeValues node)
        {
            var exchangeValuesDfir = new ExchangeValuesNode(_currentDiagram);
            _map.AddMapping(node, exchangeValuesDfir);
            _map.AddMapping(node.Terminals.ElementAt(0), exchangeValuesDfir.Terminals.ElementAt(0));
            _map.AddMapping(node.Terminals.ElementAt(1), exchangeValuesDfir.Terminals.ElementAt(1));
            _map.AddMapping(node.Terminals.ElementAt(2), exchangeValuesDfir.Terminals.ElementAt(2));
            _map.AddMapping(node.Terminals.ElementAt(3), exchangeValuesDfir.Terminals.ElementAt(3));
        }

        public void VisitImmutableBorrowNode(ImmutableBorrowNode node)
        {
            var explicitBorrowDfir = new ExplicitBorrowNode(_currentDiagram, BorrowMode.OwnerToImmutable);
            _map.AddMapping(node, explicitBorrowDfir);
            _map.AddMapping(node.Terminals.ElementAt(0), explicitBorrowDfir.Terminals.ElementAt(0));
            _map.AddMapping(node.Terminals.ElementAt(1), explicitBorrowDfir.Terminals.ElementAt(1));
        }

        public void VisitPureUnaryPrimitive(SourceModel.PureUnaryPrimitive node)
        {
            var pureUnaryPrimitiveDfir = new Nodes.PureUnaryPrimitive(_currentDiagram, node.Operation);
            _map.AddMapping(node, pureUnaryPrimitiveDfir);
            _map.AddMapping(node.Terminals.ElementAt(0), pureUnaryPrimitiveDfir.Terminals.ElementAt(0));
            _map.AddMapping(node.Terminals.ElementAt(1), pureUnaryPrimitiveDfir.Terminals.ElementAt(1));
            _map.AddMapping(node.Terminals.ElementAt(2), pureUnaryPrimitiveDfir.Terminals.ElementAt(2));
        }

        public void VisitPureBinaryPrimitive(SourceModel.PureBinaryPrimitive node)
        {
            var pureBinaryPrimitiveDfir = new Nodes.PureBinaryPrimitive(_currentDiagram, node.Operation);
            _map.AddMapping(node, pureBinaryPrimitiveDfir);
            _map.AddMapping(node.Terminals.ElementAt(0), pureBinaryPrimitiveDfir.Terminals.ElementAt(0));
            _map.AddMapping(node.Terminals.ElementAt(1), pureBinaryPrimitiveDfir.Terminals.ElementAt(1));
            _map.AddMapping(node.Terminals.ElementAt(2), pureBinaryPrimitiveDfir.Terminals.ElementAt(2));
            _map.AddMapping(node.Terminals.ElementAt(3), pureBinaryPrimitiveDfir.Terminals.ElementAt(3));
            _map.AddMapping(node.Terminals.ElementAt(4), pureBinaryPrimitiveDfir.Terminals.ElementAt(4));
        }

        public void VisitMutatingUnaryPrimitive(SourceModel.MutatingUnaryPrimitive node)
        {
            var mutatingUnaryPrimitiveDfir = new Nodes.MutatingUnaryPrimitive(_currentDiagram, node.Operation);
            _map.AddMapping(node, mutatingUnaryPrimitiveDfir);
            _map.AddMapping(node.Terminals.ElementAt(0), mutatingUnaryPrimitiveDfir.Terminals.ElementAt(0));
            _map.AddMapping(node.Terminals.ElementAt(1), mutatingUnaryPrimitiveDfir.Terminals.ElementAt(1));
        }

        public void VisitMutatingBinaryPrimitive(SourceModel.MutatingBinaryPrimitive node)
        {
            var mutatingBinaryPrimitiveDfir = new Nodes.MutatingBinaryPrimitive(_currentDiagram, node.Operation);
            _map.AddMapping(node, mutatingBinaryPrimitiveDfir);
            _map.AddMapping(node.Terminals.ElementAt(0), mutatingBinaryPrimitiveDfir.Terminals.ElementAt(0));
            _map.AddMapping(node.Terminals.ElementAt(1), mutatingBinaryPrimitiveDfir.Terminals.ElementAt(1));
            _map.AddMapping(node.Terminals.ElementAt(2), mutatingBinaryPrimitiveDfir.Terminals.ElementAt(2));
            _map.AddMapping(node.Terminals.ElementAt(3), mutatingBinaryPrimitiveDfir.Terminals.ElementAt(3));
        }

        public void VisitFreezeNode(Freeze node)
        {
            var freezeDfir = new FreezeNode(_currentDiagram);
            _map.AddMapping(node, freezeDfir);
            _map.AddMapping(node.Terminals.ElementAt(0), freezeDfir.Terminals.ElementAt(0));
            _map.AddMapping(node.Terminals.ElementAt(1), freezeDfir.Terminals.ElementAt(1));
        }

        public void VisitCreateCellNode(CreateCell node)
        {
            var createCellDfir = new CreateCellNode(_currentDiagram);
            _map.AddMapping(node, createCellDfir);
            _map.AddMapping(node.Terminals.ElementAt(0), createCellDfir.Terminals.ElementAt(0));
            _map.AddMapping(node.Terminals.ElementAt(1), createCellDfir.Terminals.ElementAt(1));
        }

        public void VisitRustyWiresFunction(RustyWiresFunction function)
        {
            if (CreatedDfirRoot.Name.IsEmpty)
            {
                CreatedDfirRoot.Name = function.ReferencingEnvoy.CreateExtendedQualifiedName();
            }

            _map.AddMapping(function.Diagram, CreatedDfirRoot.BlockDiagram);
            function.Diagram.AcceptVisitor(this);
            ConnectWires();
        }

        private void MapTerminalAndType(NationalInstruments.SourceModel.Terminal modelTerminal,
            NationalInstruments.Dfir.Terminal dfirTerminal)
        {
            _map.AddMapping(modelTerminal, dfirTerminal);
            dfirTerminal.SetSourceModelId(modelTerminal);
            dfirTerminal.DataType = modelTerminal.DataType.IsUnset() ? PFTypes.Void : modelTerminal.DataType;
        }

        /// <summary>
        /// Helper method that creates all the wires on the VI.
        /// Since the other nodes have all been created, this method can look at all the connections on the wire.
        /// </summary>
        private void ConnectWires()
        {
            foreach (NationalInstruments.SourceModel.Wire wire in _modelWires)
            {
                var connectedDfirTerminals = new List<NationalInstruments.Dfir.Terminal>();
                var looseEnds = new List<NationalInstruments.SourceModel.Terminal>();
                foreach (NationalInstruments.SourceModel.Terminal terminal in wire.Terminals)
                {
                    if (terminal.ConnectedTerminal != null)
                    {
                        connectedDfirTerminals.Add(_map.GetDfirForTerminal(terminal.ConnectedTerminal));
                    }
                    else
                    {
                        looseEnds.Add(terminal);
                    }
                }

                var parentDiagram = (NationalInstruments.Dfir.Diagram)_map.GetDfirForModel(wire.Owner);
                NationalInstruments.Dfir.Wire dfirWire = NationalInstruments.Dfir.Wire.Create(parentDiagram, connectedDfirTerminals);
                _map.AddMapping(wire, dfirWire);
                int i = 0;
                // Map connected model wire terminals
                foreach (NationalInstruments.SourceModel.Terminal terminal in wire.Terminals.Where(t => t.ConnectedTerminal != null))
                {
                    MapTerminalAndType(terminal, dfirWire.Terminals[i]);
                    i++;
                }
                // Map unconnected model wire terminals
                foreach (NationalInstruments.SourceModel.Terminal terminal in looseEnds)
                {
                    NationalInstruments.Dfir.Terminal dfirTerminal = dfirWire.CreateBranch();
                    MapTerminalAndType(terminal, dfirTerminal);
                }
                // "Map" loose ends with no terminals in the model
                int numberOfLooseEndsInModel = wire.Joints.Count(j => j.Dangling);
                for (int looseEndsIndex = 0; looseEndsIndex < numberOfLooseEndsInModel; ++looseEndsIndex)
                {
                    NationalInstruments.Dfir.Terminal dfirTerminal = dfirWire.CreateBranch();
                    dfirTerminal.DataType = PFTypes.Void;
                }

                // Now split the wire up into multiple wires if there are multiple sinks
                if (dfirWire.SinkTerminals.Count > 1)
                {

                }
            }

            // done with stored wires, set to null to avoid memory leaks
            _modelWires = null;
        }
    }
}
