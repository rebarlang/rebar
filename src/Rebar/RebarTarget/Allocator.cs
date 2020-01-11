using System;
using System.Collections.Generic;
using System.Linq;
using LLVMSharp;
using NationalInstruments;
using NationalInstruments.Compiler;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler;
using Rebar.Compiler.Nodes;
using Rebar.RebarTarget.LLVM;

namespace Rebar.RebarTarget
{
    /// <summary>
    /// Transform that associates a local slot with each <see cref="VariableReference"/> in a <see cref="DfirRoot"/>.
    /// </summary>
    /// <remarks>For now, the implementation is the most naive one possible; it assigns every Variable
    /// its own unique local slot. Future implementations can improve on this by:
    /// * Determining when variables from two different sets can reuse local slots
    /// * Using the same frame space for variables of different types
    /// * Determining when semantic variables are actually constants and thus do not need to be
    /// allocated in the frame</remarks>
    internal class Allocator : VisitorTransformBase, IDfirNodeVisitor<bool>, IInternalDfirNodeVisitor<bool>
    {
        private readonly Dictionary<VariableReference, ValueSource> _variableAllocations;
        private readonly Dictionary<object, ValueSource> _additionalAllocations;
        private readonly Dictionary<VariableReference, VariableUsage> _variableUsages;

        public Allocator(
            Dictionary<VariableReference, ValueSource> variableAllocations,
            Dictionary<object, ValueSource> additionalAllocations)
        {
            _variableAllocations = variableAllocations;
            _additionalAllocations = additionalAllocations;
            _variableUsages = VariableReference.CreateDictionaryWithUniqueVariableKeys<VariableUsage>();
        }

        public FunctionAllocationSet AllocationSet { get; } = new FunctionAllocationSet();

        private VariableUsage GetVariableUsageForVariable(VariableReference variable)
        {
            VariableUsage usage;
            if (!_variableUsages.TryGetValue(variable, out usage))
            {
                usage = new VariableUsage();
                _variableUsages[variable] = usage;
            }
            return usage;
        }

        private string VariableAllocationName(VariableReference variable)
        {
            return $"v{variable.Id}";
        }

        public override void Execute(DfirRoot dfirRoot, CompileCancellationToken cancellationToken)
        {
            base.Execute(dfirRoot, cancellationToken);

            // Now take all of the VariableUsages collected and create appropriate ValueSources from them
            var valueSources = new Dictionary<VariableUsage, ValueSource>();
            foreach (var pair in _variableUsages)
            {
                VariableReference variable = pair.Key;
                VariableUsage usage = pair.Value;

                _variableAllocations[variable] = GetValueSourceForUsage(variable, usage, valueSources);
            }
        }

        private ValueSource GetValueSourceForUsage(VariableReference variable, VariableUsage usage, Dictionary<VariableUsage, ValueSource> valueSources)
        {
            ValueSource valueSource;
            if (!valueSources.TryGetValue(usage, out valueSource))
            {
                valueSource = CreateValueSourceFromUsage(variable, usage, valueSources);
                valueSources[usage] = valueSource;
            }
            return valueSource;
        }

        private ValueSource CreateValueSourceFromUsage(VariableReference variable, VariableUsage usage, Dictionary<VariableUsage, ValueSource> otherValueSources)
        {
            if (!usage.TakesAddress && !usage.UpdatesValue && !variable.Mutable)
            {
                LLVMValueRef constantValue;
                if (usage.TryGetConstantInitialValue(out constantValue))
                {
                    return new ConstantValueSource(constantValue);
                }
            }
            if (usage.ReferencedVariableUsage != null && !variable.Mutable)
            {
                ValueSource referencedValueSource = GetValueSourceForUsage(usage.ReferencedVariable, usage.ReferencedVariableUsage, otherValueSources);
                var referencedConstantValueSource = referencedValueSource as ConstantValueSource;
                return referencedConstantValueSource != null
                    ? (ValueSource)new ReferenceToConstantValueSource(referencedConstantValueSource)
                    : new ConstantLocalReferenceValueSource((IAddressableValueSource)referencedValueSource);
            }
            return AllocationSet.CreateStateField(VariableAllocationName(variable), variable.Type);
        }

        protected override void VisitBorderNode(NationalInstruments.Dfir.BorderNode borderNode)
        {
            InitializeVariableUsagesForAllTerminals(borderNode);
            this.VisitRebarNode(borderNode);
        }

        protected override void VisitNode(Node node)
        {
            InitializeVariableUsagesForAllTerminals(node);
            this.VisitRebarNode(node);
        }

        protected override void VisitWire(Wire wire)
        {
            InitializeVariableUsagesForAllTerminals(wire);
            if (!wire.SinkTerminals.HasMoreThan(1))
            {
                return;
            }
            VariableReference sourceVariable = wire.SourceTerminal.GetTrueVariable();
            VariableUsage sourceVariableUsage = GetVariableUsageForVariable(sourceVariable);
            foreach (var sinkVariable in wire.SinkTerminals.Skip(1).Select(VariableExtensions.GetTrueVariable))
            {
                VariableUsage sinkVariableUsage = GetVariableUsageForVariable(sinkVariable);
                sinkVariableUsage.IsReferenceToVariable(sourceVariableUsage.ReferencedVariable, sourceVariableUsage.ReferencedVariableUsage);
                // TODO: this may create cases where different sinks have different characteristics, requiring
                // code to transfer value from source to individual sinks
            }
        }

        private void InitializeVariableUsagesForAllTerminals(Node node)
        {
            foreach (Terminal terminal in node.Terminals)
            {
                VariableReference variable = terminal.GetTrueVariable();
                if (variable.IsValid && !_variableUsages.ContainsKey(variable))
                {
                    _variableUsages[variable] = new VariableUsage();
                }
            }
        }

        protected override void VisitDfirRoot(DfirRoot dfirRoot)
        {
            dfirRoot.DataItems.OrderBy(d => d.ConnectorPaneIndex).ForEach(VisitDataItem);
            base.VisitDfirRoot(dfirRoot);
        }

        private void VisitDataItem(DataItem dataItem)
        {
            VariableReference dataItemVariable = dataItem.GetVariable();
            _variableAllocations[dataItemVariable] = dataItem.IsInput
                ? AllocationSet.CreateStateField(VariableAllocationName(dataItemVariable), dataItemVariable.Type)
                : AllocationSet.CreateOutputParameter(VariableAllocationName(dataItemVariable), dataItemVariable.Type);
        }

        #region IDfirNodeVisitor implementation
        // This visitor implementation parallels that of SetVariableTypesTransform:
        // For each variable created by a visited node, this should determine the appropriate ValueSource for that variable.

        public bool VisitBorrowTunnel(BorrowTunnel borrowTunnel)
        {
            VariableReference inputVariable = borrowTunnel.InputTerminals[0].GetTrueVariable(),
                outputVariable = borrowTunnel.OutputTerminals[0].GetTrueVariable();
            GetVariableUsageForVariable(outputVariable).IsReferenceToVariable(inputVariable, GetVariableUsageForVariable(inputVariable));
            return true;
        }

        public bool VisitBuildTupleNode(BuildTupleNode buildTupleNode)
        {
            // TODO: for each input variable, note that it is moved into another data structure
            return true;
        }

        public bool VisitConstant(Constant constant)
        {
            VariableReference outputVariable = constant.OutputTerminal.GetTrueVariable();
            if (outputVariable.Type.IsInteger())
            {
                GetVariableUsageForVariable(outputVariable).WillInitializeWithCompileTimeConstant(
                    constant.Value.GetIntegerValue(outputVariable.Type));
            }
            else if (outputVariable.Type.IsBoolean())
            {
                GetVariableUsageForVariable(outputVariable).WillInitializeWithCompileTimeConstant(
                    ((bool)constant.Value).AsLLVMValue());
            }
            return true;
        }

        public bool VisitDataAccessor(DataAccessor dataAccessor)
        {
            // For now, create a local allocation and copy the parameter value into it.
            return true;
        }

        public bool VisitDecomposeTupleNode(DecomposeTupleNode decomposeTupleNode)
        {
            // TODO: for Borrow mode, mark each output reference as a struct offset of the input reference
            return true;
        }

        public bool VisitDropNode(DropNode dropNode)
        {
            // TODO: if the input type has a Drop function, address is taken
            // This will likely require sharing code with FunctionCompiler.TryGetDropFunction
            return true;
        }

        public bool VisitExplicitBorrowNode(ExplicitBorrowNode explicitBorrowNode)
        {
            foreach (KeyValuePair<Terminal, Terminal> terminalPair in explicitBorrowNode.InputTerminals.Zip(explicitBorrowNode.OutputTerminals))
            {
                VariableReference inputVariable = terminalPair.Key.GetTrueVariable(),
                    outputVariable = terminalPair.Value.GetTrueVariable();
                if (inputVariable.Type == outputVariable.Type || inputVariable.Type.IsReferenceToSameTypeAs(outputVariable.Type))
                {
                    _variableUsages[outputVariable] = _variableUsages[inputVariable];
                }
                else
                {
                    // TODO: there is a bug here with creating a reference to an immutable reference binding;
                    // in CreateReferenceValueSource we create a constant reference value source for the immutable reference,
                    // which means we can't create a reference to an allocation for it.
                    GetVariableUsageForVariable(outputVariable).IsReferenceToVariable(inputVariable, GetVariableUsageForVariable(inputVariable));
                }
            }
            return true;
        }

        public bool VisitFunctionalNode(FunctionalNode functionalNode)
        {
            VisitFunctionSignatureNode(functionalNode, functionalNode.Signature);
            return true;
        }

        public bool VisitIterateTunnel(IterateTunnel iterateTunnel)
        {
            GetVariableUsageForVariable(iterateTunnel.InputTerminals[0].GetTrueVariable()).WillGetValue();

            VariableReference outputVariable = iterateTunnel.OutputTerminals[0].GetTrueVariable();
            _additionalAllocations[iterateTunnel.IntermediateValueName] =
                AllocationSet.CreateStateField(iterateTunnel.IntermediateValueName, outputVariable.Type.CreateOption());

            LoopConditionTunnel conditionTunnel = ((Compiler.Nodes.Loop)iterateTunnel.ParentStructure).BorderNodes.OfType<LoopConditionTunnel>().First();
            GetVariableUsageForVariable(conditionTunnel.InputTerminals[0].GetTrueVariable()).WillUpdateValue();
            return true;
        }

        public bool VisitLockTunnel(LockTunnel lockTunnel)
        {
            return true;
        }

        public bool VisitLoopConditionTunnel(LoopConditionTunnel loopConditionTunnel)
        {
            Terminal inputTerminal = loopConditionTunnel.InputTerminals[0],
                outputTerminal = loopConditionTunnel.OutputTerminals[0];
            VariableReference inputVariable = inputTerminal.GetTrueVariable(),
                outputVariable = outputTerminal.GetTrueVariable();
            VariableUsage inputVariableUsage = GetVariableUsageForVariable(inputVariable);
            if (!inputTerminal.IsConnected)
            {
                inputVariableUsage.WillInitializeWithCompileTimeConstant(true.AsLLVMValue());
            }
            inputVariableUsage.WillGetValue();

            GetVariableUsageForVariable(outputVariable).IsReferenceToVariable(inputVariable, inputVariableUsage);
            return true;
        }

        public bool VisitMethodCallNode(MethodCallNode methodCallNode)
        {
            VisitFunctionSignatureNode(methodCallNode, methodCallNode.Signature);
            return true;
        }

        public bool VisitOptionPatternStructureSelector(OptionPatternStructureSelector optionPatternStructureSelector)
        {
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

        public bool VisitTunnel(Tunnel tunnel)
        {
            if (tunnel.Terminals.HasExactly(2))
            {
                VariableReference inputVariable = tunnel.InputTerminals.ElementAt(0).GetTrueVariable(),
                    outputVariable = tunnel.OutputTerminals.ElementAt(0).GetTrueVariable();
                if (outputVariable.Type != inputVariable.Type.CreateOption())
                {
                    // TODO: maybe it's better to compute variable usage for the input and output separately, and only reuse
                    // the ValueSource if they are close enough
                    _variableUsages[outputVariable] = _variableUsages[inputVariable];
                }
            }
            else
            {
                // If this is an output tunnel, each input variable already has its own allocation, but
                // the output needs a distinct one (for now)
                // (Eventually we should try to share a single allocation for all variables.)
                if (tunnel.InputTerminals.HasFewerThanOrExactly(1))
                {
                    throw new NotImplementedException();
                }
            }
            return true;
        }

        public bool VisitUnwrapOptionTunnel(UnwrapOptionTunnel unwrapOptionTunnel)
        {
            // TODO: it would be nice to allow the output value source to reference an offset from the input value source,
            // rather than needing a separate allocation.
            return true;
        }

        private void VisitFunctionSignatureNode(Node node, NIType nodeFunctionSignature)
        {
            switch (nodeFunctionSignature.GetName())
            {
                case "Assign":
                    VariableUsage input0Usage = GetVariableUsageForVariable(node.InputTerminals[0].GetTrueVariable());
                    input0Usage.WillUpdateDereferencedValue();
                    // TODO: only if deref type is Drop
                    input0Usage.WillGetValue();
                    VariableUsage input1Usage = GetVariableUsageForVariable(node.InputTerminals[1].GetTrueVariable());
                    input1Usage.WillGetValue();
                    return;
                case "Exchange":
                    input0Usage = GetVariableUsageForVariable(node.InputTerminals[0].GetTrueVariable());
                    input0Usage.WillGetDereferencedValue();
                    input0Usage.WillUpdateDereferencedValue();
                    input1Usage = GetVariableUsageForVariable(node.InputTerminals[1].GetTrueVariable());
                    input1Usage.WillGetDereferencedValue();
                    input1Usage.WillUpdateDereferencedValue();
                    return;
                case "CreateCopy":
                    VariableReference input0Variable = node.InputTerminals[0].GetTrueVariable();
                    input0Usage = GetVariableUsageForVariable(input0Variable);
                    if (input0Variable.Type.GetReferentType().WireTypeMayFork())
                    {
                        input0Usage.WillGetDereferencedValue();
                    }
                    else
                    {
                        input0Usage.WillGetValue();
                    }
                    return;
                case "SelectReference":
                    input0Usage = GetVariableUsageForVariable(node.InputTerminals[0].GetTrueVariable());
                    input0Usage.WillGetDereferencedValue();
                    input1Usage = GetVariableUsageForVariable(node.InputTerminals[1].GetTrueVariable());
                    input1Usage.WillGetValue();
                    VariableUsage input2Usage = GetVariableUsageForVariable(node.InputTerminals[2].GetTrueVariable());
                    input2Usage.WillGetValue();
                    return;
                case "Increment":
                case "Not":
                    input0Usage = GetVariableUsageForVariable(node.InputTerminals[0].GetTrueVariable());
                    input0Usage.WillGetDereferencedValue();
                    return;
                case "AccumulateIncrement":
                case "AccumulateNot":
                    input0Usage = GetVariableUsageForVariable(node.InputTerminals[0].GetTrueVariable());
                    input0Usage.WillGetDereferencedValue();
                    input0Usage.WillUpdateDereferencedValue();
                    return;
                case "Add":
                case "Subtract":
                case "Multiply":
                case "Divide":
                case "Modulus":
                case "And":
                case "Or":
                case "Xor":
                case "Equal":
                case "NotEqual":
                case "LessThan":
                case "LessEqual":
                case "GreaterThan":
                case "GreaterEqual":
                    input0Usage = GetVariableUsageForVariable(node.InputTerminals[0].GetTrueVariable());
                    input0Usage.WillGetDereferencedValue();
                    input1Usage = GetVariableUsageForVariable(node.InputTerminals[1].GetTrueVariable());
                    input1Usage.WillGetDereferencedValue();
                    return;
                case "AccumulateAdd":
                case "AccumulateSubtract":
                case "AccumulateMultiply":
                case "AccumulateDivide":
                case "AccumulateAnd":
                case "AccumulateOr":
                case "AccumulateXor":
                    input0Usage = GetVariableUsageForVariable(node.InputTerminals[0].GetTrueVariable());
                    input0Usage.WillGetDereferencedValue();
                    input0Usage.WillUpdateDereferencedValue();
                    input1Usage = GetVariableUsageForVariable(node.InputTerminals[1].GetTrueVariable());
                    input1Usage.WillGetDereferencedValue();
                    return;
                case "NoneConstructor":
                    {
                        VariableReference outputVariable = node.OutputTerminals[0].GetTrueVariable();
                        LLVMTypeRef optionType = outputVariable.Type.AsLLVMType();
                        GetVariableUsageForVariable(outputVariable).WillInitializeWithCompileTimeConstant(LLVMSharp.LLVM.ConstNull(optionType));
                    }
                    return;
                case "Output":
                case "Inspect":
                    GetVariableUsageForVariable(node.InputTerminals[0].GetTrueVariable()).WillGetDereferencedValue();
                    return;
                case "ImmutPass":
                case "MutPass":
                    return;
            }

            Signature signature = Signatures.GetSignatureForNIType(nodeFunctionSignature);
            foreach (var terminalPair in node.InputTerminals.Zip(signature.Inputs))
            {
                VariableReference inputVariable = terminalPair.Key.GetTrueVariable();
                GetVariableUsageForVariable(inputVariable).WillGetValue();
            }
        }

        #endregion

        #region IInternalDfirNodeVisitor implementation

        bool IInternalDfirNodeVisitor<bool>.VisitAwaitNode(AwaitNode awaitNode)
        {
            VariableReference inputVariable = awaitNode.InputTerminal.GetTrueVariable();
            GetVariableUsageForVariable(inputVariable).WillTakeAddress();
            // It may be the case that our output variable is the same as an upstream variable (e.g., because
            // it represents a passthrough of an async node). In that case we should reuse the value source
            // we already have for it.
            return true;
        }

        bool IInternalDfirNodeVisitor<bool>.VisitCreateMethodCallPromise(CreateMethodCallPromise createMethodCallPromise)
        {
            foreach (Terminal inputTerminal in createMethodCallPromise.InputTerminals)
            {
                VariableReference inputVariable = inputTerminal.GetTrueVariable();
                GetVariableUsageForVariable(inputVariable).WillGetValue();
            }
            return true;
        }

#endregion

        private class VariableUsage
        {
            private LLVMValueRef? _constantValue;

            public VariableUsage ReferencedVariableUsage { get; private set; }

            public VariableReference ReferencedVariable { get; private set; }

            public bool GetsValue { get; private set; }

            public bool TakesAddress { get; private set; }

            public bool TryGetConstantInitialValue(out LLVMValueRef value)
            {
                if (_constantValue != null)
                {
                    value = _constantValue.Value;
                    return true;
                }
                else
                {
                    value = default(LLVMValueRef);
                    return false;
                }
            }

            public bool UpdatesValue { get; private set; }

            public void IsReferenceToVariable(VariableReference variable, VariableUsage usage)
            {
                ReferencedVariable = variable;
                ReferencedVariableUsage = usage;
            }

            public void WillGetValue()
            {
                GetsValue = true;
                ReferencedVariableUsage?.WillTakeAddress();
            }

            public void WillGetDereferencedValue()
            {
                ReferencedVariableUsage?.WillGetValue();
            }

            public void WillTakeAddress()
            {
                TakesAddress = true;
            }

            public void WillInitializeWithCompileTimeConstant(LLVMValueRef constantValue)
            {
                _constantValue = constantValue;
            }

            public void WillUpdateValue()
            {
                UpdatesValue = true;
            }

            public void WillUpdateDereferencedValue()
            {
                ReferencedVariableUsage?.WillUpdateValue();
            }
        }
    }
}
