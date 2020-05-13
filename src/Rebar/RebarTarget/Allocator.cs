using System;
using System.Collections.Generic;
using System.Linq;
using LLVMSharp;
using NationalInstruments;
using NationalInstruments.CommonModel;
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
    internal class Allocator : VisitorTransformBase, IVisitationHandler<bool>, IInternalDfirNodeVisitor<bool>
    {
        private readonly FunctionVariableStorage _variableStorage;
        private readonly IEnumerable<AsyncStateGroup> _asyncStateGroups;
        private readonly Dictionary<VariableReference, VariableUsage> _variableUsages;
        private AsyncStateGroup _currentGroup;
        private readonly string _singleFunctionName;

        public Allocator(
            ContextWrapper context,
            FunctionVariableStorage variableStorage,
            IEnumerable<AsyncStateGroup> asyncStateGroups)
        {
            Context = context;
            AllocationSet = new FunctionAllocationSet(Context);
            _variableStorage = variableStorage;
            _asyncStateGroups = asyncStateGroups;
            _asyncStateGroups.Select(g => g.FunctionId).Distinct().TryGetSingleElement(out _singleFunctionName);
            _variableUsages = VariableReference.CreateDictionaryWithUniqueVariableKeys<VariableUsage>();
        }

        private ContextWrapper Context { get; }

        public FunctionAllocationSet AllocationSet { get; }

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
            // First, execute as a VisitorTransform base to initialize VariableUsages for all variables
            base.Execute(dfirRoot, cancellationToken);

            // Then, handle each Visitation in order to set particular node/wire/structure VariableUsage characteristics
            foreach (AsyncStateGroup group in _asyncStateGroups)
            {
                _currentGroup = group;
                foreach (Visitation visitation in group.Visitations)
                {
                    visitation.Visit(this);
                }
                CreateAllocationsForAsyncStateGroup(group);
            }

            // Finally, take all of the VariableUsages collected and create appropriate ValueSources from them
            var valueSources = new Dictionary<VariableUsage, ValueSource>();
            foreach (var pair in _variableUsages)
            {
                VariableReference variable = pair.Key;
                VariableUsage usage = pair.Value;

                _variableStorage.AddValueSourceForVariable(variable, GetValueSourceForUsage(variable, usage, valueSources));
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
            if (!variable.Mutable)
            {
                if (usage.ReferencedVariableUsage != null)
                {
                    ValueSource referencedValueSource = GetValueSourceForUsage(usage.ReferencedVariable, usage.ReferencedVariableUsage, otherValueSources);
                    var referencedSingleValueSource = referencedValueSource as SingleValueSource;
                    return referencedSingleValueSource != null
                        ? (ValueSource)new ReferenceToSingleValueSource(referencedSingleValueSource)
                        : new ConstantLocalReferenceValueSource((IAddressableValueSource)referencedValueSource);
                }
                if (!usage.TakesAddress && !usage.UpdatesValue)
                {
                    LLVMValueRef constantValue;
                    if (usage.TryGetConstantInitialValue(out constantValue))
                    {
                        return new ConstantValueSource(constantValue);
                    }

                    bool initializedInSkippableGroup = usage.InitializingGroup.IsSkippable;
                    if (!usage.LiveInMultipleFunctions && !initializedInSkippableGroup)
                    {
                        return new ImmutableValueSource();
                    }
                }
            }

            if (!usage.LiveInMultipleFunctions)
            {
                return AllocationSet.CreateLocalAllocation(usage.ContainingFunctionName, VariableAllocationName(variable), variable.Type);
            }
            return AllocationSet.CreateStateField(VariableAllocationName(variable), variable.Type);
        }

        private void CreateAllocationsForAsyncStateGroup(AsyncStateGroup asyncStateGroup)
        {
            var conditionalContinuation = asyncStateGroup.Continuation as ConditionallyScheduleGroupsContinuation;
            if (conditionalContinuation != null)
            {
                NIType conditionVariableType;
                if (conditionalContinuation.SuccessorConditionGroups.Count <= 2)
                {
                    conditionVariableType = NITypes.Boolean;
                }
                else if (conditionalContinuation.SuccessorConditionGroups.Count <= 256)
                {
                    conditionVariableType = NITypes.UInt8;
                }
                else
                {
                    throw new NotSupportedException("Only 256 conditional continuations supported");
                }
                _variableStorage.AddContinuationConditionVariable(
                    asyncStateGroup,
                    AllocationSet.CreateLocalAllocation(asyncStateGroup.FunctionId, $"{asyncStateGroup.Label}_continuationStatePtr", conditionVariableType));
            }
        }

        #region VisitorTransformBase overrides

        protected override void VisitBorderNode(NationalInstruments.Dfir.BorderNode borderNode)
        {
            InitializeVariableUsagesForAllTerminals(borderNode);
        }

        protected override void VisitNode(Node node)
        {
            InitializeVariableUsagesForAllTerminals(node);
        }

        protected override void VisitWire(Wire wire)
        {
            InitializeVariableUsagesForAllTerminals(wire);
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
            ValueSource valueSource;
            if (_singleFunctionName != null)
            {
                valueSource = dataItem.IsInput
                    ? AllocationSet.CreateLocalAllocation(_singleFunctionName, VariableAllocationName(dataItemVariable), dataItemVariable.Type)
                    : AllocationSet.CreateOutputParameterLocalAllocation(_singleFunctionName, VariableAllocationName(dataItemVariable), dataItemVariable.Type);
            }
            else
            {
                valueSource = dataItem.IsInput
                    ? AllocationSet.CreateStateField(VariableAllocationName(dataItemVariable), dataItemVariable.Type)
                    : AllocationSet.CreateOutputParameterStateField(VariableAllocationName(dataItemVariable), dataItemVariable.Type);
            }
            _variableStorage.AddValueSourceForVariable(dataItemVariable, valueSource);
        }

        #endregion

        #region IDfirNodeVisitor implementation
        // This visitor implementation parallels that of SetVariableTypesTransform:
        // For each variable created by a visited node, this should determine the appropriate ValueSource for that variable.

        public bool VisitBorrowTunnel(BorrowTunnel borrowTunnel)
        {
            VariableReference inputVariable = borrowTunnel.InputTerminals[0].GetTrueVariable(),
                outputVariable = borrowTunnel.OutputTerminals[0].GetTrueVariable();
            GetVariableUsageForVariable(outputVariable).IsReferenceToVariable(inputVariable, GetVariableUsageForVariable(inputVariable));
            WillInitializeWithValue(borrowTunnel.OutputTerminals[0]);
            return true;
        }

        public bool VisitBuildTupleNode(BuildTupleNode buildTupleNode)
        {
            // TODO: for each input variable, note that it is moved into another data structure
            WillInitializeWithValue(buildTupleNode.OutputTerminals[0]);
            return true;
        }

        public bool VisitConstant(Constant constant)
        {
            VariableReference outputVariable = constant.OutputTerminal.GetTrueVariable();
            if (outputVariable.Type.IsInteger())
            {
                GetVariableUsageForVariable(outputVariable).WillInitializeWithCompileTimeConstant(
                    _currentGroup,
                    Context.GetIntegerValue(constant.Value, outputVariable.Type));
            }
            else if (outputVariable.Type.IsBoolean())
            {
                GetVariableUsageForVariable(outputVariable).WillInitializeWithCompileTimeConstant(
                    _currentGroup,
                    Context.AsLLVMValue((bool)constant.Value));
            }
            else
            {
                WillInitializeWithValue(constant.OutputTerminal);
            }
            return true;
        }

        public bool VisitDataAccessor(DataAccessor dataAccessor)
        {
            if (dataAccessor.Terminal.Direction == Direction.Output)
            {
                // TODO: distinguish inout from in parameters?
                WillInitializeWithValue(dataAccessor.Terminal);
            }
            else if (dataAccessor.Terminal.Direction == Direction.Input)
            {
                WillGetValue(dataAccessor.Terminal);
            }
            return true;
        }

        public bool VisitDecomposeTupleNode(DecomposeTupleNode decomposeTupleNode)
        {
            // TODO: for Borrow mode, mark each output reference as a struct offset of the input reference
            WillGetValue(decomposeTupleNode.InputTerminals[0]);
            decomposeTupleNode.OutputTerminals.ForEach(WillInitializeWithValue);
            return true;
        }

        public bool VisitDropNode(DropNode dropNode)
        {
            VariableReference droppedVariable = dropNode.InputTerminals[0].GetTrueVariable();
            if (TraitHelpers.TypeHasDropFunction(droppedVariable.Type))
            {
                WillTakeAddress(dropNode.InputTerminals[0]);
            }
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
                    WillInitializeWithValue(terminalPair.Value);
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
            WillGetValue(iterateTunnel.InputTerminals[0]);

            Terminal outputTerminal = iterateTunnel.OutputTerminals[0];
            _variableStorage.AddAdditionalValueSource(
                iterateTunnel.IntermediateValueName,
                AllocationSet.CreateStateField(iterateTunnel.IntermediateValueName, outputTerminal.GetTrueVariable().Type.CreateOption()));
            WillInitializeWithValue(outputTerminal);

            LoopConditionTunnel conditionTunnel = ((Compiler.Nodes.Loop)iterateTunnel.ParentStructure).BorderNodes.OfType<LoopConditionTunnel>().First();
            WillUpdateValue(conditionTunnel.InputTerminals[0]);
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
                var loop = (Compiler.Nodes.Loop)loopConditionTunnel.ParentStructure;
                AsyncStateGroup loopInitialGroup = _asyncStateGroups.First(g => g.GroupContainsStructureTraversalPoint(loop, loop.Diagram, StructureTraversalPoint.BeforeLeftBorderNodes));
                inputVariableUsage.WillInitializeWithCompileTimeConstant(loopInitialGroup, Context.AsLLVMValue(true));
            }
            WillGetValue(inputTerminal);

            GetVariableUsageForVariable(outputVariable).IsReferenceToVariable(inputVariable, inputVariableUsage);
            WillInitializeWithValue(loopConditionTunnel.OutputTerminals[0]);
            return true;
        }

        public bool VisitMethodCallNode(MethodCallNode methodCallNode)
        {
            VisitFunctionSignatureNode(methodCallNode, methodCallNode.Signature);
            return true;
        }

        public bool VisitOptionPatternStructureSelector(OptionPatternStructureSelector optionPatternStructureSelector)
        {
            WillGetValue(optionPatternStructureSelector.InputTerminals[0]);
            // The selector output terminals are initialized in VisitOptionPatternStructure, each in their own
            // diagram initial group.
            return true;
        }

        public bool VisitStructConstructorNode(StructConstructorNode structConstructorNode)
        {
            // TODO: the input variables can become references to fields of the output variable
            WillInitializeWithValue(structConstructorNode.OutputTerminals[0]);
            return true;
        }

        public bool VisitStructFieldAccessorNode(StructFieldAccessorNode structFieldAccessorNode)
        {
            // TODO: the output variables can become constant GEPs of the input pointer variable
            WillGetValue(structFieldAccessorNode.StructInputTerminal);
            structFieldAccessorNode.OutputTerminals.ForEach(WillInitializeWithValue);
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
            if (tunnel.Direction == Direction.Input)
            {
                VariableReference inputVariable = tunnel.InputTerminals[0].GetTrueVariable();
                foreach (Terminal outputTerminal in tunnel.OutputTerminals)
                {
                    _variableUsages[outputTerminal.GetTrueVariable()] = _variableUsages[inputVariable];
                }
            }
            else // tunnel.Direction == Direction.Output
            {
                tunnel.InputTerminals.ForEach(WillGetValue);
                Terminal inputTerminal;
                if (tunnel.InputTerminals.TryGetSingleElement(out inputTerminal))
                {
                    Terminal outputTerminal = tunnel.OutputTerminals[0];
                    VariableReference inputVariable = inputTerminal.GetTrueVariable(),
                        outputVariable = outputTerminal.GetTrueVariable();
                    if (outputVariable.Type == inputVariable.Type.CreateOption())
                    {
                        WillUpdateValue(outputTerminal);
                    }
                    else
                    {
                        // TODO: maybe it's better to compute variable usage for the input and output separately, and only reuse
                        // the ValueSource if they are close enough
                        _variableUsages[outputVariable] = _variableUsages[inputVariable];
                    }
                }
            }
            return true;
        }

        public bool VisitUnwrapOptionTunnel(UnwrapOptionTunnel unwrapOptionTunnel)
        {
            WillGetValue(unwrapOptionTunnel.InputTerminals[0]);
            WillInitializeWithValue(unwrapOptionTunnel.OutputTerminals[0]);
            return true;
        }

        public bool VisitVariantConstructorNode(VariantConstructorNode variantConstructorNode)
        {
            WillGetValue(variantConstructorNode.InputTerminals[0]);
            WillInitializeWithValue(variantConstructorNode.OutputTerminals[0]);
            return true;
        }

        public bool VisitVariantMatchStructureSelector(VariantMatchStructureSelector variantMatchStructureSelector)
        {
            WillGetValue(variantMatchStructureSelector.InputTerminals[0]);
            // The selector output terminals are initialized in VisitVariantMatchStructure, each in their own
            // diagram initial group.
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitWire(Wire wire)
        {
            if (!wire.SinkTerminals.HasMoreThan(1))
            {
                return true;
            }
            VariableReference sourceVariable = wire.SourceTerminal.GetTrueVariable();
            VariableUsage sourceVariableUsage = GetVariableUsageForVariable(sourceVariable);
            foreach (var sinkTerminal in wire.SinkTerminals.Skip(1))
            {
                VariableUsage sinkVariableUsage = GetVariableUsageForVariable(sinkTerminal.GetTrueVariable());
                sinkVariableUsage.IsReferenceToVariable(sourceVariableUsage.ReferencedVariable, sourceVariableUsage.ReferencedVariableUsage);
                WillInitializeWithValue(sinkTerminal);
            }
            return true;
        }

        private void VisitFunctionSignatureNode(Node node, NIType nodeFunctionSignature)
        {
            switch (nodeFunctionSignature.GetName())
            {
                case "Assign":
                    {
                        Terminal assignedInputTerminal = node.InputTerminals[0];
                        WillUpdateDereferencedValue(assignedInputTerminal);
                        if (TraitHelpers.TypeHasDropFunction(assignedInputTerminal.GetTrueVariable().Type.GetReferentType()))
                        {
                            WillGetValue(assignedInputTerminal);
                        }
                        WillGetValue(node.InputTerminals[1]);
                    }
                    return;
                case "Exchange":
                    WillGetDereferencedValue(node.InputTerminals[0]);
                    WillUpdateDereferencedValue(node.InputTerminals[0]);
                    WillGetDereferencedValue(node.InputTerminals[1]);
                    WillUpdateDereferencedValue(node.InputTerminals[1]);
                    return;
                case "CreateCopy":
                    VariableReference input0Variable = node.InputTerminals[0].GetTrueVariable();
                    if (input0Variable.Type.GetReferentType().WireTypeMayFork())
                    {
                        WillGetDereferencedValue(node.InputTerminals[0]);
                    }
                    else
                    {
                        WillGetValue(node.InputTerminals[0]);
                        WillTakeAddress(node.OutputTerminals[1]);
                    }
                    WillInitializeWithValue(node.OutputTerminals[1]);
                    return;
                case "SelectReference":
                    WillGetDereferencedValue(node.InputTerminals[0]);
                    WillGetValue(node.InputTerminals[1]);
                    WillGetValue(node.InputTerminals[2]);
                    WillInitializeWithValue(node.OutputTerminals[1]);
                    return;
                case "Increment":
                case "Not":
                    WillGetDereferencedValue(node.InputTerminals[0]);
                    WillInitializeWithValue(node.OutputTerminals[1]);
                    return;
                case "AccumulateIncrement":
                case "AccumulateNot":
                    WillGetDereferencedValue(node.InputTerminals[0]);
                    WillUpdateDereferencedValue(node.InputTerminals[0]);
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
                    WillGetDereferencedValue(node.InputTerminals[0]);
                    WillGetDereferencedValue(node.InputTerminals[1]);
                    WillInitializeWithValue(node.OutputTerminals[2]);
                    return;
                case "AccumulateAdd":
                case "AccumulateSubtract":
                case "AccumulateMultiply":
                case "AccumulateDivide":
                case "AccumulateAnd":
                case "AccumulateOr":
                case "AccumulateXor":
                    WillGetDereferencedValue(node.InputTerminals[0]);
                    WillUpdateDereferencedValue(node.InputTerminals[0]);
                    WillGetDereferencedValue(node.InputTerminals[1]);
                    return;
                case "NoneConstructor":
                    {
                        VariableReference outputVariable = node.OutputTerminals[0].GetTrueVariable();
                        LLVMTypeRef optionType = Context.AsLLVMType(outputVariable.Type);
                        GetVariableUsageForVariable(outputVariable).WillInitializeWithCompileTimeConstant(_currentGroup, LLVMSharp.LLVM.ConstNull(optionType));
                    }
                    return;
                case "Inspect":
                    WillGetDereferencedValue(node.InputTerminals[0]);
                    return;
                case "Output":
                    {
                        Terminal inputTerminal = node.InputTerminals[0];
                        NIType inputReferentType = inputTerminal.GetTrueVariable().Type.GetReferentType();
                        if (inputReferentType.IsInteger() || inputReferentType.IsBoolean())
                        {
                            WillGetDereferencedValue(inputTerminal);
                        }
                        else
                        {
                            WillGetValue(inputTerminal);
                        }
                    }
                    return;
                case "ImmutPass":
                case "MutPass":
                    return;
            }

            Signature signature = Signatures.GetSignatureForNIType(nodeFunctionSignature);
            foreach (var terminalPair in node.InputTerminals.Zip(signature.Inputs))
            {
                WillGetValue(terminalPair.Key);
            }
            foreach (var outputPair in node.OutputTerminals.Zip(signature.Outputs))
            {
                if (outputPair.Value.IsPassthrough)
                {
                    continue;
                }
                WillInitializeWithValue(outputPair.Key);
                WillTakeAddress(outputPair.Key);
            }
        }

        #endregion

        #region IInternalDfirNodeVisitor implementation

        bool IInternalDfirNodeVisitor<bool>.VisitAwaitNode(AwaitNode awaitNode)
        {
            WillTakeAddress(awaitNode.InputTerminal);
            // It may be the case that our output variable is the same as an upstream variable (e.g., because
            // it represents a passthrough of an async node). In that case we should reuse the value source
            // we already have for it.
            WillInitializeWithValue(awaitNode.OutputTerminal);
            return true;
        }

        bool IInternalDfirNodeVisitor<bool>.VisitCreateMethodCallPromise(CreateMethodCallPromise createMethodCallPromise)
        {
            foreach (Terminal inputTerminal in createMethodCallPromise.InputTerminals)
            {
                WillGetValue(inputTerminal);
            }
            WillInitializeWithValue(createMethodCallPromise.PromiseTerminal);
            return true;
        }

        bool IInternalDfirNodeVisitor<bool>.VisitDecomposeStructNode(DecomposeStructNode decomposeStructNode)
        {
            WillGetValue(decomposeStructNode.InputTerminals[0]);
            decomposeStructNode.OutputTerminals.ForEach(WillInitializeWithValue);
            return true;
        }

        bool IInternalDfirNodeVisitor<bool>.VisitPanicOrContinueNode(PanicOrContinueNode panicOrContinueNode)
        {
            WillGetValue(panicOrContinueNode.InputTerminal);
            WillInitializeWithValue(panicOrContinueNode.OutputTerminal);
            return true;
        }

        #endregion

        #region IDfirStructureVisitor implementation

        bool IDfirStructureVisitor<bool>.VisitLoop(Compiler.Nodes.Loop loop, StructureTraversalPoint traversalPoint)
        {
            switch (traversalPoint)
            {
                case StructureTraversalPoint.BeforeLeftBorderNodes:
                    foreach (Tunnel outputTunnel in loop.BorderNodes.OfType<Tunnel>().Where(tunnel => tunnel.Direction == Direction.Output))
                    {
                        // TODO: these tunnels are required to have local allocations for now.
                        // As with output tunnels of conditionally-executing Frames, it would be nice
                        // to treat these as Phi ValueSources.
                        WillUpdateValue(outputTunnel.OutputTerminals[0]);
                    }
                    break;
                case StructureTraversalPoint.AfterLeftBorderNodesAndBeforeDiagram:
                    LoopConditionTunnel loopCondition = loop.BorderNodes.OfType<LoopConditionTunnel>().First();
                    Terminal loopConditionInput = loopCondition.InputTerminals[0];
                    WillGetValue(loopConditionInput);
                    break;
            }
            return true;
        }

        bool IDfirStructureVisitor<bool>.VisitFrame(Frame frame, StructureTraversalPoint traversalPoint)
        {
            switch (traversalPoint)
            {
                case StructureTraversalPoint.AfterLeftBorderNodesAndBeforeDiagram:
                    foreach (Tunnel tunnel in frame.BorderNodes.OfType<Tunnel>().Where(t => t.Direction == Direction.Output))
                    {
                        // TODO: these tunnels require local allocations for now.
                        // It would be nicer to allow them to be Phi values--i.e., ValueSources that can be
                        // initialized by values from different predecessor blocks, but may not change
                        // after initialization.
                        WillUpdateValue(tunnel.OutputTerminals[0]);
                    }
                    break;
            }
            return true;
        }

        bool IDfirStructureVisitor<bool>.VisitOptionPatternStructure(OptionPatternStructure optionPatternStructure, StructureTraversalPoint traversalPoint, Diagram nestedDiagram)
        {
            switch (traversalPoint)
            {
                case StructureTraversalPoint.BeforeLeftBorderNodes:
                    WillGetValue(optionPatternStructure.Selector.InputTerminals[0]);
                    break;
                case StructureTraversalPoint.AfterLeftBorderNodesAndBeforeDiagram:
                    if (nestedDiagram == optionPatternStructure.Diagrams[0])
                    {
                        WillGetValue(optionPatternStructure.Selector.InputTerminals[0]);
                        WillInitializeWithValue(optionPatternStructure.Selector.OutputTerminals[0]);
                    }
                    break;
                case StructureTraversalPoint.AfterDiagram:
                    foreach (Tunnel outputTunnel in optionPatternStructure.Tunnels.Where(tunnel => tunnel.Direction == Direction.Output))
                    {
                        Terminal inputTerminal = outputTunnel.InputTerminals.First(t => t.ParentDiagram == nestedDiagram);
                        WillGetValue(inputTerminal);
                        WillUpdateValue(outputTunnel.OutputTerminals[0]);
                    }
                    break;
            }
            return true;
        }

        bool IDfirStructureVisitor<bool>.VisitVariantMatchStructure(VariantMatchStructure variantMatchStructure, StructureTraversalPoint traversalPoint, Diagram nestedDiagram)
        {
            switch (traversalPoint)
            {
                case StructureTraversalPoint.AfterLeftBorderNodesAndBeforeDiagram:
                    WillGetValue(variantMatchStructure.Selector.InputTerminals[0]);
                    foreach (NationalInstruments.Dfir.BorderNode borderNode in variantMatchStructure.BorderNodes.Where(bn => bn.Direction == Direction.Input))
                    {
                        WillInitializeWithValue(borderNode.OutputTerminals[nestedDiagram.Index]);
                    }
                    break;
            }
            return true;
        }

        #endregion

        private void WillInitializeWithValue(Terminal terminal)
        {
            GetVariableUsageForVariable(terminal.GetTrueVariable()).WillInitializeWithValue(_currentGroup);
        }

        private void WillGetValue(Terminal terminal)
        {
            GetVariableUsageForVariable(terminal.GetTrueVariable()).WillGetValue(_currentGroup);
        }

        private void WillGetDereferencedValue(Terminal terminal)
        {
            GetVariableUsageForVariable(terminal.GetTrueVariable()).WillGetDereferencedValue(_currentGroup);
        }

        private void WillUpdateValue(Terminal terminal)
        {
            GetVariableUsageForVariable(terminal.GetTrueVariable()).WillUpdateValue(_currentGroup);
        }

        private void WillUpdateDereferencedValue(Terminal terminal)
        {
            GetVariableUsageForVariable(terminal.GetTrueVariable()).WillUpdateDereferencedValue(_currentGroup);
        }

        private void WillTakeAddress(Terminal terminal)
        {
            GetVariableUsageForVariable(terminal.GetTrueVariable()).WillTakeAddress(_currentGroup);
        }

        private class VariableUsage
        {
            private LLVMValueRef? _constantValue;
            private HashSet<string> _liveFunctionNames = new HashSet<string>();

            public VariableUsage ReferencedVariableUsage { get; private set; }

            public VariableReference ReferencedVariable { get; private set; }

            public AsyncStateGroup InitializingGroup { get; private set; }

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

            public bool LiveInMultipleFunctions => _liveFunctionNames.Count > 1;

            public string ContainingFunctionName => _liveFunctionNames.Count > 1 ? null : _liveFunctionNames.First();

            public void IsReferenceToVariable(VariableReference variable, VariableUsage usage)
            {
                ReferencedVariable = variable;
                ReferencedVariableUsage = usage;
            }

            public void WillInitializeWithValue(AsyncStateGroup inGroup)
            {
                _liveFunctionNames.Add(inGroup.FunctionId);
                InitializingGroup = inGroup;
            }

            public void WillGetValue(AsyncStateGroup inGroup)
            {
                GetsValue = true;
                _liveFunctionNames.Add(inGroup.FunctionId);
                ReferencedVariableUsage?.WillTakeAddress(inGroup);
            }

            public void WillGetDereferencedValue(AsyncStateGroup inGroup)
            {
                _liveFunctionNames.Add(inGroup.FunctionId);
                ReferencedVariableUsage?.WillGetValue(inGroup);
            }

            public void WillTakeAddress(AsyncStateGroup inGroup)
            {
                _liveFunctionNames.Add(inGroup.FunctionId);
                TakesAddress = true;
            }

            public void WillInitializeWithCompileTimeConstant(AsyncStateGroup inGroup, LLVMValueRef constantValue)
            {
                _liveFunctionNames.Add(inGroup.FunctionId);
                _constantValue = constantValue;
            }

            public void WillUpdateValue(AsyncStateGroup inGroup)
            {
                _liveFunctionNames.Add(inGroup.FunctionId);
                UpdatesValue = true;
            }

            public void WillUpdateDereferencedValue(AsyncStateGroup inGroup)
            {
                _liveFunctionNames.Add(inGroup.FunctionId);
                ReferencedVariableUsage?.WillUpdateValue(inGroup);
            }
        }
    }
}
