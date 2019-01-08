using System.Collections.Generic;
using System.Linq;
using NationalInstruments.Dfir;
using RustyWires.Compiler.Nodes;

namespace RustyWires.Compiler
{
    internal class DetermineVariablesTransform : VisitorTransformBase, IRustyWiresDfirNodeVisitor<IEnumerable<PassthroughTerminalPair>>
    {
        private VariableSet CurrentDiagramVariableSet { get; set; }

        protected override void VisitDiagram(Diagram diagram)
        {
            diagram.SetVariableSet(new VariableSet());
        }

        protected override void VisitWire(Wire wire)
        {
            Terminal connectedTerminal = wire.SourceTerminal.ConnectedTerminal;
            if (connectedTerminal != null)
            {
                Variable sourceVariable = connectedTerminal.GetVariable();
                wire.SourceTerminal.AddTerminalToVariable(sourceVariable);
                bool reuseSource = true;
                foreach (Terminal sinkTerminal in wire.SinkTerminals)
                {
                    if (reuseSource)
                    {
                        sinkTerminal.AddTerminalToVariable(sourceVariable);
                        reuseSource = false;
                    }
                    else
                    {
                        sinkTerminal.AddTerminalToNewVariable(sourceVariable.Mutable);
                    }
                }
            }
            else
            {
                // source is not connected somehow; not sure what to do here
            }
        }

        protected override void VisitNode(Node node)
        {
            List<Terminal> nonPassthroughInputs = new List<Terminal>(node.InputTerminals), nonPassthroughOutputs = new List<Terminal>(node.OutputTerminals);
            var rustyWiresDfirNode = node as RustyWiresDfirNode;
            var passthroughTerminalPairs = this.VisitRustyWiresNode(node);
            foreach (var pair in passthroughTerminalPairs)
            {
                nonPassthroughInputs.Remove(pair.InputTerminal);
                nonPassthroughOutputs.Remove(pair.OutputTerminal);
            }

            // TODO: if there are any non-passthrough inputs with reference types that are connected to wires with owned values, create a frame
            // with borrow tunnels, then update passthroughTerminalPairs
            foreach (var pair in passthroughTerminalPairs)
            {
                LinkPassthroughTerminalPair(pair.InputTerminal, pair.OutputTerminal);
            }

            foreach (var nonPassthroughInput in nonPassthroughInputs)
            {
                PullInputTerminalVariable(nonPassthroughInput);
            }

            foreach (var nonPassthroughOutput in nonPassthroughOutputs)
            {
                AddTerminalToNewVariable(nonPassthroughOutput, !(node is TerminateLifetimeNode));
            }
        }

        private void AddTerminalToNewVariable(Terminal outputTerminal, bool markWireAsVariableFirst = true)
        {
            bool mutableVariable = false;
            if (outputTerminal.IsConnected && markWireAsVariableFirst)
            {
                Wire connectedWire = (Wire)outputTerminal.ConnectedTerminal.ParentNode;
                connectedWire.SetIsFirstVariableWire(true);
                mutableVariable = connectedWire.GetWireBeginsMutableVariable();
            }
            outputTerminal.AddTerminalToNewVariable(mutableVariable);
        }

        private Variable PullInputTerminalVariable(Terminal inputTerminal)
        {
            var connectedTerminal = inputTerminal.ConnectedTerminal;
            if (connectedTerminal != null)
            {
                Variable variable = connectedTerminal.GetVariable();
                inputTerminal.AddTerminalToVariable(variable);
                return variable;
            }
            return null;
        }

        private void LinkPassthroughTerminalPair(Terminal inputTerminal, Terminal outputTerminal)
        {
            PullInputTerminalVariable(inputTerminal);
            // The input terminal may have a variable whether or not it is connected.
            Variable inputVariable = inputTerminal.GetVariable();
            if (inputVariable != null)
            {
                outputTerminal.AddTerminalToVariable(inputVariable);
            }
            else
            {
                // the input on a passthrough is not wired, but the output is
                // for now, create a new variable for the output
                // Since the wire will be broken, do not allow it to be set as mutable
                AddTerminalToNewVariable(outputTerminal, false);
            }
        }

        protected override void VisitBorderNode(BorderNode borderNode)
        {
            this.VisitRustyWiresNode(borderNode);
        }

        IEnumerable<PassthroughTerminalPair> IRustyWiresDfirNodeVisitor<IEnumerable<PassthroughTerminalPair>>.VisitAssignNode(AssignNode assignNode)
        {
            yield return new PassthroughTerminalPair(assignNode.InputTerminals.ElementAt(0), assignNode.OutputTerminals.ElementAt(0));
        }

        IEnumerable<PassthroughTerminalPair> IRustyWiresDfirNodeVisitor<IEnumerable<PassthroughTerminalPair>>.VisitConstant(Constant constant)
        {
            return Enumerable.Empty<PassthroughTerminalPair>();
        }

        IEnumerable<PassthroughTerminalPair> IRustyWiresDfirNodeVisitor<IEnumerable<PassthroughTerminalPair>>.VisitCreateCellNode(CreateCellNode createCellNode)
        {
            return Enumerable.Empty<PassthroughTerminalPair>();
        }

        IEnumerable<PassthroughTerminalPair> IRustyWiresDfirNodeVisitor<IEnumerable<PassthroughTerminalPair>>.VisitCreateCopyNode(CreateCopyNode createCopyNode)
        {
            yield return new PassthroughTerminalPair(createCopyNode.InputTerminals.ElementAt(0), createCopyNode.OutputTerminals.ElementAt(0));
        }

        IEnumerable<PassthroughTerminalPair> IRustyWiresDfirNodeVisitor<IEnumerable<PassthroughTerminalPair>>.VisitDropNode(DropNode dropNode)
        {
            return Enumerable.Empty<PassthroughTerminalPair>();
        }

        IEnumerable<PassthroughTerminalPair> IRustyWiresDfirNodeVisitor<IEnumerable<PassthroughTerminalPair>>.VisitExchangeValuesNode(ExchangeValuesNode exchangeValuesNode)
        {
            yield return new PassthroughTerminalPair(exchangeValuesNode.Terminals[0], exchangeValuesNode.Terminals[2]);
            yield return new PassthroughTerminalPair(exchangeValuesNode.Terminals[1], exchangeValuesNode.Terminals[3]);
        }

        IEnumerable<PassthroughTerminalPair> IRustyWiresDfirNodeVisitor<IEnumerable<PassthroughTerminalPair>>.VisitExplicitBorrowNode(ExplicitBorrowNode explicitBorrowNode)
        {
            return Enumerable.Empty<PassthroughTerminalPair>();
        }

        IEnumerable<PassthroughTerminalPair> IRustyWiresDfirNodeVisitor<IEnumerable<PassthroughTerminalPair>>.VisitImmutablePassthroughNode(ImmutablePassthroughNode immutablePassthroughNode)
        {
            yield return new PassthroughTerminalPair(immutablePassthroughNode.InputTerminals.ElementAt(0), immutablePassthroughNode.OutputTerminals.ElementAt(0));
        }

        IEnumerable<PassthroughTerminalPair> IRustyWiresDfirNodeVisitor<IEnumerable<PassthroughTerminalPair>>.VisitMutablePassthroughNode(MutablePassthroughNode mutablePassthroughNode)
        {
            yield return new PassthroughTerminalPair(mutablePassthroughNode.InputTerminals.ElementAt(0), mutablePassthroughNode.OutputTerminals.ElementAt(0));
        }

        IEnumerable<PassthroughTerminalPair> IRustyWiresDfirNodeVisitor<IEnumerable<PassthroughTerminalPair>>.VisitMutatingBinaryPrimitive(MutatingBinaryPrimitive mutatingBinaryPrimitive)
        {
            yield return new PassthroughTerminalPair(mutatingBinaryPrimitive.Terminals[0], mutatingBinaryPrimitive.Terminals[2]);
            yield return new PassthroughTerminalPair(mutatingBinaryPrimitive.Terminals[1], mutatingBinaryPrimitive.Terminals[3]);
        }

        IEnumerable<PassthroughTerminalPair> IRustyWiresDfirNodeVisitor<IEnumerable<PassthroughTerminalPair>>.VisitMutatingUnaryPrimitive(MutatingUnaryPrimitive mutatingUnaryPrimitive)
        {
            yield return new PassthroughTerminalPair(mutatingUnaryPrimitive.Terminals[0], mutatingUnaryPrimitive.Terminals[1]);
        }

        IEnumerable<PassthroughTerminalPair> IRustyWiresDfirNodeVisitor<IEnumerable<PassthroughTerminalPair>>.VisitPureBinaryPrimitive(PureBinaryPrimitive pureBinaryPrimitive)
        {
            yield return new PassthroughTerminalPair(pureBinaryPrimitive.Terminals[0], pureBinaryPrimitive.Terminals[2]);
            yield return new PassthroughTerminalPair(pureBinaryPrimitive.Terminals[1], pureBinaryPrimitive.Terminals[3]);
        }

        IEnumerable<PassthroughTerminalPair> IRustyWiresDfirNodeVisitor<IEnumerable<PassthroughTerminalPair>>.VisitPureUnaryPrimitive(PureUnaryPrimitive pureUnaryPrimitive)
        {
            yield return new PassthroughTerminalPair(pureUnaryPrimitive.Terminals[0], pureUnaryPrimitive.Terminals[1]);
        }

        IEnumerable<PassthroughTerminalPair> IRustyWiresDfirNodeVisitor<IEnumerable<PassthroughTerminalPair>>.VisitRangeNode(RangeNode rangeNode)
        {
            return Enumerable.Empty<PassthroughTerminalPair>();
        }

        IEnumerable<PassthroughTerminalPair> IRustyWiresDfirNodeVisitor<IEnumerable<PassthroughTerminalPair>>.VisitSelectReferenceNode(SelectReferenceNode selectReferenceNode)
        {
            yield return new PassthroughTerminalPair(selectReferenceNode.Terminals[0], selectReferenceNode.Terminals[3]);
            yield return new PassthroughTerminalPair(selectReferenceNode.Terminals[1], selectReferenceNode.Terminals[4]);
            yield return new PassthroughTerminalPair(selectReferenceNode.Terminals[2], selectReferenceNode.Terminals[5]);
        }

        IEnumerable<PassthroughTerminalPair> IRustyWiresDfirNodeVisitor<IEnumerable<PassthroughTerminalPair>>.VisitSomeConstructorNode(SomeConstructorNode someConstructorNode)
        {
            return Enumerable.Empty<PassthroughTerminalPair>();
        }

        IEnumerable<PassthroughTerminalPair> IRustyWiresDfirNodeVisitor<IEnumerable<PassthroughTerminalPair>>.VisitTerminateLifetimeNode(TerminateLifetimeNode terminateLifetimeNode)
        {
            return Enumerable.Empty<PassthroughTerminalPair>();
        }

        IEnumerable<PassthroughTerminalPair> IRustyWiresDfirNodeVisitor<IEnumerable<PassthroughTerminalPair>>.VisitBorrowTunnel(BorrowTunnel borrowTunnel)
        {
            PullInputTerminalVariable(borrowTunnel.GetOuterTerminal(0));
            AddTerminalToNewVariable(borrowTunnel.GetInnerTerminal(0, 0));
            return Enumerable.Empty<PassthroughTerminalPair>();
        }

        IEnumerable<PassthroughTerminalPair> IRustyWiresDfirNodeVisitor<IEnumerable<PassthroughTerminalPair>>.VisitIterateTunnel(IterateTunnel iterateTunnel)
        {
            PullInputTerminalVariable(iterateTunnel.GetOuterTerminal(0));
            AddTerminalToNewVariable(iterateTunnel.GetInnerTerminal(0, 0));
            return Enumerable.Empty<PassthroughTerminalPair>();
        }

        IEnumerable<PassthroughTerminalPair> IRustyWiresDfirNodeVisitor<IEnumerable<PassthroughTerminalPair>>.VisitLockTunnel(LockTunnel lockTunnel)
        {
            PullInputTerminalVariable(lockTunnel.GetOuterTerminal(0));
            AddTerminalToNewVariable(lockTunnel.GetInnerTerminal(0, 0));
            return Enumerable.Empty<PassthroughTerminalPair>();
        }

        IEnumerable<PassthroughTerminalPair> IRustyWiresDfirNodeVisitor<IEnumerable<PassthroughTerminalPair>>.VisitLoopConditionTunnel(LoopConditionTunnel loopConditionTunnel)
        {
            Terminal outerTerminal = loopConditionTunnel.GetOuterTerminal(0);
            if (PullInputTerminalVariable(outerTerminal) == null)
            {
                // TODO: how to determine the mutability of the outer loop condition variable?
                AddTerminalToNewVariable(outerTerminal, false);
            }
            // Disallow marking the loop condition reference variable as mutable.
            AddTerminalToNewVariable(loopConditionTunnel.GetInnerTerminal(0, 0), false);
            return Enumerable.Empty<PassthroughTerminalPair>();
        }

        IEnumerable<PassthroughTerminalPair> IRustyWiresDfirNodeVisitor<IEnumerable<PassthroughTerminalPair>>.VisitTunnel(Tunnel tunnel)
        {
            Terminal inputTerminal = tunnel.Direction == Direction.Input ? tunnel.GetOuterTerminal() : tunnel.GetInnerTerminal();
            Terminal outputTerminal = tunnel.Direction == Direction.Input ? tunnel.GetInnerTerminal() : tunnel.GetOuterTerminal();
            PullInputTerminalVariable(inputTerminal);
            AddTerminalToNewVariable(outputTerminal);
            return Enumerable.Empty<PassthroughTerminalPair>();
        }

        IEnumerable<PassthroughTerminalPair> IRustyWiresDfirNodeVisitor<IEnumerable<PassthroughTerminalPair>>.VisitTerminateLifetimeTunnel(TerminateLifetimeTunnel unborrowTunnel)
        {
            LinkPassthroughTerminalPair(((RustyWiresBorderNode)unborrowTunnel.BeginLifetimeTunnel).GetOuterTerminal(0), unborrowTunnel.GetOuterTerminal(0));
            return Enumerable.Empty<PassthroughTerminalPair>();
        }

        IEnumerable<PassthroughTerminalPair> IRustyWiresDfirNodeVisitor<IEnumerable<PassthroughTerminalPair>>.VisitUnwrapOptionTunnel(UnwrapOptionTunnel unwrapOptionTunnel)
        {
            Terminal inputTerminal = unwrapOptionTunnel.Direction == Direction.Input ? unwrapOptionTunnel.GetOuterTerminal(0) : unwrapOptionTunnel.GetInnerTerminal(0, 0);
            Terminal outputTerminal = unwrapOptionTunnel.Direction == Direction.Input ? unwrapOptionTunnel.GetInnerTerminal(0, 0) : unwrapOptionTunnel.GetOuterTerminal(0);
            PullInputTerminalVariable(inputTerminal);
            AddTerminalToNewVariable(outputTerminal);
            return Enumerable.Empty<PassthroughTerminalPair>();
        }
    }
}
