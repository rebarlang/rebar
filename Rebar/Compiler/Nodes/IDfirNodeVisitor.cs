using System;
using NationalInstruments.Dfir;

namespace Rebar.Compiler.Nodes
{
    internal interface IDfirNodeVisitor<T>
    {
        T VisitAssignNode(AssignNode assignNode);
        T VisitBorrowTunnel(BorrowTunnel borrowTunnel);
        T VisitConstant(Constant constant);
        T VisitCreateCellNode(CreateCellNode createCellNode);
        T VisitCreateCopyNode(CreateCopyNode createCopyNode);
        T VisitDropNode(DropNode dropNode);
        T VisitExchangeValuesNode(ExchangeValuesNode exchangeValuesNode);
        T VisitExplicitBorrowNode(ExplicitBorrowNode explicitBorrowNode);
        T VisitImmutablePassthroughNode(ImmutablePassthroughNode immutablePassthroughNode);
        T VisitIterateTunnel(IterateTunnel iterateTunnel);
        T VisitLockTunnel(LockTunnel lockTunnel);
        T VisitLoopConditionTunnel(LoopConditionTunnel loopConditionTunnel);
        T VisitMutablePassthroughNode(MutablePassthroughNode mutablePassthroughNode);
        T VisitMutatingBinaryPrimitive(MutatingBinaryPrimitive mutatingBinaryPrimitive);
        T VisitMutatingUnaryPrimitive(MutatingUnaryPrimitive mutatingUnaryPrimitive);
        T VisitOutputNode(OutputNode outputNode);
        T VisitPureBinaryPrimitive(PureBinaryPrimitive pureBinaryPrimitive);
        T VisitPureUnaryPrimitive(PureUnaryPrimitive pureUnaryPrimitive);
        T VisitRangeNode(RangeNode rangeNode);
        T VisitSomeConstructorNode(SomeConstructorNode someConstructorNode);
        T VisitSelectReferenceNode(SelectReferenceNode selectReferenceNode);
        T VisitTerminateLifetimeNode(TerminateLifetimeNode terminateLifetimeNode);
        T VisitTerminateLifetimeTunnel(TerminateLifetimeTunnel terminateLifetimeTunnel);
        T VisitTunnel(Tunnel tunnel);
        T VisitUnwrapOptionTunnel(UnwrapOptionTunnel unwrapOptionTunnel);
    }

    internal static class DfirNodeVisitorExtensions
    {
        public static T VisitRebarNode<T>(this IDfirNodeVisitor<T> visitor, Node node)
        {
            var dfirNode = node as DfirNode;
            var borderNode = node as BorderNode;
            var constant = node as Constant;
            var tunnel = node as Tunnel;
            if (dfirNode != null)
            {
                return dfirNode.AcceptVisitor(visitor);
            }
            else if (borderNode != null)
            {
                return borderNode.AcceptVisitor(visitor);
            }
            else if (constant != null)
            {
                return visitor.VisitConstant(constant);
            }
            else if (tunnel != null)
            {
                return visitor.VisitTunnel(tunnel);
            }
            throw new NotImplementedException();
        }
    }
}
