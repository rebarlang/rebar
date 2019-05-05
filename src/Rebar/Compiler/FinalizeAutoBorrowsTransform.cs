using NationalInstruments.Dfir;

namespace Rebar.Compiler
{
    internal class FinalizeAutoBorrowsTransform : VisitorTransformBase
    {
        protected override void VisitBorderNode(BorderNode borderNode)
        {
            AutoBorrowNodeFacade.GetNodeFacade(borderNode).FinalizeAutoBorrows();
        }

        protected override void VisitNode(Node node)
        {
            AutoBorrowNodeFacade.GetNodeFacade(node).FinalizeAutoBorrows();
        }

        protected override void VisitWire(Wire wire)
        {
        }
    }
}
