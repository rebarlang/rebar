using System;
using NationalInstruments.Dfir;

namespace Rebar.Compiler.TypeDiagram
{
    internal class UnifyTypesAcrossWiresTransform : VisitorTransformBase
    {
        private readonly TerminalTypeUnificationResults _typeUnificationResults = new TerminalTypeUnificationResults();

        protected override void VisitBorderNode(BorderNode borderNode)
        {
            throw new NotImplementedException();
        }

        protected override void VisitNode(Node node)
        {
            node.UnifyNodeInputTerminalTypes(_typeUnificationResults);
        }

        protected override void VisitWire(Wire wire)
        {
            wire.UnifyWireInputTerminalTypes(_typeUnificationResults);
        }
    }
}
