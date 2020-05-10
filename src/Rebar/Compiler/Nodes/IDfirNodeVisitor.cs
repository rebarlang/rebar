using System;
using NationalInstruments.Dfir;

namespace Rebar.Compiler.Nodes
{
    internal interface IDfirNodeVisitor<T>
    {
        T VisitBorrowTunnel(BorrowTunnel borrowTunnel);
        T VisitBuildTupleNode(BuildTupleNode buildTupleNode);
        T VisitConstant(Constant constant);
        T VisitDataAccessor(DataAccessor dataAccessor);
        T VisitDecomposeTupleNode(DecomposeTupleNode decomposeTupleNode);
        T VisitDropNode(DropNode dropNode);
        T VisitExplicitBorrowNode(ExplicitBorrowNode explicitBorrowNode);
        T VisitFunctionalNode(FunctionalNode functionalNode);
        T VisitIterateTunnel(IterateTunnel iterateTunnel);
        T VisitLockTunnel(LockTunnel lockTunnel);
        T VisitLoopConditionTunnel(LoopConditionTunnel loopConditionTunnel);
        T VisitMethodCallNode(MethodCallNode methodCallNode);
        T VisitOptionPatternStructureSelector(OptionPatternStructureSelector optionPatternStructureSelector);
        T VisitStructConstructorNode(StructConstructorNode structConstructorNode);
        T VisitStructFieldAccessorNode(StructFieldAccessorNode structFieldAccessorNode);
        T VisitTerminateLifetimeNode(TerminateLifetimeNode terminateLifetimeNode);
        T VisitTerminateLifetimeTunnel(TerminateLifetimeTunnel terminateLifetimeTunnel);
        T VisitTunnel(Tunnel tunnel);
        T VisitUnwrapOptionTunnel(UnwrapOptionTunnel unwrapOptionTunnel);
        T VisitVariantConstructorNode(VariantConstructorNode variantConstructorNode);
        T VisitWire(Wire wire);
    }

    internal static class DfirNodeVisitorExtensions
    {
        public static T VisitRebarNode<T>(this IDfirNodeVisitor<T> visitor, Node node)
        {
            var dfirNode = node as DfirNode;
            var borderNode = node as BorderNode;
            var constant = node as Constant;
            var dataAccessor = node as DataAccessor;
            var tunnel = node as Tunnel;
            var wire = node as Wire;
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
            else if (dataAccessor != null)
            {
                return visitor.VisitDataAccessor(dataAccessor);
            }
            else if (tunnel != null)
            {
                return visitor.VisitTunnel(tunnel);
            }
            else if (wire != null)
            {
                return visitor.VisitWire(wire);
            }
            throw new NotImplementedException();
        }
    }
}
