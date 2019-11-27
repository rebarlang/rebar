using System;
using System.Linq;
using NationalInstruments.Dfir;
using NationalInstruments.SourceModel;
using Rebar.SourceModel.TypeDiagram;
using DfirDiagram = NationalInstruments.Dfir.Diagram;
using SMNode = NationalInstruments.SourceModel.Node;
using SMWire = NationalInstruments.SourceModel.Wire;

namespace Rebar.Compiler.TypeDiagram
{
    internal class TypeDiagramDfirBuilder
    {
        private DfirDiagram _currentDiagram;

        public TypeDiagramDfirBuilder()
        {
            TypeDiagramDfirRoot = DfirRoot.Create();
            TypeDiagramDfirRoot.RuntimeType = TypeDiagramMocPlugin.TypeDiagramRuntimeType;
        }

        public DfirRoot TypeDiagramDfirRoot { get; }

        public DfirModelMap DfirModelMap { get; } = new DfirModelMap();

        public void VisitTypeDiagram(NationalInstruments.SourceModel.RootDiagram rootDiagram)
        {
            if (TypeDiagramDfirRoot.Name.IsEmpty)
            {
                TypeDiagramDfirRoot.Name = rootDiagram.Definition.ReferencingEnvoy.CreateExtendedQualifiedName();
            }

            var rootDfirDiagram = TypeDiagramDfirRoot.BlockDiagram;
            DfirModelMap.AddMapping(rootDiagram, rootDfirDiagram);

            var savedDiagram = _currentDiagram;
            _currentDiagram = rootDfirDiagram;

            foreach (var node in rootDiagram.Nodes)
            {
                VisitNode(node);
            }

            foreach (var wire in rootDiagram.Wires)
            {
                VisitWire(wire);
            }

            _currentDiagram = savedDiagram;
        }

        private void VisitNode(SMNode node)
        {
            var selfTypeNode = node as SelfType;
            var primitiveTypeNode = node as PrimitiveType;
            if (selfTypeNode != null)
            {
                var selfTypeDfirNode = new Nodes.SelfTypeNode(_currentDiagram);
                DfirModelMap.AddMapping(selfTypeNode, selfTypeDfirNode);
                DfirModelMap.AddMapping(selfTypeNode.InputTerminals.ElementAt(0), selfTypeDfirNode.InputTerminal);
                return;
            }
            if (primitiveTypeNode != null)
            {
                var primitiveTypeDfirNode = new Nodes.PrimitiveTypeNode(_currentDiagram, primitiveTypeNode.Type);
                DfirModelMap.AddMapping(primitiveTypeNode, primitiveTypeDfirNode);
                DfirModelMap.AddMapping(primitiveTypeNode.OutputTerminals.ElementAt(0), primitiveTypeDfirNode.OutputTerminal);
                return;
            }
            throw new NotSupportedException($"Unsupported node type: {node.GetType().Name}");
        }

        private void VisitWire(SMWire wire)
        {
            DfirModelMap.TranslateModelWire(wire);
        }
    }
}
