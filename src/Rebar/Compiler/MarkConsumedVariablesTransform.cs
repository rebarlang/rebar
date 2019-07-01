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

        private void MarkFacadeVariableOfTerminalLive(Terminal terminal)
        {
            _lifetimeVariableAssociation.MarkVariableLive(terminal.GetFacadeVariable(), terminal);
        }

        private void MarkFacadeVariableOfTerminalInterrupted(Terminal terminal)
        {
            _lifetimeVariableAssociation.MarkVariableInterrupted(terminal.GetFacadeVariable());
        }

        private void MarkTrueVariableOfTerminalConsumed(Terminal terminal)
        {
            _lifetimeVariableAssociation.MarkVariableConsumed(terminal.GetTrueVariable());
        }

        bool IDfirNodeVisitor<bool>.VisitBorrowTunnel(BorrowTunnel borrowTunnel)
        {
            MarkFacadeVariableOfTerminalInterrupted(borrowTunnel.InputTerminals[0]);
            MarkFacadeVariableOfTerminalLive(borrowTunnel.OutputTerminals[0]);
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitConstant(Constant constant)
        {
            MarkFacadeVariableOfTerminalLive(constant.OutputTerminal);
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitDropNode(DropNode dropNode)
        {
            MarkTrueVariableOfTerminalConsumed(dropNode.InputTerminals[0]);
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitExplicitBorrowNode(ExplicitBorrowNode explicitBorrowNode)
        {
            foreach (Terminal inputTerminal in explicitBorrowNode.InputTerminals)
            {
                MarkFacadeVariableOfTerminalInterrupted(inputTerminal);
            }
            foreach (Terminal outputTerminal in explicitBorrowNode.OutputTerminals)
            {
                MarkFacadeVariableOfTerminalLive(outputTerminal);
            }
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitFunctionalNode(FunctionalNode functionalNode)
        {
            var signature = Signatures.GetSignatureForNIType(functionalNode.Signature);
            foreach (var inputPair in signature.Inputs.Zip(functionalNode.InputTerminals))
            {
                SignatureTerminal signatureTerminal = inputPair.Key;
                Terminal terminal = inputPair.Value;
                if (signatureTerminal.IsPassthrough)
                {
                    MarkFacadeVariableOfTerminalInterrupted(terminal);
                }
                else
                {
                    // NOTE: this is only correct if we assume that all FunctionalNodes are in normal form;
                    // that is, they don't take in bounded-lifetime variables without outputting at least 
                    // one variable in the same lifetime.
                    MarkFacadeVariableOfTerminalInterrupted(terminal);
                    MarkTrueVariableOfTerminalConsumed(terminal);
                }
            }
            foreach (var outputTerminal in functionalNode.OutputTerminals)
            {
                MarkFacadeVariableOfTerminalLive(outputTerminal);
            }

            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitIterateTunnel(IterateTunnel iterateTunnel)
        {
            MarkFacadeVariableOfTerminalInterrupted(iterateTunnel.InputTerminals[0]);
            MarkFacadeVariableOfTerminalLive(iterateTunnel.OutputTerminals[0]);
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitLockTunnel(LockTunnel lockTunnel)
        {
            MarkFacadeVariableOfTerminalInterrupted(lockTunnel.InputTerminals[0]);
            MarkFacadeVariableOfTerminalLive(lockTunnel.OutputTerminals[0]);
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitLoopConditionTunnel(LoopConditionTunnel loopConditionTunnel)
        {
            MarkFacadeVariableOfTerminalInterrupted(loopConditionTunnel.InputTerminals[0]);
            MarkFacadeVariableOfTerminalLive(loopConditionTunnel.OutputTerminals[0]);
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitTerminateLifetimeNode(TerminateLifetimeNode terminateLifetimeNode)
        {
            terminateLifetimeNode.UnificationState.FinalizeTerminateLifetimeInputs(terminateLifetimeNode, _lifetimeVariableAssociation);
            foreach (Terminal outputTerminal in terminateLifetimeNode.OutputTerminals)
            {
                MarkFacadeVariableOfTerminalLive(outputTerminal);
            }
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitTerminateLifetimeTunnel(TerminateLifetimeTunnel terminateLifetimeTunnel)
        {
            NationalInstruments.Dfir.BorderNode beginLifetimeTunnel = (NationalInstruments.Dfir.BorderNode)terminateLifetimeTunnel.BeginLifetimeTunnel;
            MarkTrueVariableOfTerminalConsumed(beginLifetimeTunnel.OutputTerminals[0]);
            MarkFacadeVariableOfTerminalLive(terminateLifetimeTunnel.OutputTerminals[0]);
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitTunnel(Tunnel tunnel)
        {
            MarkTrueVariableOfTerminalConsumed(tunnel.InputTerminals[0]);
            MarkFacadeVariableOfTerminalLive(tunnel.OutputTerminals[0]);
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitUnwrapOptionTunnel(UnwrapOptionTunnel unwrapOptionTunnel)
        {
            MarkTrueVariableOfTerminalConsumed(unwrapOptionTunnel.InputTerminals[0]);
            MarkFacadeVariableOfTerminalLive(unwrapOptionTunnel.OutputTerminals[0]);
            return true;
        }
    }
}
