using NationalInstruments.Dfir;
using Rebar.Common;

namespace Rebar.Compiler
{
    /// <summary>
    /// This transform runs on semantically-valid DFIR as part of transforming it for the target compiler; it uses
    /// <see cref="AutoBorrowNodeFacade"/>s for each node to insert <see cref="ExplicitBorrowNode"/> and 
    /// <see cref="TerminateLifetimeNode"/>, so that the in the resulting DFIR each terminal gets the exact type that it expects.
    /// </summary>
    internal class AutoBorrowTransform : VisitorTransformBase
    {
        private readonly LifetimeVariableAssociation _lifetimeVariableAssociation;

        public AutoBorrowTransform(LifetimeVariableAssociation lifetimeVariableAssociation)
        {
            _lifetimeVariableAssociation = lifetimeVariableAssociation;
        }

        protected override void VisitBorderNode(BorderNode borderNode)
        {
            AutoBorrowNodeFacade nodeFacade = AutoBorrowNodeFacade.GetNodeFacade(borderNode);
            nodeFacade.CreateBorrowAndTerminateLifetimeNodes(_lifetimeVariableAssociation);
        }

        protected override void VisitNode(Node node)
        {
            AutoBorrowNodeFacade nodeFacade = AutoBorrowNodeFacade.GetNodeFacade(node);
            nodeFacade.CreateBorrowAndTerminateLifetimeNodes(_lifetimeVariableAssociation);
        }

        protected override void VisitWire(Wire wire)
        {
        }
    }
}
