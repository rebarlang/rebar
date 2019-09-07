using System.Linq;
using NationalInstruments.Compiler;
using NationalInstruments.Dfir;

namespace Rebar.Compiler
{
    internal abstract class VisitorTransformBase : IDfirTransform
    {
        public void Execute(DfirRoot dfirRoot, CompileCancellationToken cancellationToken)
        {
            VisitDfirRoot(dfirRoot);
            VisitDiagram(dfirRoot.BlockDiagram);
            TraverseDiagram(dfirRoot.BlockDiagram);
            PostVisitDiagram(dfirRoot.BlockDiagram);
        }

        protected virtual void VisitDfirRoot(DfirRoot dfirRoot)
        {
        }

        protected virtual void VisitDiagram(Diagram diagram)
        {
        }

        protected virtual void PostVisitDiagram(Diagram diagram)
        {
        }

        private void TraverseDiagram(Diagram diagram)
        {
            foreach (var node in diagram.Nodes.ToList())
            {
                var wire = node as Wire;
                var structure = node as Structure;
                if (structure != null)
                {
                    TraverseStructure(structure);
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

        private void TraverseStructure(Structure structure)
        {
            VisitStructure(structure, Nodes.StructureTraversalPoint.BeforeLeftBorderNodes);
            Diagram diagram = structure.Diagrams.First();
            VisitDiagram(diagram);

            foreach (BorderNode inputBorderNode in structure.BorderNodes.Where(bn => bn.Direction == Direction.Input))
            {
                VisitBorderNode(inputBorderNode);
            }
            VisitStructure(structure, Nodes.StructureTraversalPoint.AfterLeftBorderNodesAndBeforeDiagram);
            TraverseDiagram(diagram);
            VisitStructure(structure, Nodes.StructureTraversalPoint.AfterDiagramAndBeforeRightBorderNodes);
            foreach (BorderNode outputBorderNode in structure.BorderNodes.Where(bn => bn.Direction == Direction.Output))
            {
                VisitBorderNode(outputBorderNode);
            }
            VisitStructure(structure, Nodes.StructureTraversalPoint.AfterRightBorderNodes);
        }

        protected abstract void VisitNode(Node node);
        protected abstract void VisitWire(Wire wire);
        protected abstract void VisitBorderNode(BorderNode borderNode);

        protected virtual void VisitStructure(Structure structure, Nodes.StructureTraversalPoint traversalPoint)
        {
        }
    }
}
