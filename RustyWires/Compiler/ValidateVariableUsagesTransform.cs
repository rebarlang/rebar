using NationalInstruments.Dfir;
using RustyWires.Compiler.Nodes;

namespace RustyWires.Compiler
{
    internal class ValidateVariableUsagesTransform : VisitorTransformBase
    {
        protected override void VisitNode(Node node)
        {
            var rustyWiresDfirNode = node as RustyWiresDfirNode;
            rustyWiresDfirNode?.CheckVariableUsages();
        }

        protected override void VisitWire(Wire wire)
        {
        }

        protected override void VisitBorderNode(BorderNode borderNode)
        {
            var rustyWiresBorderNode = borderNode as RustyWiresBorderNode;
            rustyWiresBorderNode?.CheckVariableUsages();
        }
    }
}
