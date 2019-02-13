using System.Linq;
using NationalInstruments.Compiler;
using NationalInstruments.Dfir;

namespace Rebar.Compiler
{
    internal abstract class VisitorTransformBase : IDfirTransform
    {
        public void Execute(DfirRoot dfirRoot, CompileCancellationToken cancellationToken)
        {
            VisitDiagram(dfirRoot.BlockDiagram);
            TraverseDiagram(dfirRoot.BlockDiagram);
        }

        protected virtual void VisitDiagram(Diagram diagram)
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
            Diagram diagram = structure.Diagrams.First();
            VisitDiagram(diagram);

            foreach (BorderNode inputBorderNode in structure.BorderNodes.Where(bn => bn.Direction == Direction.Input))
            {
                VisitBorderNode(inputBorderNode);
            }

            TraverseDiagram(diagram);

            foreach (BorderNode outputBorderNode in structure.BorderNodes.Where(bn => bn.Direction == Direction.Output))
            {
                VisitBorderNode(outputBorderNode);
            }
        }

        protected abstract void VisitNode(Node node);
        protected abstract void VisitWire(Wire wire);
        protected abstract void VisitBorderNode(BorderNode borderNode);
    }
}
