using System.Collections.Generic;
using System.Linq;
using NationalInstruments;
using NationalInstruments.CommonModel;
using NationalInstruments.Compiler;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler;
using Rebar.RebarTarget.LLVM;
using Rebar.RebarTarget.LLVM.CodeGen;

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
    internal class Allocator : VisitorTransformBase, ICodeGenElementVisitor<bool>
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
            return variable.Name ?? $"v{variable.Id}";
        }

        public override void Execute(DfirRoot dfirRoot, CompileCancellationToken cancellationToken)
        {
            // Execute as a VisitorTransform base to initialize VariableUsages for all variables
            base.Execute(dfirRoot, cancellationToken);

            // Handle each Visitation in order to set particular node/wire/structure VariableUsage characteristics
            foreach (AsyncStateGroup group in _asyncStateGroups)
            {
                _currentGroup = group;
                VariableUsage continuationConditionUsage = group.Continuation is ConditionallyScheduleGroupsContinuation
                    ? GetVariableUsageForVariable(group.ContinuationCondition)
                    : null;
                continuationConditionUsage?.WillInitializeWithValue(_currentGroup);

                foreach (Visitation visitation in group.Visitations)
                {
                    visitation.Visit<bool>(this);
                }

                continuationConditionUsage?.WillGetValue(_currentGroup);
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
            if (usage.ParameterDirection == Direction.Input)
            {
                return _singleFunctionName != null
                    ? (ValueSource)AllocationSet.CreateLocalAllocation(_singleFunctionName, VariableAllocationName(variable), variable.Type)
                    : AllocationSet.CreateStateField(VariableAllocationName(variable), variable.Type);
            }
            else if (usage.ParameterDirection == Direction.Output)
            {
                return _singleFunctionName != null
                    ? (ValueSource)AllocationSet.CreateOutputParameterLocalAllocation(_singleFunctionName, VariableAllocationName(variable), variable.Type)
                    : AllocationSet.CreateOutputParameterStateField(VariableAllocationName(variable), variable.Type);
            }

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

        #region VisitorTransformBase overrides

        protected override void VisitBorderNode(NationalInstruments.Dfir.BorderNode borderNode)
        {
        }

        protected override void VisitNode(Node node)
        {
        }

        protected override void VisitWire(Wire wire)
        {
        }

        protected override void VisitDfirRoot(DfirRoot dfirRoot)
        {
            dfirRoot.DataItems.OrderBy(d => d.ConnectorPaneIndex).ForEach(VisitDataItem);
            base.VisitDfirRoot(dfirRoot);
        }

        private void VisitDataItem(DataItem dataItem)
        {
            GetVariableUsageForVariable(dataItem.GetVariable()).IsParameter(
                dataItem.IsInput ? Direction.Input : Direction.Output);
        }

        #endregion

        #region ICodeGenElementVisitor

        bool ICodeGenElementVisitor<bool>.VisitGetValue(GetValue getValue)
        {
            GetVariableUsageForVariable(getValue.Variable).WillGetValue(_currentGroup);
            return true;
        }

        bool ICodeGenElementVisitor<bool>.VisitGetDereferencedValue(GetDereferencedValue getDereferencedValue)
        {
            GetVariableUsageForVariable(getDereferencedValue.Variable).WillGetDereferencedValue(_currentGroup);
            return true;
        }

        bool ICodeGenElementVisitor<bool>.VisitUpdateValue(UpdateValue updateValue)
        {
            GetVariableUsageForVariable(updateValue.Variable).WillUpdateValue(_currentGroup);
            return true;
        }

        bool ICodeGenElementVisitor<bool>.VisitUpdateDereferencedValue(UpdateDereferencedValue updateDereferencedValue)
        {
            GetVariableUsageForVariable(updateDereferencedValue.Variable).WillUpdateDereferencedValue(_currentGroup);
            return true;
        }

        bool ICodeGenElementVisitor<bool>.VisitGetAddress(GetAddress getAddress)
        {
            VariableUsage usage = GetVariableUsageForVariable(getAddress.Variable);
            if (getAddress.ForInitialize)
            {
                usage.WillInitializeWithValue(_currentGroup);
            }
            usage.WillTakeAddress(_currentGroup);
            return true;
        }

        bool ICodeGenElementVisitor<bool>.VisitInitializeValue(InitializeValue initializeValue)
        {
            GetVariableUsageForVariable(initializeValue.Variable).WillInitializeWithValue(_currentGroup);
            return true;
        }

        bool ICodeGenElementVisitor<bool>.VisitInitializeAsReference(InitializeAsReference initializeAsReference)
        {
            VariableUsage toInitializeUsage = GetVariableUsageForVariable(initializeAsReference.InitializedVariable);
            toInitializeUsage.IsReferenceToVariable(
                initializeAsReference.ReferencedVariable,
                GetVariableUsageForVariable(initializeAsReference.ReferencedVariable));
            toInitializeUsage.WillInitializeWithValue(_currentGroup);
            return true;
        }

        bool ICodeGenElementVisitor<bool>.VisitInitializeWithCopy(InitializeWithCopy initializeWithCopy)
        {
            VariableUsage copiedVariableUsage = GetVariableUsageForVariable(initializeWithCopy.CopiedVariable);
            VariableUsage initializedVariableUsage = GetVariableUsageForVariable(initializeWithCopy.InitializedVariable);
            initializedVariableUsage.IsReferenceToVariable(copiedVariableUsage.ReferencedVariable, copiedVariableUsage.ReferencedVariableUsage);
            initializedVariableUsage.WillInitializeWithValue(_currentGroup);
            return true;
        }

        bool ICodeGenElementVisitor<bool>.VisitBuildStruct(BuildStruct buildStruct)
        {
            return true;
        }

        bool ICodeGenElementVisitor<bool>.VisitGetStructFieldValue(GetStructFieldValue getStructFieldValue)
        {
            return true;
        }

        bool ICodeGenElementVisitor<bool>.VisitGetStructFieldPointer(GetStructFieldPointer getStructFieldPointer)
        {
            return true;
        }

        bool ICodeGenElementVisitor<bool>.VisitGetConstant(GetConstant getConstant)
        {
            return true;
        }

        bool ICodeGenElementVisitor<bool>.VisitCall(Call call)
        {
            return true;
        }

        bool ICodeGenElementVisitor<bool>.VisitCallWithReturn(CallWithReturn callWithReturn)
        {
            return true;
        }

        bool ICodeGenElementVisitor<bool>.VisitOp(Op op)
        {
            return true;
        }

        bool ICodeGenElementVisitor<bool>.VisitShareValue(ShareValue shareValue)
        {
            _variableUsages[shareValue.User] = _variableUsages[shareValue.Provider];
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
            private HashSet<string> _liveFunctionNames = new HashSet<string>();

            public VariableUsage ReferencedVariableUsage { get; private set; }

            public VariableReference ReferencedVariable { get; private set; }

            public AsyncStateGroup InitializingGroup { get; private set; }

            public bool GetsValue { get; private set; }

            public bool TakesAddress { get; private set; }

            public Direction ParameterDirection { get; private set; } = Direction.Unknown;

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

            public void IsParameter(Direction direction)
            {
                ParameterDirection = direction;
            }
        }
    }
}
