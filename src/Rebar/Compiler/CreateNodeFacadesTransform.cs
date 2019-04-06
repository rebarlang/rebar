using System.Linq;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler.Nodes;

namespace Rebar.Compiler
{
    internal class CreateNodeFacadesTransform : VisitorTransformBase, IDfirNodeVisitor<bool>
    {
        private AutoBorrowNodeFacade _nodeFacade;

        protected override void VisitDiagram(Diagram diagram)
        {
            diagram.SetVariableSet(new VariableSet());
        }

        protected override void VisitWire(Wire wire)
        {
            AutoBorrowNodeFacade wireFacade = AutoBorrowNodeFacade.GetNodeFacade(wire);
            foreach (var terminal in wire.Terminals)
            {
                wireFacade[terminal] = new SimpleTerminalFacade(terminal);
            }

            Terminal firstSinkWireTerminal = wire.SinkTerminals.FirstOrDefault(),
                sourceWireTerminal = null;
            if (wire.TryGetSourceTerminal(out sourceWireTerminal) && firstSinkWireTerminal != null)
            {
                firstSinkWireTerminal.GetFacadeVariable().MergeInto(sourceWireTerminal.GetFacadeVariable());
            }
        }

        protected override void VisitNode(Node node)
        {
            _nodeFacade = AutoBorrowNodeFacade.GetNodeFacade(node);
            this.VisitRebarNode(node);
            _nodeFacade = null;
        }

        protected override void VisitBorderNode(NationalInstruments.Dfir.BorderNode borderNode)
        {
            _nodeFacade = AutoBorrowNodeFacade.GetNodeFacade(borderNode);
            this.VisitRebarNode(borderNode);
            _nodeFacade = null;
        }

        bool IDfirNodeVisitor<bool>.VisitAssignNode(AssignNode assignNode)
        {
            Terminal assigneeInput = assignNode.InputTerminals.ElementAt(0),
                newValueInput = assignNode.InputTerminals.ElementAt(1),
                assigneeOutput = assignNode.OutputTerminals.ElementAt(0);
            _nodeFacade.CreateInputLifetimeGroup(InputReferenceMutability.RequireMutable).AddTerminalFacade(assigneeInput, assigneeOutput);
            _nodeFacade[newValueInput] = new SimpleTerminalFacade(newValueInput);
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitConstant(Constant constant)
        {
            Terminal valueOutput = constant.OutputTerminals.ElementAt(0);
            _nodeFacade[valueOutput] = new SimpleTerminalFacade(valueOutput);
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitCreateCellNode(CreateCellNode createCellNode)
        {
            Terminal valueInput = createCellNode.InputTerminals.ElementAt(0),
                cellOutput = createCellNode.OutputTerminals.ElementAt(0);
            _nodeFacade[valueInput] = new SimpleTerminalFacade(valueInput);
            _nodeFacade[cellOutput] = new SimpleTerminalFacade(cellOutput);
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitDropNode(DropNode dropNode)
        {
            Terminal valueInput = dropNode.InputTerminals.ElementAt(0);
            _nodeFacade[valueInput] = new SimpleTerminalFacade(valueInput);
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitExchangeValuesNode(ExchangeValuesNode exchangeValuesNode)
        {
            Terminal input1Terminal = exchangeValuesNode.InputTerminals.ElementAt(0),
                input2Terminal = exchangeValuesNode.InputTerminals.ElementAt(1),
                output1Terminal = exchangeValuesNode.OutputTerminals.ElementAt(0),
                output2Terminal = exchangeValuesNode.OutputTerminals.ElementAt(1);
            ReferenceInputTerminalLifetimeGroup lifetimeGroup = _nodeFacade.CreateInputLifetimeGroup(InputReferenceMutability.RequireMutable);
            lifetimeGroup.AddTerminalFacade(input1Terminal, output1Terminal);
            lifetimeGroup.AddTerminalFacade(input2Terminal, output2Terminal);
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitExplicitBorrowNode(ExplicitBorrowNode explicitBorrowNode)
        {
            foreach (var terminal in explicitBorrowNode.Terminals)
            {
                _nodeFacade[terminal] = new SimpleTerminalFacade(terminal);
            }
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitFunctionalNode(FunctionalNode functionalNode)
        {
            int inputIndex = 0, outputIndex = 0;
            foreach (NIType parameter in functionalNode.Signature.GetParameters())
            {
                NIType parameterDataType = parameter.GetDataType();
                bool isInput = parameter.GetInputParameterPassingRule() != NIParameterPassingRule.NotAllowed,
                    isOutput = parameter.GetOutputParameterPassingRule() != NIParameterPassingRule.NotAllowed;
                Terminal inputTerminal = null, outputTerminal = null;
                if (isInput)
                {
                    inputTerminal = functionalNode.InputTerminals[inputIndex];
                    ++inputIndex;
                }
                if (isOutput)
                {
                    outputTerminal = functionalNode.OutputTerminals[outputIndex];
                    ++outputIndex;
                }
                if (isInput && isOutput)
                {
                    if (parameterDataType.IsImmutableReferenceType())
                    {
                        // TODO: sharing lifetime groups
                        _nodeFacade.CreateInputLifetimeGroup(InputReferenceMutability.AllowImmutable)
                            .AddTerminalFacade(inputTerminal, outputTerminal);
                    }
                    else if (parameterDataType.IsMutableReferenceType())
                    {
                        // TODO: sharing lifetime groups
                        _nodeFacade.CreateInputLifetimeGroup(InputReferenceMutability.RequireMutable)
                            .AddTerminalFacade(inputTerminal, outputTerminal);
                    }
                    else
                    {
                        throw new System.NotSupportedException("Inout parameters must be reference types.");
                    }
                }
                else if (isOutput)
                {
                    _nodeFacade[outputTerminal] = new SimpleTerminalFacade(outputTerminal);
                }
                else if (isInput)
                {
                    _nodeFacade[inputTerminal] = new SimpleTerminalFacade(inputTerminal);
                }
                else
                {
                    throw new System.NotSupportedException("Parameter is neither input nor output");
                }
            }
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitSelectReferenceNode(SelectReferenceNode selectReferenceNode)
        {
            Terminal selectorInput = selectReferenceNode.InputTerminals.ElementAt(0),
                trueInput = selectReferenceNode.InputTerminals.ElementAt(1),
                falseInput = selectReferenceNode.InputTerminals.ElementAt(2),
                selectorOutput = selectReferenceNode.OutputTerminals.ElementAt(0),
                resultOutput = selectReferenceNode.OutputTerminals.ElementAt(1);
            _nodeFacade.CreateInputLifetimeGroup(InputReferenceMutability.AllowImmutable).AddTerminalFacade(selectorInput, selectorOutput);
            ReferenceInputTerminalLifetimeGroup lifetimeGroup = _nodeFacade.CreateInputLifetimeGroup(InputReferenceMutability.Polymorphic);
            lifetimeGroup.AddTerminalFacade(trueInput);
            lifetimeGroup.AddTerminalFacade(falseInput);
            _nodeFacade[resultOutput] = new SimpleTerminalFacade(resultOutput);
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitSomeConstructorNode(SomeConstructorNode someConstructorNode)
        {
            Terminal valueInput = someConstructorNode.InputTerminals.ElementAt(0),
                optionOutput = someConstructorNode.OutputTerminals.ElementAt(0);
            _nodeFacade[valueInput] = new SimpleTerminalFacade(valueInput);
            _nodeFacade[optionOutput] = new SimpleTerminalFacade(optionOutput);
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitTerminateLifetimeNode(TerminateLifetimeNode terminateLifetimeNode)
        {
            foreach (var terminal in terminateLifetimeNode.Terminals)
            {
                // TODO: when updating terminals during SA, also update the TerminalFacades
                _nodeFacade[terminal] = new SimpleTerminalFacade(terminal);
            }
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitBorrowTunnel(BorrowTunnel borrowTunnel)
        {
            Terminal valueInput = borrowTunnel.InputTerminals.ElementAt(0),
                borrowOutput = borrowTunnel.OutputTerminals.ElementAt(0);
            _nodeFacade[valueInput] = new SimpleTerminalFacade(valueInput);
            _nodeFacade[borrowOutput] = new SimpleTerminalFacade(borrowOutput);
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitIterateTunnel(IterateTunnel iterateTunnel)
        {
            Terminal iteratorInput = iterateTunnel.InputTerminals.ElementAt(0),
                borrowOutput = iterateTunnel.OutputTerminals.ElementAt(0);
            _nodeFacade.CreateInputLifetimeGroup(InputReferenceMutability.RequireMutable).AddTerminalFacade(iteratorInput);
            _nodeFacade[borrowOutput] = new SimpleTerminalFacade(borrowOutput);
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitLockTunnel(LockTunnel lockTunnel)
        {
            Terminal lockInput = lockTunnel.InputTerminals.ElementAt(0),
                referenceOutput = lockTunnel.OutputTerminals.ElementAt(0);
            _nodeFacade.CreateInputLifetimeGroup(InputReferenceMutability.AllowImmutable).AddTerminalFacade(lockInput);
            _nodeFacade[referenceOutput] = new SimpleTerminalFacade(referenceOutput);
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitLoopConditionTunnel(LoopConditionTunnel loopConditionTunnel)
        {
            // TODO: how to determine the mutability of the outer loop condition variable?
            Terminal loopConditionInput = loopConditionTunnel.InputTerminals.ElementAt(0),
                loopConditionOutput = loopConditionTunnel.OutputTerminals.ElementAt(0);
            _nodeFacade[loopConditionInput] = new SimpleTerminalFacade(loopConditionInput);
            _nodeFacade[loopConditionOutput] = new SimpleTerminalFacade(loopConditionOutput);
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitTunnel(Tunnel tunnel)
        {
            Terminal valueInput = tunnel.InputTerminals.ElementAt(0),
                valueOutput = tunnel.OutputTerminals.ElementAt(0);
            _nodeFacade[valueInput] = new SimpleTerminalFacade(valueInput);
            _nodeFacade[valueOutput] = new SimpleTerminalFacade(valueOutput);
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitTerminateLifetimeTunnel(TerminateLifetimeTunnel terminateLifetimeTunnel)
        {
            Terminal valueOutput = terminateLifetimeTunnel.OutputTerminals.ElementAt(0);
            var valueFacade = new SimpleTerminalFacade(valueOutput);
            _nodeFacade[valueOutput] = valueFacade;

            NationalInstruments.Dfir.BorderNode beginLifetimeBorderNode = (NationalInstruments.Dfir.BorderNode)terminateLifetimeTunnel.BeginLifetimeTunnel;
            Terminal beginLifetimeTerminal = beginLifetimeBorderNode.GetOuterTerminal(0);
            valueFacade.FacadeVariable.MergeInto(beginLifetimeTerminal.GetFacadeVariable());
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitUnwrapOptionTunnel(UnwrapOptionTunnel unwrapOptionTunnel)
        {
            Terminal optionInput = unwrapOptionTunnel.InputTerminals.ElementAt(0),
                unwrappedOutput = unwrapOptionTunnel.OutputTerminals.ElementAt(0);
            _nodeFacade[optionInput] = new SimpleTerminalFacade(optionInput);
            _nodeFacade[unwrappedOutput] = new SimpleTerminalFacade(unwrappedOutput);
            return true;
        }
    }
}
