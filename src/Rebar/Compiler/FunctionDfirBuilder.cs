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
using Rebar.SourceModel;
using Rebar.Compiler.Nodes;
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
using Rebar.Common;

namespace Rebar.Compiler
{
    public class FunctionDfirBuilder : IFunctionVisitor
    {
        private NationalInstruments.Dfir.Diagram _currentDiagram = null;
        private DfirModelMap _map = new DfirModelMap();
        private List<Wire> _modelWires = new List<Wire>();

        public DfirRoot CreatedDfirRoot { get; }

        internal DfirModelMap DfirModelMap => _map;

        public FunctionDfirBuilder()
        {
            CreatedDfirRoot = DfirRoot.Create();
            CreatedDfirRoot.RuntimeType = RebarFeatureToggles.IsRebarTargetEnabled
                ? FunctionMocPlugin.FunctionRuntimeType
                : DfirRootRuntimeType.FunctionType;
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
            var flatSequence = structure as FlatSequence;
            var loop = structure as SourceModel.Loop;
            if (flatSequence != null)
            {
                VisitRebarFlatSequence(flatSequence);
            }
            else if (loop != null)
            {
                VisitLoop(loop);
            }
        }

        private void VisitRebarFlatSequence(FlatSequence flatSequence)
        {
            var firstDiagram = flatSequence.NestedDiagrams.First();
            var flatSequenceDfir = Frame.Create(_currentDiagram);
            _map.AddMapping(flatSequence, flatSequenceDfir);
            _map.AddMapping(firstDiagram, flatSequenceDfir.Diagram);

            foreach (BorderNode borderNode in flatSequence.BorderNodes)
            {
                NationalInstruments.Dfir.BorderNode dfirBorderNode = TranslateBorderNode(borderNode, flatSequenceDfir);
                MapBorderNode(borderNode, dfirBorderNode);
            }

            firstDiagram.AcceptVisitor(this);
        }

        private void VisitLoop(SourceModel.Loop loop)
        {
            var firstDiagram = loop.NestedDiagrams.First();
            var loopDfir = new Nodes.Loop(_currentDiagram);
            _map.AddMapping(loop, loopDfir);
            _map.AddMapping(firstDiagram, loopDfir.Diagrams[0]);

            foreach (BorderNode borderNode in loop.BorderNodes)
            {
                NationalInstruments.Dfir.BorderNode dfirBorderNode = TranslateBorderNode(borderNode, loopDfir);
                MapBorderNode(borderNode, dfirBorderNode);
            }

            firstDiagram.AcceptVisitor(this);
        }

        private NationalInstruments.Dfir.BorderNode TranslateBorderNode(BorderNode sourceModelBorderNode, NationalInstruments.Dfir.Structure dfirParentStructure)
        {
            var flatSequenceSimpleTunnel = sourceModelBorderNode as FlatSequenceSimpleTunnel;
            var loopTunnel = sourceModelBorderNode as LoopTunnel;
            var borrowTunnel = sourceModelBorderNode as SourceModel.BorrowTunnel;
            var loopBorrowTunnel = sourceModelBorderNode as LoopBorrowTunnel;
            var lockTunnel = sourceModelBorderNode as SourceModel.LockTunnel;
            var loopConditionTunnel = sourceModelBorderNode as SourceModel.LoopConditionTunnel;
            var loopIterateTunnel = sourceModelBorderNode as SourceModel.LoopIterateTunnel;
            var flatSequenceTerminateLifetimeTunnel = sourceModelBorderNode as FlatSequenceTerminateLifetimeTunnel;
            var loopTerminateLifetimeTunnel = sourceModelBorderNode as LoopTerminateLifetimeTunnel;
            var unwrapOptionTunnel = sourceModelBorderNode as SourceModel.UnwrapOptionTunnel;
            if (borrowTunnel != null)
            {
                var borrowDfir = new Nodes.BorrowTunnel(dfirParentStructure, borrowTunnel.BorrowMode);
                CreateTerminateLifetimeTunnel(borrowDfir, dfirParentStructure);
                return borrowDfir;
            }
            else if (loopBorrowTunnel != null)
            {
                var borrowDfir = new Nodes.BorrowTunnel(dfirParentStructure, loopBorrowTunnel.BorrowMode);
                CreateTerminateLifetimeTunnel(borrowDfir, dfirParentStructure);
                return borrowDfir;
            }
            else if (lockTunnel != null)
            {
                var lockDfir = new Nodes.LockTunnel(dfirParentStructure);
                CreateTerminateLifetimeTunnel(lockDfir, dfirParentStructure);
                return lockDfir;
            }
            else if (loopConditionTunnel != null)
            {
                var loopConditionDfir = new Nodes.LoopConditionTunnel((Nodes.Loop)dfirParentStructure);
                CreateTerminateLifetimeTunnel(loopConditionDfir, dfirParentStructure);
                return loopConditionDfir;
            }
            else if (loopIterateTunnel != null)
            {
                var loopIterateDfir = new IterateTunnel(dfirParentStructure);
                CreateTerminateLifetimeTunnel(loopIterateDfir, dfirParentStructure);
                return loopIterateDfir;
            }
            else if (flatSequenceTerminateLifetimeTunnel != null)
            {
                var beginLifetimeDfir = (Nodes.IBeginLifetimeTunnel)_map.GetDfirForModel((Element)flatSequenceTerminateLifetimeTunnel.BeginLifetimeTunnel);
                return beginLifetimeDfir.TerminateLifetimeTunnel;
            }
            else if (loopTerminateLifetimeTunnel != null)
            {
                var beginLifetimeDfir = (Nodes.IBeginLifetimeTunnel)_map.GetDfirForModel((Element)loopTerminateLifetimeTunnel.BeginLifetimeTunnel);
                return beginLifetimeDfir.TerminateLifetimeTunnel;
            }
            else if (flatSequenceSimpleTunnel != null || loopTunnel != null)
            {
                return dfirParentStructure.CreateTunnel(
                    VIDfirBuilder.TranslateDirection(sourceModelBorderNode.PrimaryOuterTerminal.Direction),
                    NationalInstruments.Dfir.TunnelMode.LastValue,
                    sourceModelBorderNode.PrimaryOuterTerminal.DataType,
                    sourceModelBorderNode.PrimaryInnerTerminals.First().DataType);
            }
            else if (unwrapOptionTunnel != null)
            {
                return new Nodes.UnwrapOptionTunnel(dfirParentStructure);
            }
            throw new NotImplementedException("Unknown BorderNode type: " + sourceModelBorderNode.GetType().Name);
        }

        private void CreateTerminateLifetimeTunnel(Nodes.IBeginLifetimeTunnel beginLifetimeTunnel, NationalInstruments.Dfir.Structure dfirParentStructure)
        {
            var terminateLifetimeDfir = new TerminateLifetimeTunnel(dfirParentStructure);
            beginLifetimeTunnel.TerminateLifetimeTunnel = terminateLifetimeDfir;
            terminateLifetimeDfir.BeginLifetimeTunnel = beginLifetimeTunnel;
        }

        private void MapBorderNode(BorderNode sourceModelBorderNode, NationalInstruments.Dfir.BorderNode dfirBorderNode)
        {
            if (dfirBorderNode != null)
            {
                _map.AddMapping(sourceModelBorderNode, dfirBorderNode);
                int i = 0;
                foreach (var terminal in sourceModelBorderNode.OuterTerminals)
                {
                    MapTerminalAndType(terminal, dfirBorderNode.GetOuterTerminal(i));
                    ++i;
                }
                i = 0;
                foreach (var terminal in sourceModelBorderNode.InnerTerminals)
                {
                    MapTerminalAndType(terminal, dfirBorderNode.GetInnerTerminal(0, i));
                    ++i;
                }
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

        public void VisitRootDiagram(NationalInstruments.SourceModel.RootDiagram rootDiagram)
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
            VisitStructure(simpleStructure);
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
            if (dataType.IsRebarReferenceType())
            {
                dataType = dataType.GetUnderlyingTypeFromRebarType();
            }
            NIType constantType = dataType.WireTypeMayFork()
                ? dataType
                : dataType.CreateImmutableReference();
            Constant constant = Constant.Create(_currentDiagram, literal.Data, constantType);
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

        public void VisitFunctionalNode(SourceModel.FunctionalNode functionalNode)
        {
            var functionalNodeDfir = new Nodes.FunctionalNode(_currentDiagram, functionalNode.Signature, functionalNode.RequiredFeatureToggles);
            _map.AddMapping(functionalNode, functionalNodeDfir);
            MapTerminalsInOrder(functionalNode, functionalNodeDfir);
        }

        public void VisitDropNode(SourceModel.DropNode dropNode)
        {
            var dropDfir = new Nodes.DropNode(_currentDiagram);
            _map.AddMapping(dropNode, dropDfir);
            MapTerminalsInOrder(dropNode, dropDfir);
        }

        public void VisitTerminateLifetimeNode(TerminateLifetime node)
        {
            var terminateLifetimeDfir = new TerminateLifetimeNode(_currentDiagram, node.InputTerminals.Count(), node.OutputTerminals.Count());
            _map.AddMapping(node, terminateLifetimeDfir);
            MapTerminalsInOrder(node, terminateLifetimeDfir);
        }

        public void VisitImmutableBorrowNode(ImmutableBorrowNode immutableBorrowNode)
        {
            var explicitBorrowDfir = new ExplicitBorrowNode(_currentDiagram, BorrowMode.Immutable, 1, true, true);
            _map.AddMapping(immutableBorrowNode, explicitBorrowDfir);
            MapTerminalsInOrder(immutableBorrowNode, explicitBorrowDfir);
        }

        public void VisitFunction(Function function)
        {
            if (CreatedDfirRoot.Name.IsEmpty)
            {
                CreatedDfirRoot.Name = function.ReferencingEnvoy.CreateExtendedQualifiedName();
            }

            _map.AddMapping(function.Diagram, CreatedDfirRoot.BlockDiagram);
            function.Diagram.AcceptVisitor(this);
            ConnectWires();
        }

        private void MapTerminalAndType(Terminal modelTerminal, NationalInstruments.Dfir.Terminal dfirTerminal)
        {
            _map.AddMapping(modelTerminal, dfirTerminal);
            dfirTerminal.SetSourceModelId(modelTerminal);
            dfirTerminal.DataType = modelTerminal.DataType.IsUnset() ? PFTypes.Void : modelTerminal.DataType;
        }

        private void MapTerminalsInOrder(Node sourceModelNode, NationalInstruments.Dfir.Node dfirNode)
        {
            foreach (var pair in sourceModelNode.Terminals.Zip(dfirNode.Terminals))
            {
                _map.AddMapping(pair.Key, pair.Value);
            }
        }

        /// <summary>
        /// Helper method that creates all the wires on the VI.
        /// Since the other nodes have all been created, this method can look at all the connections on the wire.
        /// </summary>
        private void ConnectWires()
        {
            foreach (Wire wire in _modelWires)
            {
                var connectedDfirTerminals = new List<NationalInstruments.Dfir.Terminal>();
                var looseEnds = new List<Terminal>();
                foreach (Terminal terminal in wire.Terminals)
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
                dfirWire.SetWireBeginsMutableVariable(wire.GetWireBeginsMutableVariable());
                int i = 0;
                // Map connected model wire terminals
                foreach (Terminal terminal in wire.Terminals.Where(t => t.ConnectedTerminal != null))
                {
                    MapTerminalAndType(terminal, dfirWire.Terminals[i]);
                    i++;
                }
                // Map unconnected model wire terminals
                foreach (Terminal terminal in looseEnds)
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
