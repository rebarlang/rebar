using System;
using System.Collections.Generic;
using System.Linq;
using NationalInstruments.CommonModel;
using NationalInstruments.Compiler;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using NationalInstruments.MocCommon.SourceModel;
using NationalInstruments.SourceModel;
using NationalInstruments.VI.DfirBuilder;
using Rebar.SourceModel;
using Rebar.Common;
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
            var typePassthrough = node as TypePassthrough;
            if (typePassthrough != null)
            {
                var typePassthroughDfir = new Nodes.FunctionalNode(_currentDiagram, Signatures.ImmutablePassthroughType);
                _map.AddMapping(typePassthrough, typePassthroughDfir);
                _map.MapTerminalsInOrder(typePassthrough, typePassthroughDfir);
                return;
            }
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
            var optionPatternStructure = structure as SourceModel.OptionPatternStructure;
            if (flatSequence != null)
            {
                VisitRebarFlatSequence(flatSequence);
            }
            else if (loop != null)
            {
                VisitLoop(loop);
            }
            else if (optionPatternStructure != null)
            {
                VisitOptionPatternStructure(optionPatternStructure);
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
            _map.AddMapping(firstDiagram, loopDfir.Diagram);

            foreach (BorderNode borderNode in loop.BorderNodes)
            {
                NationalInstruments.Dfir.BorderNode dfirBorderNode = TranslateBorderNode(borderNode, loopDfir);
                MapBorderNode(borderNode, dfirBorderNode);
            }

            firstDiagram.AcceptVisitor(this);
        }

        private void VisitOptionPatternStructure(SourceModel.OptionPatternStructure pattern)
        {
            var patternDfir = new Nodes.OptionPatternStructure(_currentDiagram);
            _map.AddMapping(pattern, patternDfir);
            int diagramIndex = 0;
            foreach (NestedDiagram nestedDiagram in pattern.NestedDiagrams)
            {
                NationalInstruments.Dfir.Diagram dfirDiagram;
                if (diagramIndex == 0)
                {
                    dfirDiagram = patternDfir.Diagrams[0];
                }
                else
                {
                    dfirDiagram = patternDfir.CreateDiagram();
                }
                _map.AddMapping(nestedDiagram, dfirDiagram);
                ++diagramIndex;
            }

            foreach (BorderNode borderNode in pattern.BorderNodes)
            {
                NationalInstruments.Dfir.BorderNode dfirBorderNode = TranslateBorderNode(borderNode, patternDfir);
                MapBorderNode(borderNode, dfirBorderNode);
            }

            foreach (NestedDiagram nestedDiagram in pattern.NestedDiagrams)
            {
                nestedDiagram.AcceptVisitor(this);
            }
        }

        private NationalInstruments.Dfir.BorderNode TranslateBorderNode(BorderNode sourceModelBorderNode, NationalInstruments.Dfir.Structure dfirParentStructure)
        {
            var flatSequenceSimpleTunnel = sourceModelBorderNode as FlatSequenceSimpleTunnel;
            var loopTunnel = sourceModelBorderNode as LoopTunnel;
            var optionPatternStructureTunnel = sourceModelBorderNode as OptionPatternStructureTunnel;
            var borrowTunnel = sourceModelBorderNode as SourceModel.BorrowTunnel;
            var loopBorrowTunnel = sourceModelBorderNode as LoopBorrowTunnel;
            var lockTunnel = sourceModelBorderNode as SourceModel.LockTunnel;
            var loopConditionTunnel = sourceModelBorderNode as SourceModel.LoopConditionTunnel;
            var loopIterateTunnel = sourceModelBorderNode as SourceModel.LoopIterateTunnel;
            var flatSequenceTerminateLifetimeTunnel = sourceModelBorderNode as FlatSequenceTerminateLifetimeTunnel;
            var loopTerminateLifetimeTunnel = sourceModelBorderNode as LoopTerminateLifetimeTunnel;
            var unwrapOptionTunnel = sourceModelBorderNode as SourceModel.UnwrapOptionTunnel;
            var optionPatternStructureSelector = sourceModelBorderNode as SourceModel.OptionPatternStructureSelector;
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
            else if (flatSequenceSimpleTunnel != null || loopTunnel != null || optionPatternStructureTunnel != null)
            {
                return dfirParentStructure.CreateTunnel(
                    sourceModelBorderNode.PrimaryOuterTerminal.Direction,
                    NationalInstruments.Dfir.TunnelMode.LastValue,
                    sourceModelBorderNode.PrimaryOuterTerminal.DataType,
                    sourceModelBorderNode.PrimaryInnerTerminals.First().DataType);
            }
            else if (unwrapOptionTunnel != null)
            {
                return new Nodes.UnwrapOptionTunnel(dfirParentStructure);
            }
            else if (optionPatternStructureSelector != null)
            {
                return ((Nodes.OptionPatternStructure)dfirParentStructure).Selector;
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
                    _map.MapTerminalAndType(terminal, dfirBorderNode.GetOuterTerminal(i));
                    ++i;
                }
                i = 0;
                foreach (var terminal in sourceModelBorderNode.InnerTerminals)
                {
                    // TODO: won't work for border nodes with multiple inner terminals per diagram
                    // also assumes that the border node has the same terminals on each diagram, which
                    // won't be true for the pattern selector
                    // Fortunately, for now, the only inner terminal on OptionPatternStructureSelector is on the first diagram
                    _map.MapTerminalAndType(terminal, dfirBorderNode.GetInnerTerminal(i, 0));
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
            var dfirDataItem = (NationalInstruments.Dfir.DataItem)_map.GetDfirForModel(dataAccessor.DataItem);
            NationalInstruments.Dfir.DataAccessor dfirDataAccessor = NationalInstruments.Dfir.DataAccessor.Create(_currentDiagram, dfirDataItem, dataAccessor.Direction);
            _map.AddMapping(dataAccessor, dfirDataAccessor);
            _map.MapTerminalsInOrder(dataAccessor, dfirDataAccessor);
        }

        public void VisitDataItem(DataItem dataItem)
        {
            NIParameterPassingRule inputParameterPassingRule =
                (dataItem.CallIndex != -1 &&
                dataItem.CallDirection == ParameterCallDirection.Input || dataItem.CallDirection == ParameterCallDirection.Passthrough)
                ? NIParameterPassingRule.Required
                : NIParameterPassingRule.NotAllowed;
            NIParameterPassingRule outputParameterPassingRule =
                (dataItem.CallIndex != -1 &&
                dataItem.CallDirection == ParameterCallDirection.Output || dataItem.CallDirection == ParameterCallDirection.Passthrough)
                ? NIParameterPassingRule.Optional
                : NIParameterPassingRule.NotAllowed;
            NationalInstruments.Dfir.DataItem dfirDataItem = CreatedDfirRoot.CreateDataItem(
                dataItem.Name,
                dataItem.DataType,
                null,
                inputParameterPassingRule,
                outputParameterPassingRule,
                dataItem.CallIndex);
            _map.AddMapping(dataItem, dfirDataItem);
        }

        public void VisitLiteral(ILiteralModel literal)
        {
            NIType dataType = literal.DataType;
            if (dataType.IsRebarReferenceType())
            {
                dataType = dataType.GetUnderlyingTypeFromRebarType();
            }
            NIType constantType;
            if (dataType.IsString() && RebarFeatureToggles.IsStringDataTypeEnabled)
            {
                // Temporary: create a &'static str constant, and wire it through a StringFromSlice node
                // Get rid of this once there is source model support for &str Literals
                Constant stringSliceConstant = Constant.Create(_currentDiagram, literal.Data, DataTypes.StringSliceType.CreateImmutableReference());
                Nodes.FunctionalNode stringFromSliceNode = new Nodes.FunctionalNode(
                    _currentDiagram,
                    Signatures.StringFromSliceType,
                    new[] { RebarFeatureToggles.StringDataType });
                NationalInstruments.Dfir.Wire.Create(_currentDiagram, stringSliceConstant.OutputTerminal, stringFromSliceNode.InputTerminals[0]);

                _map.AddMapping((Content)literal, stringFromSliceNode);
                _map.MapTerminalAndType(literal.OutputTerminal, stringFromSliceNode.OutputTerminals[1]);
                return;
            }

            if (dataType.WireTypeMayFork())
            {
                constantType = dataType;
            }
            else
            {
                constantType = dataType.CreateImmutableReference();
            }
            Constant constant = Constant.Create(_currentDiagram, literal.Data, constantType);
            _map.AddMapping((Content)literal, constant);
            _map.MapTerminalsInOrder((Node)literal, constant);
        }

        public void VisitMethodCall(MocCommonMethodCall callStatic)
        {
            var methodCallDfir = new MethodCallNode(
                _currentDiagram,
                callStatic.SelectedMethodCallTarget.AssociatedEnvoy.GetCompilableDefinitionName(),
                callStatic.Signature);
            _map.AddMapping(callStatic, methodCallDfir);
            _map.MapTerminalsInOrder(callStatic, methodCallDfir);
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
            _map.MapTerminalsInOrder(functionalNode, functionalNodeDfir);
        }

        public void VisitConstructor(Constructor constructor)
        {
            NIType dependencyType = constructor.Type;
            if (dependencyType.IsValueClass() && dependencyType.GetFields().Count() > 0)
            {
                var structConstructorNode = new StructConstructorNode(_currentDiagram, dependencyType);
                _map.AddMapping(constructor, structConstructorNode);
                _map.MapTerminalsInOrder(constructor, structConstructorNode);
                return;
            }
            else
            {
                Constant constant = Constant.CreateConstantWithDefaultValue(_currentDiagram, dependencyType);
                _map.AddMapping(constructor, constant);
                _map.MapTerminalsInOrder(constructor, constant);
            }
        }

        public void VisitDropNode(SourceModel.DropNode dropNode)
        {
            var dropDfir = new Nodes.DropNode(_currentDiagram);
            _map.AddMapping(dropNode, dropDfir);
            _map.MapTerminalsInOrder(dropNode, dropDfir);
        }

        public void VisitTerminateLifetimeNode(TerminateLifetime node)
        {
            var terminateLifetimeDfir = new TerminateLifetimeNode(_currentDiagram, node.InputTerminals.Count(), node.OutputTerminals.Count());
            _map.AddMapping(node, terminateLifetimeDfir);
            _map.MapTerminalsInOrder(node, terminateLifetimeDfir);
        }

        public void VisitImmutableBorrowNode(ImmutableBorrowNode immutableBorrowNode)
        {
            var explicitBorrowDfir = new ExplicitBorrowNode(_currentDiagram, BorrowMode.Immutable, 1, true, true);
            _map.AddMapping(immutableBorrowNode, explicitBorrowDfir);
            _map.MapTerminalsInOrder(immutableBorrowNode, explicitBorrowDfir);
        }

        public void VisitStructFieldAccessor(StructFieldAccessor structFieldAccessor)
        {
            var structFieldAccessorDfir = new StructFieldAccessorNode(
                _currentDiagram,
                structFieldAccessor.FieldTerminals.Select(fieldTerminal => fieldTerminal.FieldName).ToArray());
            _map.AddMapping(structFieldAccessor, structFieldAccessorDfir);
            _map.MapTerminalsInOrder(structFieldAccessor, structFieldAccessorDfir);
        }

        public void VisitFunction(Function function)
        {
            if (CreatedDfirRoot.Name.IsEmpty)
            {
                CreatedDfirRoot.Name = function.ReferencingEnvoy.GetCompilableDefinitionName();
            }

            foreach (DataItem dataItem in function.DataItems)
            {
                dataItem.AcceptVisitor(this);
            }

            _map.AddMapping(function.Diagram, CreatedDfirRoot.BlockDiagram);
            function.Diagram.AcceptVisitor(this);
            ConnectWires();
        }

        /// <summary>
        /// Helper method that creates all the wires on the VI.
        /// Since the other nodes have all been created, this method can look at all the connections on the wire.
        /// </summary>
        private void ConnectWires()
        {
            foreach (Wire wire in _modelWires)
            {
                NationalInstruments.Dfir.Wire dfirWire = _map.TranslateModelWire(wire);
                dfirWire.SetWireBeginsMutableVariable(wire.GetWireBeginsMutableVariable());
            }

            // done with stored wires, set to null to avoid memory leaks
            _modelWires = null;
        }
    }
}
