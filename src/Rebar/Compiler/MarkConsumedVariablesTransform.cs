using System.Linq;
using NationalInstruments;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler.Nodes;

namespace Rebar.Compiler
{
    /// <summary>
    /// Transform that marks as consumed in a <see cref="LifetimeVariableAssociation"/> all <see cref="VariableReference"/> that are consumed as an input to some node.
    /// </summary>
    /// <remarks>
    /// To be considered consumed, a variable should be an input to a node
    /// * that does not pass it through
    /// * that does not borrow it
    /// * that outputs at least one other variable in the same lifetime, for bounded-lifetime variables
    /// </remarks>
    internal class MarkConsumedVariablesTransform : VisitorTransformBase, IDfirNodeVisitor<bool>
    {
        private readonly LifetimeVariableAssociation _lifetimeVariableAssociation;

        public MarkConsumedVariablesTransform(LifetimeVariableAssociation lifetimeVariableAssociation)
        {
            _lifetimeVariableAssociation = lifetimeVariableAssociation;
        }

        protected override void VisitBorderNode(NationalInstruments.Dfir.BorderNode borderNode)
        {
            this.VisitRebarNode(borderNode);
        }

        protected override void VisitNode(Node node)
        {
            this.VisitRebarNode(node);
        }

        protected override void VisitWire(Wire wire)
        {
        }

        bool IDfirNodeVisitor<bool>.VisitBorrowTunnel(BorrowTunnel borrowTunnel)
        {
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitConstant(Constant constant)
        {
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitDropNode(DropNode dropNode)
        {
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitExplicitBorrowNode(ExplicitBorrowNode explicitBorrowNode)
        {
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitFunctionalNode(FunctionalNode functionalNode)
        {
            var signature = Signatures.GetSignatureForNIType(functionalNode.Signature);
            foreach (var inputPair in signature.Inputs.Zip(functionalNode.InputTerminals))
            {
                SignatureTerminal signatureTerminal = inputPair.Key;
                if (signatureTerminal.IsPassthrough)
                {
                    continue;
                }
                Terminal terminal = inputPair.Value;
                // NOTE: this is only correct if we assume that all FunctionalNods are in normal form;
                // that is, they don't take in bounded-lifetime variables without outputting at least 
                // one variable in the same lifetime.
                _lifetimeVariableAssociation.MarkVariableConsumed(terminal.GetTrueVariable());
            }
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitIterateTunnel(IterateTunnel iterateTunnel)
        {
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitLockTunnel(LockTunnel lockTunnel)
        {
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitLoopConditionTunnel(LoopConditionTunnel loopConditionTunnel)
        {
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitTerminateLifetimeNode(TerminateLifetimeNode terminateLifetimeNode)
        {
            terminateLifetimeNode.UnificationState.FinalizeTerminateLifetimeInputs(terminateLifetimeNode, _lifetimeVariableAssociation);
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitTerminateLifetimeTunnel(TerminateLifetimeTunnel terminateLifetimeTunnel)
        {
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitTunnel(Tunnel tunnel)
        {
            _lifetimeVariableAssociation.MarkVariableConsumed(tunnel.InputTerminals[0].GetTrueVariable());
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitUnwrapOptionTunnel(UnwrapOptionTunnel unwrapOptionTunnel)
        {
            _lifetimeVariableAssociation.MarkVariableConsumed(unwrapOptionTunnel.InputTerminals[0].GetTrueVariable());
            return true;
        }
    }
}
