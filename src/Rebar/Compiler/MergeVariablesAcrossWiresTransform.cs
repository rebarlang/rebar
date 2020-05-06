using System.Collections.Generic;
using System.Linq;
using NationalInstruments;
using NationalInstruments.CommonModel;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler.Nodes;

namespace Rebar.Compiler
{
    internal class MergeVariablesAcrossWiresTransform : VisitorTransformBase, IDfirNodeVisitor<bool>
    {
        private readonly LifetimeVariableAssociation _lifetimeVariableAssociation;
        private readonly TerminalTypeUnificationResults _typeUnificationResults;

        public MergeVariablesAcrossWiresTransform(LifetimeVariableAssociation lifetimeVariableAssociation, TerminalTypeUnificationResults typeUnificationResults)
        {
            _lifetimeVariableAssociation = lifetimeVariableAssociation;
            _typeUnificationResults = typeUnificationResults;
        }

        protected override void VisitBorderNode(NationalInstruments.Dfir.BorderNode borderNode)
        {
            this.VisitRebarNode(borderNode);
        }

        protected override void VisitNode(Node node)
        {
            node.UnifyNodeInputTerminalTypes(_typeUnificationResults);
            this.VisitRebarNode(node);
        }

        bool IDfirNodeVisitor<bool>.VisitWire(Wire wire)
        {
            VisitWire(wire);
            return true;
        }

        protected override void VisitWire(Wire wire)
        {
            wire.UnifyNodeInputTerminalTypes(_typeUnificationResults);
        }

        bool IDfirNodeVisitor<bool>.VisitBorrowTunnel(BorrowTunnel borrowTunnel)
        {
            borrowTunnel.UnifyNodeInputTerminalTypes(_typeUnificationResults);
            Terminal inputTerminal = borrowTunnel.InputTerminals[0], outputTerminal = borrowTunnel.OutputTerminals[0];
            OutputLifetimeInterruptsInputVariable(borrowTunnel.InputTerminals[0], borrowTunnel.OutputTerminals[0]);
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitBuildTupleNode(BuildTupleNode buildTupleNode)
        {
            buildTupleNode.UnifyNodeInputTerminalTypes(_typeUnificationResults);
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitConstant(Constant constant)
        {
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitDataAccessor(DataAccessor dataAccessor)
        {
            if (dataAccessor.Terminal.Direction == Direction.Input)
            {
                dataAccessor.UnifyNodeInputTerminalTypes(_typeUnificationResults);
            }
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitDecomposeTupleNode(DecomposeTupleNode decomposeTupleNode)
        {
            decomposeTupleNode.UnifyNodeInputTerminalTypes(_typeUnificationResults);
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitDropNode(DropNode dropNode)
        {
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitExplicitBorrowNode(ExplicitBorrowNode explicitBorrowNode)
        {
            Lifetime outputLifetime = explicitBorrowNode.OutputTerminals[0].GetTrueVariable().Lifetime;
            IEnumerable<VariableReference> inputVariables = explicitBorrowNode.InputTerminals.Select(VariableExtensions.GetTrueVariable);
            inputVariables.ForEach(v => _lifetimeVariableAssociation.AddVariableInterruptedByLifetime(v, outputLifetime));
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitFunctionalNode(FunctionalNode functionalNode)
        {
            AutoBorrowNodeFacade.GetNodeFacade(functionalNode).SetLifetimeInterruptedVariables(_lifetimeVariableAssociation);
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitIterateTunnel(IterateTunnel iterateTunnel)
        {
            iterateTunnel.UnifyNodeInputTerminalTypes(_typeUnificationResults);
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitLockTunnel(LockTunnel lockTunnel)
        {
            lockTunnel.UnifyNodeInputTerminalTypes(_typeUnificationResults);
            OutputLifetimeInterruptsInputVariable(lockTunnel.InputTerminals[0], lockTunnel.OutputTerminals[0]);
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitLoopConditionTunnel(LoopConditionTunnel loopConditionTunnel)
        {
            Terminal nodeTerminal = loopConditionTunnel.InputTerminals[0];
            var connectedWireTerminal = nodeTerminal.ConnectedTerminal;
            if (connectedWireTerminal != null)
            {
                // Unify node input terminal with its connected source
                AutoBorrowNodeFacade nodeFacade = AutoBorrowNodeFacade.GetNodeFacade(loopConditionTunnel);
                nodeFacade[nodeTerminal].UnifyWithConnectedWireTypeAsNodeInput(connectedWireTerminal.GetFacadeVariable(), _typeUnificationResults);
            }
            OutputLifetimeInterruptsInputVariable(nodeTerminal, loopConditionTunnel.OutputTerminals[0]);
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitMethodCallNode(MethodCallNode methodCallNode)
        {
            AutoBorrowNodeFacade.GetNodeFacade(methodCallNode).SetLifetimeInterruptedVariables(_lifetimeVariableAssociation);
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitOptionPatternStructureSelector(OptionPatternStructureSelector optionPatternStructureSelector)
        {
            optionPatternStructureSelector.UnifyNodeInputTerminalTypes(_typeUnificationResults);
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitStructConstructorNode(StructConstructorNode structConstructorNode)
        {
            structConstructorNode.UnifyNodeInputTerminalTypes(_typeUnificationResults);
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitStructFieldAccessorNode(StructFieldAccessorNode structFieldAccessorNode)
        {
            structFieldAccessorNode.UnifyNodeInputTerminalTypes(_typeUnificationResults);
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitTerminateLifetimeNode(TerminateLifetimeNode terminateLifetimeNode)
        {
            terminateLifetimeNode.UnificationState.UpdateTerminateLifetimeOutputs(terminateLifetimeNode, _lifetimeVariableAssociation);
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitTerminateLifetimeTunnel(TerminateLifetimeTunnel terminateLifetimeTunnel)
        {
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitTunnel(Tunnel tunnel)
        {
            tunnel.UnifyNodeInputTerminalTypes(_typeUnificationResults);
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitUnwrapOptionTunnel(UnwrapOptionTunnel unwrapOptionTunnel)
        {
            unwrapOptionTunnel.UnifyNodeInputTerminalTypes(_typeUnificationResults);
            return true;
        }

        private void OutputLifetimeInterruptsInputVariable(Terminal inputTerminal, Terminal outputTerminal)
        {
            _lifetimeVariableAssociation.AddVariableInterruptedByLifetime(inputTerminal.GetTrueVariable(), outputTerminal.GetTrueVariable().Lifetime);
        }
    }
}
