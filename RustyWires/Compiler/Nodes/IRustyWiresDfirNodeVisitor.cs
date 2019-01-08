using System;
using NationalInstruments.Dfir;

namespace RustyWires.Compiler.Nodes
{
    internal interface IRustyWiresDfirNodeVisitor<T>
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

    internal static class RustyWiresDfirNodeVisitorExtensions
    {
        public static T VisitRustyWiresNode<T>(this IRustyWiresDfirNodeVisitor<T> visitor, Node node)
        {
            var rustyWiresDfirNode = node as RustyWiresDfirNode;
            var rustyWiresBorderNode = node as RustyWiresBorderNode;
            var constant = node as Constant;
            var tunnel = node as Tunnel;
            if (rustyWiresDfirNode != null)
            {
                return rustyWiresDfirNode.AcceptVisitor(visitor);
            }
            else if (rustyWiresBorderNode != null)
            {
                return rustyWiresBorderNode.AcceptVisitor(visitor);
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
