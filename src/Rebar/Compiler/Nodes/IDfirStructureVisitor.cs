using System;
using NationalInstruments.Dfir;

namespace Rebar.Compiler.Nodes
{
    internal interface IDfirStructureVisitor<T>
    {
        T VisitLoop(Loop loop, StructureTraversalPoint traversalPoint);
        T VisitFrame(Frame frame, StructureTraversalPoint traversalPoint);
        T VisitOptionPatternStructure(OptionPatternStructure optionPatternStructure, StructureTraversalPoint traversalPoint, Diagram nestedDiagram);
    }

    internal enum StructureTraversalPoint
    {
        BeforeLeftBorderNodes,
        AfterLeftBorderNodesAndBeforeDiagram,
        AfterDiagram,
        AfterAllDiagramsAndBeforeRightBorderNodes,
        AfterRightBorderNodes
    }

    internal static class DfirStructureVisitorExtensions
    {
        public static T VisitRebarStructure<T>(this IDfirStructureVisitor<T> visitor, Structure structure, StructureTraversalPoint traversalPoint, Diagram nestedDiagram)
        {
            var frame = structure as Frame;
            var loop = structure as Loop;
            var optionPatternStructure = structure as OptionPatternStructure;
            if (frame != null)
            {
                return visitor.VisitFrame(frame, traversalPoint);
            }
            else if (loop != null)
            {
                return visitor.VisitLoop(loop, traversalPoint);
            }
            else if (optionPatternStructure != null)
            {
                return visitor.VisitOptionPatternStructure(optionPatternStructure, traversalPoint, nestedDiagram);
            }
            throw new NotSupportedException();
        }
    }
}
