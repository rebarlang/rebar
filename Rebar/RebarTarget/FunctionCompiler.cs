using System;
using System.Collections.Generic;
using System.Linq;
using NationalInstruments;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Compiler.Nodes;
using Rebar.Common;
using Rebar.Compiler;
using Rebar.RebarTarget.Execution;

namespace Rebar.RebarTarget
{
    internal class FunctionCompiler : VisitorTransformBase, IDfirNodeVisitor<bool>, IDfirStructureVisitor<bool>
    {
        private readonly FunctionBuilder _builder;
        private readonly Dictionary<Variable, Allocation> _variableAllocations;

        public FunctionCompiler(FunctionBuilder builder, Dictionary<Variable, Allocation> variableAllocations)
        {
            _builder = builder;
            _variableAllocations = variableAllocations;
        }

        private byte GetVariableIndex(Variable variable)
        {
            return (byte)_variableAllocations[variable].Index;
        }

        private void LoadLocalAddressAndDerefIfReference(Variable local)
        {
            _builder.EmitLoadLocalAddress(GetVariableIndex(local));
            if (local.Type.IsRebarReferenceType())
            {
                _builder.EmitDerefPointer();
            }
        }

        private void BorrowFromVariableIntoVariable(Variable from, Variable into)
        {
            _builder.EmitLoadLocalAddress(GetVariableIndex(into));
            _builder.EmitLoadLocalAddress(GetVariableIndex(from));
            _builder.EmitStorePointer();
        }

        private void EmitUnaryOperationOnVariable(Variable variable, UnaryPrimitiveOps operation)
        {
            switch (operation)
            {
                case UnaryPrimitiveOps.Increment:
                    _builder.EmitLoadIntegerImmediate(1);
                    LoadLocalAddressAndDerefIfReference(variable);
                    _builder.EmitDerefInteger();
                    _builder.EmitAdd();
                    break;
                case UnaryPrimitiveOps.Not:
                    _builder.EmitLoadIntegerImmediate(1);
                    LoadLocalAddressAndDerefIfReference(variable);
                    _builder.EmitDerefInteger();
                    _builder.EmitSubtract();
                    break;
            }
        }

        private void EmitBinaryOperation(BinaryPrimitiveOps operation)
        {
            switch (operation)
            {
                case BinaryPrimitiveOps.Add:
                    _builder.EmitAdd();
                    break;
                case BinaryPrimitiveOps.Subtract:
                    _builder.EmitSubtract();
                    break;
                case BinaryPrimitiveOps.Multiply:
                    _builder.EmitMultiply();
                    break;
                case BinaryPrimitiveOps.Divide:
                    _builder.EmitDivide();
                    break;
                case BinaryPrimitiveOps.And:
                    _builder.EmitAnd();
                    break;
                case BinaryPrimitiveOps.Or:
                    _builder.EmitOr();
                    break;
                case BinaryPrimitiveOps.Xor:
                    _builder.EmitXor();
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public bool VisitAssignNode(AssignNode assignNode)
        {
            Variable assignee = assignNode.InputTerminals.ElementAt(0).GetVariable(),
                value = assignNode.InputTerminals.ElementAt(1).GetVariable();
            LoadLocalAddressAndDerefIfReference(assignee);
            _builder.EmitLoadLocalAddress(GetVariableIndex(value));
            _builder.EmitDerefInteger();
            _builder.EmitStoreInteger();
            return true;
        }

        public bool VisitBorrowTunnel(BorrowTunnel borrowTunnel)
        {
            Variable input = borrowTunnel.InputTerminals.ElementAt(0).GetVariable(),
                output = borrowTunnel.OutputTerminals.ElementAt(0).GetVariable();
            BorrowFromVariableIntoVariable(input, output);
            return true;
        }

        public bool VisitConstant(Constant constant)
        {
            var output = constant.OutputTerminal.GetVariable();
            if (constant.Value is int)
            {
                _builder.EmitLoadLocalAddress(GetVariableIndex(output));
                _builder.EmitLoadIntegerImmediate((int)constant.Value);
                _builder.EmitStoreInteger();
            }
            else if (constant.Value is bool)
            {
                _builder.EmitLoadLocalAddress(GetVariableIndex(output));
                _builder.EmitLoadIntegerImmediate((bool)constant.Value ? 1 : 0);
                _builder.EmitStoreInteger();
            }
            return true;
        }

        public bool VisitCreateCellNode(CreateCellNode createCellNode)
        {
            throw new NotImplementedException();
        }

        public bool VisitCreateCopyNode(CreateCopyNode createCopyNode)
        {
            Variable copyFrom = createCopyNode.InputTerminals.ElementAt(0).GetVariable(),
                copyTo = createCopyNode.OutputTerminals.ElementAt(1).GetVariable();
            _builder.EmitLoadLocalAddress(GetVariableIndex(copyTo));
            LoadLocalAddressAndDerefIfReference(copyFrom);
            _builder.EmitDerefInteger();
            _builder.EmitStoreInteger();
            return true;
        }

        public bool VisitDropNode(DropNode dropNode)
        {
            throw new NotImplementedException();
        }

        public bool VisitExchangeValuesNode(ExchangeValuesNode exchangeValuesNode)
        {
            Variable var1 = exchangeValuesNode.InputTerminals.ElementAt(0).GetVariable(),
                var2 = exchangeValuesNode.InputTerminals.ElementAt(1).GetVariable();
            LoadLocalAddressAndDerefIfReference(var2);
            LoadLocalAddressAndDerefIfReference(var1);
            _builder.EmitDuplicate();
            _builder.EmitDerefInteger();
            _builder.EmitSwap();
            LoadLocalAddressAndDerefIfReference(var2);
            _builder.EmitDerefInteger();
            _builder.EmitStoreInteger();
            _builder.EmitStoreInteger();
            return true;
        }

        public bool VisitExplicitBorrowNode(ExplicitBorrowNode explicitBorrowNode)
        {
            Variable input = explicitBorrowNode.InputTerminal.GetVariable(),
                output = explicitBorrowNode.OutputTerminal.GetVariable();
            BorrowFromVariableIntoVariable(input, output);
            return true;
        }

        public bool VisitImmutablePassthroughNode(ImmutablePassthroughNode immutablePassthroughNode)
        {
            return true;
        }

        public bool VisitLockTunnel(LockTunnel lockTunnel)
        {
            throw new NotImplementedException();
        }

        public bool VisitMutablePassthroughNode(MutablePassthroughNode mutablePassthroughNode)
        {
            return true;
        }

        public bool VisitMutatingBinaryPrimitive(MutatingBinaryPrimitive mutatingBinaryPrimitive)
        {
            Variable input1 = mutatingBinaryPrimitive.InputTerminals.ElementAt(0).GetVariable(),
                input2 = mutatingBinaryPrimitive.InputTerminals.ElementAt(1).GetVariable();
            LoadLocalAddressAndDerefIfReference(input1);
            _builder.EmitDuplicate();
            _builder.EmitDerefInteger();
            LoadLocalAddressAndDerefIfReference(input2);
            _builder.EmitDerefInteger();
            EmitBinaryOperation(mutatingBinaryPrimitive.Operation);
            _builder.EmitStoreInteger();
            return true;
        }

        public bool VisitMutatingUnaryPrimitive(MutatingUnaryPrimitive mutatingUnaryPrimitive)
        {
            Variable input = mutatingUnaryPrimitive.InputTerminals.ElementAt(0).GetVariable();
            LoadLocalAddressAndDerefIfReference(input);
            EmitUnaryOperationOnVariable(input, mutatingUnaryPrimitive.Operation);
            _builder.EmitStoreInteger();
            return true;
        }

        public bool VisitOutputNode(OutputNode outputNode)
        {
            Variable input = outputNode.InputTerminals.ElementAt(0).GetVariable();
            LoadLocalAddressAndDerefIfReference(input);
            _builder.EmitDerefInteger();
            _builder.EmitOutput_TEMP();
            return true;
        }

        public bool VisitPureBinaryPrimitive(PureBinaryPrimitive pureBinaryPrimitive)
        {
            Variable input1 = pureBinaryPrimitive.InputTerminals.ElementAt(0).GetVariable(),
                input2 = pureBinaryPrimitive.InputTerminals.ElementAt(1).GetVariable(),
                output = pureBinaryPrimitive.OutputTerminals.ElementAt(2).GetVariable();
            _builder.EmitLoadLocalAddress(GetVariableIndex(output));
            LoadLocalAddressAndDerefIfReference(input1);
            _builder.EmitDerefInteger();
            LoadLocalAddressAndDerefIfReference(input2);
            _builder.EmitDerefInteger();
            EmitBinaryOperation(pureBinaryPrimitive.Operation);
            _builder.EmitStoreInteger();
            return true;
        }

        public bool VisitPureUnaryPrimitive(PureUnaryPrimitive pureUnaryPrimitive)
        {
            Variable input = pureUnaryPrimitive.InputTerminals.ElementAt(0).GetVariable(),
                output = pureUnaryPrimitive.OutputTerminals.ElementAt(1).GetVariable();
            _builder.EmitLoadLocalAddress(GetVariableIndex(output));
            EmitUnaryOperationOnVariable(input, pureUnaryPrimitive.Operation);
            _builder.EmitStoreInteger();
            return true;
        }

        public bool VisitRangeNode(RangeNode rangeNode)
        {
            Variable lowInput = rangeNode.InputTerminals.ElementAt(0).GetVariable(),
                highInput = rangeNode.InputTerminals.ElementAt(1).GetVariable(),
                output = rangeNode.OutputTerminals.ElementAt(0).GetVariable();

            _builder.EmitLoadLocalAddress(GetVariableIndex(output));
            _builder.EmitDuplicate();
            _builder.EmitLoadLocalAddress(GetVariableIndex(lowInput));
            _builder.EmitDerefInteger();
            _builder.EmitLoadIntegerImmediate(1);
            _builder.EmitSubtract();
            _builder.EmitStoreInteger();

            _builder.EmitLoadIntegerImmediate(4);
            _builder.EmitAdd();
            _builder.EmitLoadLocalAddress(GetVariableIndex(highInput));
            _builder.EmitDerefInteger();
            _builder.EmitStoreInteger();
            return true;
        }

        public bool VisitSelectReferenceNode(SelectReferenceNode selectReferenceNode)
        {
            LabelBuilder falseLabel = _builder.CreateLabel(),
                endLabel = _builder.CreateLabel();
            // handle auto-borrowed tunnels
            // NOTE: this is a great example of why it would be better to have an auto-borrowing
            // transform
            Variable input1 = selectReferenceNode.InputTerminals.ElementAt(1).GetVariable(),
                input2 = selectReferenceNode.InputTerminals.ElementAt(2).GetVariable();
            bool input1IsReference = input1.Type.IsRebarReferenceType(),
                input2IsReference = input2.Type.IsRebarReferenceType();

            // select reference
            Variable selector = selectReferenceNode.InputTerminals.ElementAt(0).GetVariable(),
                selectedReference = selectReferenceNode.OutputTerminals.ElementAt(1).GetVariable();
            _builder.EmitLoadLocalAddress(GetVariableIndex(selectedReference));            
            LoadLocalAddressAndDerefIfReference(selector);
            _builder.EmitDerefInteger();
            _builder.EmitBranchIfFalse(falseLabel);

            // true
            LoadLocalAddressAndDerefIfReference(input1);
            _builder.EmitBranch(endLabel);

            // false
            _builder.SetLabel(falseLabel);
            LoadLocalAddressAndDerefIfReference(input2);

            // end
            _builder.SetLabel(endLabel);
            _builder.EmitStorePointer();

            return true;
        }

        public bool VisitSomeConstructorNode(SomeConstructorNode someConstructorNode)
        {
            Variable input = someConstructorNode.InputTerminals.ElementAt(0).GetVariable(),
                output = someConstructorNode.OutputTerminals.ElementAt(0).GetVariable();
            _builder.EmitLoadLocalAddress(GetVariableIndex(output));
            _builder.EmitDuplicate();
            _builder.EmitLoadIntegerImmediate(1);
            _builder.EmitStoreInteger();
            _builder.EmitLoadIntegerImmediate(4);
            _builder.EmitAdd();
            _builder.EmitLoadLocalAddress(GetVariableIndex(input));
            if (input.Type.IsRebarReferenceType())
            {
                _builder.EmitDerefPointer();
                _builder.EmitStorePointer();
            }
            else
            {
                _builder.EmitDerefInteger();
                _builder.EmitStoreInteger();
            }
            return true;
        }

        public bool VisitTerminateLifetimeNode(TerminateLifetimeNode terminateLifetimeNode)
        {
            return true;
        }

        public bool VisitTerminateLifetimeTunnel(TerminateLifetimeTunnel terminateLifetimeTunnel)
        {
            return true;
        }
        
        public bool VisitVectorCreateNode(VectorCreateNode vectorCreateNode)
        {
            throw new NotImplementedException();
        }

        public bool VisitVectorInsertNode(VectorInsertNode vectorCreateNode)
        {
            throw new NotImplementedException();
        }

        private void CopyValue(NIType type)
        {
            // TODO: this should just be a generic Deref and Store
            if (type.IsRebarReferenceType())
            {
                _builder.EmitDerefPointer();
                _builder.EmitStorePointer();
            }
            else
            {
                _builder.EmitDerefInteger();
                _builder.EmitStoreInteger();
            }
        }

        public bool VisitTunnel(Tunnel tunnel)
        {
            Variable input = tunnel.InputTerminals.ElementAt(0).GetVariable(),
                output = tunnel.OutputTerminals.ElementAt(0).GetVariable();
            if (output.Type == input.Type.CreateOption())
            {
                _builder.EmitLoadLocalAddress(GetVariableIndex(output));
                _builder.EmitLoadIntegerImmediate(1);
                _builder.EmitStoreInteger();
                _builder.EmitLoadLocalAddress(GetVariableIndex(output));
                _builder.EmitLoadIntegerImmediate(4);
                _builder.EmitAdd();
                _builder.EmitLoadLocalAddress(GetVariableIndex(input));
                CopyValue(input.Type);
                return true;
            }

            byte inputIndex = GetVariableIndex(input), outputIndex = GetVariableIndex(output);
            if (inputIndex != outputIndex)
            {
                _builder.EmitLoadLocalAddress(GetVariableIndex(output));
                _builder.EmitLoadLocalAddress(GetVariableIndex(input));
                CopyValue(input.Type);
            }
            return true;
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
            if (wire.SinkTerminals.HasMoreThan(1))
            {
                Variable sourceVariable = wire.SourceTerminal.GetVariable();
                Variable[] sinkVariables = wire.SinkTerminals.Skip(1).Select(VariableSetExtensions.GetVariable).ToArray();
                foreach (var sinkVariable in sinkVariables)
                {
                    _builder.EmitLoadLocalAddress(GetVariableIndex(sinkVariable));
                }
                _builder.EmitLoadLocalAddress(GetVariableIndex(sourceVariable));
                if (sourceVariable.Type.IsRebarReferenceType())
                {
                    _builder.EmitDerefPointer();
                }
                else
                {
                    _builder.EmitDerefInteger();
                }

                for (int i = 0; i < sinkVariables.Length; ++i)
                {
                    Variable sinkVariable = sinkVariables[i];
                    if (i < sinkVariables.Length - 1)
                    {
                        _builder.EmitDuplicate();
                    }
                    // TODO: this should just be a generic Deref and Store
                    if (sinkVariable.Type.IsRebarReferenceType())
                    {
                        _builder.EmitStorePointer();
                    }
                    else
                    {
                        _builder.EmitStoreInteger();
                    }
                }
            }
        }

        protected override void VisitStructure(Structure structure, StructureTraversalPoint traversalPoint)
        {
            base.VisitStructure(structure, traversalPoint);
            this.VisitRebarStructure(structure, traversalPoint);
        }

#region Frame

        private struct FrameData
        {
            public FrameData(LabelBuilder unwrapFailed, LabelBuilder end)
            {
                UnwrapFailed = unwrapFailed;
                End = end;
            }

            public LabelBuilder UnwrapFailed { get; }

            public LabelBuilder End { get; }
        }

        private readonly Dictionary<Frame, FrameData> _frameData = new Dictionary<Frame, FrameData>();

        public bool VisitFrame(Frame frame, StructureTraversalPoint traversalPoint)
        {
            switch (traversalPoint)
            {
                case StructureTraversalPoint.BeforeLeftBorderNodes:
                    VisitFrameBeforeLeftBorderNodes(frame);
                    break;
                case StructureTraversalPoint.AfterRightBorderNodes:
                    VisitFrameAfterRightBorderNodes(frame);
                    break;
            }
            return true;
        }

        private void VisitFrameBeforeLeftBorderNodes(Frame frame)
        {
            LabelBuilder unwrapFailed = _builder.CreateLabel(),
                end = _builder.CreateLabel();
            _frameData[frame] = new FrameData(unwrapFailed, end);
        }

        private void VisitFrameAfterRightBorderNodes(Frame frame)
        {
            FrameData frameData = _frameData[frame];
            _builder.EmitBranch(frameData.End);
            _builder.SetLabel(frameData.UnwrapFailed);
            foreach (Tunnel tunnel in frame.BorderNodes.OfType<Tunnel>().Where(t => t.Direction == Direction.Output))
            {
                // Store a None value for the tunnel
                Variable tunnelOutput = tunnel.OutputTerminals.ElementAt(0).GetVariable();
                _builder.EmitLoadLocalAddress(GetVariableIndex(tunnelOutput));
                _builder.EmitLoadIntegerImmediate(0);
                _builder.EmitStoreInteger();
            }
            _builder.SetLabel(frameData.End);
        }

        public bool VisitUnwrapOptionTunnel(UnwrapOptionTunnel unwrapOptionTunnel)
        {
            FrameData frameData = _frameData[(Frame)unwrapOptionTunnel.ParentStructure];
            Variable tunnelInput = unwrapOptionTunnel.InputTerminals.ElementAt(0).GetVariable(),
                tunnelOutput = unwrapOptionTunnel.OutputTerminals.ElementAt(0).GetVariable();
            _builder.EmitLoadLocalAddress(GetVariableIndex(tunnelInput));
            _builder.EmitDerefInteger();
            _builder.EmitBranchIfFalse(frameData.UnwrapFailed);

            // TODO: we could cheat here and do nothing if we say that the address of the 
            // output is the address of the value within the input
            // (assuming Option<T> always ::= { bool, T })
            _builder.EmitLoadLocalAddress(GetVariableIndex(tunnelOutput));
            _builder.EmitLoadLocalAddress(GetVariableIndex(tunnelInput));
            _builder.EmitLoadIntegerImmediate(4);
            _builder.EmitAdd();
            if (tunnelOutput.Type.IsRebarReferenceType())
            {
                _builder.EmitDerefPointer();
                _builder.EmitStorePointer();
            }
            else
            {
                // TODO
                _builder.EmitDerefInteger();
                _builder.EmitStoreInteger();
            }
            return true;
        }

#endregion

#region Loop

        private struct LoopData
        {
            public LoopData(LabelBuilder start, LabelBuilder end, Variable loopCondition)
            {
                Start = start;
                End = end;
                LoopCondition = loopCondition;
            }

            public LabelBuilder Start { get; }

            public LabelBuilder End { get; }

            public Variable LoopCondition { get; }
        }

        private Dictionary<Compiler.Nodes.Loop, LoopData> _loopData = new Dictionary<Compiler.Nodes.Loop, LoopData>();

        public bool VisitLoop(Compiler.Nodes.Loop loop, StructureTraversalPoint traversalPoint)
        {
            // generate code for each left-side border node;
            // each border node that can affect condition should &&= the LoopCondition
            // variable with whether it allows loop to proceed
            switch (traversalPoint)
            {
                case StructureTraversalPoint.BeforeLeftBorderNodes:
                    VisitLoopBeforeLeftBorderNodes(loop);
                    break;
                case StructureTraversalPoint.AfterLeftBorderNodesAndBeforeDiagram:
                    VisitLoopAfterLeftBorderNodes(loop);
                    break;
                case StructureTraversalPoint.AfterRightBorderNodes:
                    VisitLoopAfterRightBorderNodes(loop);
                    break;
            }
            return true;
        }

        private void VisitLoopBeforeLeftBorderNodes(Compiler.Nodes.Loop loop)
        {
            LabelBuilder start = _builder.CreateLabel(),
                end = _builder.CreateLabel();
            LoopConditionTunnel loopCondition = loop.BorderNodes.OfType<LoopConditionTunnel>().First();
            Terminal loopConditionInput = loopCondition.InputTerminals.ElementAt(0);
            Variable loopConditionVariable = loopConditionInput.GetVariable();
            _loopData[loop] = new LoopData(start, end, loopConditionVariable);

            Variable loopConditionReferenceVariable = loopCondition.OutputTerminals.ElementAt(0).GetVariable();
            _builder.EmitLoadLocalAddress(GetVariableIndex(loopConditionReferenceVariable));
            _builder.EmitLoadLocalAddress(GetVariableIndex(loopConditionVariable));
            if (!loopConditionInput.IsConnected)
            {
                // if loop condition was unwired, initialize it to true
                _builder.EmitDuplicate();
                _builder.EmitLoadIntegerImmediate(1);
                _builder.EmitStoreInteger();
            }
            _builder.EmitStorePointer();

            _builder.SetLabel(start);
        }

        private void VisitLoopAfterLeftBorderNodes(Compiler.Nodes.Loop loop)
        {
            LoopData loopData = _loopData[loop];
            LoadLocalAddressAndDerefIfReference(loopData.LoopCondition);
            _builder.EmitDerefInteger();
            _builder.EmitBranchIfFalse(loopData.End);
        }

        private void VisitLoopAfterRightBorderNodes(Compiler.Nodes.Loop loop)
        {
            LoopData loopData = _loopData[loop];
            _builder.EmitBranch(loopData.Start);
            _builder.SetLabel(loopData.End);
        }

        public bool VisitLoopConditionTunnel(LoopConditionTunnel loopConditionTunnel)
        {
            return true;
        }

        public bool VisitIterateTunnel(IterateTunnel iterateTunnel)
        {
            // TODO: this should eventually call a RangeIterator::next function
            LoopData loopData = _loopData[(Compiler.Nodes.Loop)iterateTunnel.ParentStructure];
            _builder.EmitLoadLocalAddress(GetVariableIndex(loopData.LoopCondition));    // &cond
            _builder.EmitDuplicate();   // &cond, &cond
            _builder.EmitDerefInteger();    // &cond, cond

            Variable rangeInput = iterateTunnel.InputTerminals.ElementAt(0).GetVariable();
            Variable output = iterateTunnel.OutputTerminals.ElementAt(0).GetVariable();
            LoadLocalAddressAndDerefIfReference(rangeInput);    // &range
            _builder.EmitDuplicate();
            _builder.EmitLoadIntegerImmediate(4);
            _builder.EmitAdd();     // &range, &range.max
            _builder.EmitDerefInteger();    // &range, range.max
            _builder.EmitSwap();    // range.max, &range
            _builder.EmitDuplicate();   // range.max, &range, &range
            _builder.EmitDuplicate();   // range.max, &range, &range, &range
            _builder.EmitDerefInteger();    // range.max, &range, &range, range.current
            _builder.EmitLoadIntegerImmediate(1);
            _builder.EmitAdd();     // range.max, &range, &range, range.current+1
            _builder.EmitDuplicate();
            _builder.EmitLoadLocalAddress(GetVariableIndex(output));
            _builder.EmitSwap();            // range.max, &range, &range, range.current+1, &output, range.current+1
            _builder.EmitStoreInteger();    // range.max, &range, &range, range.current+1
            _builder.EmitStoreInteger();    // range.max, &range

            _builder.EmitDerefInteger();    // range.max, range.current
            _builder.EmitGreaterThan();     // &cond, cond, (range.max > range.current)
            _builder.EmitAnd(); // &cond, (cond && range.max > range.current)
            _builder.EmitStoreInteger();
            return true;
        }

#endregion
    }
}
