using System;
using NationalInstruments.Dfir;

namespace Rebar.Compiler.Nodes
{
    internal interface IDfirStructureVisitor<T>
    {
        T VisitLoop(Loop loop, StructureTraversalPoint traversalPoint);
        T VisitFrame(Frame frame, StructureTraversalPoint traversalPoint);
    }

    internal enum StructureTraversalPoint
    {
        BeforeLeftBorderNodes,
        AfterLeftBorderNodesAndBeforeDiagram,
        AfterDiagramAndBeforeRightBorderNodes,
        AfterRightBorderNodes
    }

    internal static class DfirStructureVisitorExtensions
    {
        public static T VisitRebarStructure<T>(this IDfirStructureVisitor<T> visitor, Structure structure, StructureTraversalPoint traversalPoint)
        {
            var frame = structure as Frame;
            var loop = structure as Loop;
            if (frame != null)
            {
                return visitor.VisitFrame(frame, traversalPoint);
            }
            else if (loop != null)
            {
                return visitor.VisitLoop(loop, traversalPoint);
            }
            throw new NotSupportedException();
        }
    }
}
