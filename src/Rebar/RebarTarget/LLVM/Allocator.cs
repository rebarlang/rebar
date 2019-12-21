using System;
using System.Collections.Generic;
using System.Linq;
using LLVMSharp;
using NationalInstruments.DataTypes;
using Rebar.Common;

namespace Rebar.RebarTarget.LLVM
{
    internal class Allocator : Allocator<ValueSource, AllocationValueSource, ConstantLocalReferenceValueSource>
    {
        public Allocator(
            Dictionary<VariableReference, ValueSource> valueSources,
            Dictionary<object, ValueSource> additionalSources)
            : base(valueSources, additionalSources)
        {
        }

        public FunctionAllocationSet AllocationSet { get; } = new FunctionAllocationSet();

        protected override ConstantLocalReferenceValueSource CreateConstantLocalReference(VariableReference referencedVariable)
        {
            return new ConstantLocalReferenceValueSource((IAddressableValueSource)GetValueSourceForVariable(referencedVariable));
        }

        protected override AllocationValueSource CreateLocalAllocation(VariableReference variable)
        {
            string name = $"v{variable.Id}";
            return AllocationSet.CreateStateField(name, variable.Type);
        }

        protected override AllocationValueSource CreateLocalAllocation(string name, NIType type)
        {
            return AllocationSet.CreateStateField(name, type);
        }

        protected override AllocationValueSource CreateOutputParameterAllocation(VariableReference outputParameterVariable)
        {
            string name = $"v{outputParameterVariable.Id}";
            return AllocationSet.CreateOutputParameter(name, outputParameterVariable.Type);
        }
    }

    internal abstract class ValueSource
    {
        public abstract LLVMValueRef GetValue(IRBuilder builder);
    }

    internal interface IAddressableValueSource
    {
        LLVMValueRef GetAddress(IRBuilder builder);

        LLVMTypeRef AddressType { get; }
    }

    internal interface IUpdateableValueSource
    {
        void UpdateValue(IRBuilder builder, LLVMValueRef value);
    }

    internal abstract class AllocationValueSource : ValueSource, IAddressableValueSource, IUpdateableValueSource
    {
        private int _loadCount;

        protected AllocationValueSource(string allocationName)
        {
            AllocationName = allocationName;
        }

        protected string AllocationName { get; }

        public abstract LLVMTypeRef AddressType { get; }

        protected abstract LLVMValueRef GetAllocationPointer(IRBuilder builder);

        LLVMValueRef IAddressableValueSource.GetAddress(IRBuilder builder)
        {
            return GetValidPointer(builder);
        }

        private LLVMValueRef GetValidPointer(IRBuilder builder)
        {
            LLVMValueRef allocationPointer = GetAllocationPointer(builder);
            allocationPointer.ThrowIfNull();
            return allocationPointer;
        }

        public override LLVMValueRef GetValue(IRBuilder builder)
        {
            string name = $"{AllocationName}_load_{_loadCount}";
            ++_loadCount;
            return builder.CreateLoad(GetValidPointer(builder), name);
        }

        void IUpdateableValueSource.UpdateValue(IRBuilder builder, LLVMValueRef value)
        {
            builder.CreateStore(value, GetValidPointer(builder));
        }
    }

    internal class LocalAllocationValueSource : AllocationValueSource
    {
        private readonly FunctionAllocationSet _allocationSet;
        private readonly int _allocationIndex;

        public LocalAllocationValueSource(string allocationName, FunctionAllocationSet allocationSet, int allocationIndex)
            : base(allocationName)
        {
            _allocationSet = allocationSet;
            _allocationIndex = allocationIndex;
        }

        public override LLVMTypeRef AddressType => _allocationSet.GetLocalAllocationPointer(_allocationIndex).TypeOf();

        protected override LLVMValueRef GetAllocationPointer(IRBuilder builder) => _allocationSet.GetLocalAllocationPointer(_allocationIndex);
    }

    internal class StateFieldValueSource : AllocationValueSource
    {
        private readonly FunctionAllocationSet _allocationSet;
        private readonly int _fieldIndex;

        public StateFieldValueSource(string allocationName, FunctionAllocationSet allocationSet, int fieldIndex)
            : base(allocationName)
        {
            _allocationSet = allocationSet;
            _fieldIndex = fieldIndex;
        }

        public override LLVMTypeRef AddressType => _allocationSet.GetStateFieldPointerType(_fieldIndex);

        protected override LLVMValueRef GetAllocationPointer(IRBuilder builder) => _allocationSet.GetStateFieldPointer(builder, _fieldIndex);
    }

    internal class OutputParameterValueSource : StateFieldValueSource
    {
        public OutputParameterValueSource(string allocationName, FunctionAllocationSet allocationSet, int fieldIndex)
            : base(allocationName, allocationSet, fieldIndex)
        {
        }

        public override LLVMTypeRef AddressType => base.AddressType.GetElementType();

        protected override LLVMValueRef GetAllocationPointer(IRBuilder builder)
        {
            LLVMValueRef stateFieldAllocationPtr = base.GetAllocationPointer(builder),
                outputParameterAllocationPtr = builder.CreateLoad(stateFieldAllocationPtr, AllocationName + "Load");
            return outputParameterAllocationPtr;
        }
    }

    internal class ConstantLocalReferenceValueSource : ValueSource
    {
        private readonly IAddressableValueSource _referencedAddressableValueSource;

        public ConstantLocalReferenceValueSource(IAddressableValueSource referencedAddressableValueSource)
        {
            _referencedAddressableValueSource = referencedAddressableValueSource;
        }

        public override LLVMValueRef GetValue(IRBuilder builder)
        {
            return _referencedAddressableValueSource.GetAddress(builder);
        }
    }

    internal static class ValueSourceExtensions
    {
        public static LLVMValueRef GetDereferencedValue(this ValueSource valueSource, IRBuilder builder)
        {
            return builder.CreateLoad(valueSource.GetValue(builder), $"deref");
        }

        public static void UpdateDereferencedValue(this ValueSource valueSource, IRBuilder builder, LLVMValueRef value)
        {
            builder.CreateStore(value, valueSource.GetValue(builder));
        }
    }

    internal class FunctionAllocationSet
    {
        private readonly List<Tuple<string, NIType>> _localAllocationTypes = new List<Tuple<string, NIType>>();
        private readonly List<Tuple<string, NIType>> _stateFieldTypes = new List<Tuple<string, NIType>>();
        private LLVMValueRef[] _localAllocationPointers;

        private const int FixedFieldCount = 3;

        public LocalAllocationValueSource CreateLocalAllocation(string allocationName, NIType allocationType)
        {
            int allocationIndex = _localAllocationTypes.Count;
            _localAllocationTypes.Add(new Tuple<string, NIType>(allocationName, allocationType));
            return new LocalAllocationValueSource(allocationName, this, allocationIndex);
        }

        public StateFieldValueSource CreateStateField(string allocationName, NIType allocationType)
        {
            int fieldIndex = _stateFieldTypes.Count;
            _stateFieldTypes.Add(new Tuple<string, NIType>(allocationName, allocationType));
            return new StateFieldValueSource(allocationName, this, fieldIndex);
        }

        public OutputParameterValueSource CreateOutputParameter(string allocationName, NIType allocationType)
        {
            int fieldIndex = _stateFieldTypes.Count;
            _stateFieldTypes.Add(new Tuple<string, NIType>(allocationName, allocationType.CreateMutableReference()));
            return new OutputParameterValueSource(allocationName, this, fieldIndex);
        }

        public void InitializeStateType(Module module, string functionName)
        {
            StateType = LLVMTypeRef.StructCreateNamed(module.GetModuleContext(), functionName + "_state_t");

            var stateFieldTypes = new List<LLVMTypeRef>();
            // fixed fields
            stateFieldTypes.Add(LLVMTypeRef.Int1Type());    // function done?
            stateFieldTypes.Add(LLVMTypeRef.PointerType(LLVMExtensions.ScheduledTaskFunctionType, 0u)); // caller waker function
            stateFieldTypes.Add(LLVMExtensions.VoidPointerType);    // caller waker state
            // end fixed fields
            stateFieldTypes.AddRange(_stateFieldTypes.Select(a => a.Item2.AsLLVMType()));
            StateType.StructSetBody(stateFieldTypes.ToArray(), false);
        }

        public void InitializeAllocations(IRBuilder builder)
        {
            if (_localAllocationPointers != null)
            {
                throw new InvalidOperationException("Already initialized allocations");
            }
            _localAllocationPointers = _localAllocationTypes.Select(a => builder.CreateAlloca(a.Item2.AsLLVMType(), a.Item1)).ToArray();
        }

        public LLVMTypeRef StateType { get; private set; }

        public FunctionCompilerState CompilerState { get; set; }

        public LLVMValueRef StatePointer => CompilerState.StatePointer;

        public LLVMValueRef GetLocalAllocationPointer(int index)
        {
            return _localAllocationPointers[index];
        }

        internal LLVMValueRef GetStateDonePointer(IRBuilder builder)
        {
            StatePointer.ThrowIfNull();
            return builder.CreateStructGEP(StatePointer, 0u, "donePtr");
        }

        internal LLVMValueRef GetStateCallerWakerFunctionPointer(IRBuilder builder)
        {
            StatePointer.ThrowIfNull();
            return builder.CreateStructGEP(StatePointer, 1u, "callerWakerFunctionPtr");
        }

        internal LLVMValueRef GetStateCallerWakerStatePointer(IRBuilder builder)
        {
            StatePointer.ThrowIfNull();
            return builder.CreateStructGEP(StatePointer, 2u, "callerWakerStatePtr");
        }

        internal LLVMValueRef GetStateFieldPointer(IRBuilder builder, int fieldIndex)
        {
            StatePointer.ThrowIfNull();
            return builder.CreateStructGEP(StatePointer, (uint)(fieldIndex + FixedFieldCount), _stateFieldTypes[fieldIndex].Item1 + "_fieldptr");
        }

        internal LLVMTypeRef GetStateFieldPointerType(int fieldIndex)
        {
            return LLVMTypeRef.PointerType(_stateFieldTypes[fieldIndex].Item2.AsLLVMType(), 0u);
        }
    }
}
