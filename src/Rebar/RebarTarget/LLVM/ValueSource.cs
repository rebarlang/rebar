using System;
using System.Collections.Generic;
using LLVMSharp;

namespace Rebar.RebarTarget.LLVM
{
    internal abstract class ValueSource
    {
        public abstract LLVMValueRef GetValue(IRBuilder builder);
    }

    internal interface IAddressableValueSource
    {
        LLVMValueRef GetAddress(IRBuilder builder);
    }

    internal interface IInitializableValueSource
    {
        void InitializeValue(IRBuilder builder, LLVMValueRef value);
    }

    internal interface IUpdateableValueSource
    {
        void UpdateValue(IRBuilder builder, LLVMValueRef value);
    }

    internal interface IGetDereferencedValueSource
    {
        LLVMValueRef GetDereferencedValue(IRBuilder builder);
    }

    internal abstract class SingleValueSource : ValueSource
    {
        protected LLVMValueRef? Value { get; set; }

        public override LLVMValueRef GetValue(IRBuilder builder)
        {
            if (Value == null)
            {
                throw new InvalidOperationException("Trying to get value of uninitialized variable");
            }
            return Value.Value;
        }
    }

    internal class ConstantValueSource : SingleValueSource
    {
        public ConstantValueSource(LLVMValueRef value)
        {
            Value = value;
        }
    }

    internal class ImmutableValueSource : SingleValueSource, IInitializableValueSource
    {
        public void InitializeValue(IRBuilder builder, LLVMValueRef value)
        {
            if (Value != null)
            {
                throw new InvalidOperationException("Trying to re-initialize variable");
            }
            Value = value;
        }
    }

    internal class ReferenceToSingleValueSource : ValueSource, IGetDereferencedValueSource
    {
        private readonly SingleValueSource _singleValueSource;

        public ReferenceToSingleValueSource(SingleValueSource constantValueSource)
        {
            _singleValueSource = constantValueSource;
        }

        public LLVMValueRef GetDereferencedValue(IRBuilder builder)
        {
            return _singleValueSource.GetValue(builder);
        }

        public override LLVMValueRef GetValue(IRBuilder builder)
        {
            throw new InvalidOperationException("Cannot get address value from ReferenceToConstantValueSource");
        }
    }

    internal abstract class AllocationValueSource : ValueSource, IAddressableValueSource, IInitializableValueSource, IUpdateableValueSource
    {
        private int _loadCount;
        private readonly Dictionary<LLVMBasicBlockRef, LLVMValueRef> _basicBlockAddresses = new Dictionary<LLVMBasicBlockRef, LLVMValueRef>();

        protected AllocationValueSource(string allocationName)
        {
            AllocationName = allocationName;
        }

        protected string AllocationName { get; }

        protected abstract LLVMValueRef GetAllocationPointer(IRBuilder builder);

        public void InitializeValue(IRBuilder builder, LLVMValueRef value)
        {
            UpdateValue(builder, value);
        }

        LLVMValueRef IAddressableValueSource.GetAddress(IRBuilder builder)
        {
            return GetValidPointer(builder);
        }

        private LLVMValueRef GetValidPointer(IRBuilder builder)
        {
            LLVMBasicBlockRef currentBlock = builder.GetInsertBlock();
            LLVMValueRef allocationPointer;
            if (!_basicBlockAddresses.TryGetValue(currentBlock, out allocationPointer))
            {
                allocationPointer = GetAllocationPointer(builder);
                allocationPointer.ThrowIfNull();
                _basicBlockAddresses[currentBlock] = allocationPointer;
            }
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
            UpdateValue(builder, value);
        }

        private void UpdateValue(IRBuilder builder, LLVMValueRef value)
        {
            builder.CreateStore(value, GetValidPointer(builder));
        }
    }

    internal class LocalAllocationValueSource : AllocationValueSource
    {
        private readonly FunctionAllocationSet _allocationSet;
        private readonly string _functionName;
        private readonly int _allocationIndex;

        public LocalAllocationValueSource(string allocationName, FunctionAllocationSet allocationSet, string functionName, int allocationIndex)
            : base(allocationName)
        {
            _allocationSet = allocationSet;
            _functionName = functionName;
            _allocationIndex = allocationIndex;
        }

        protected override LLVMValueRef GetAllocationPointer(IRBuilder builder) => _allocationSet.GetLocalAllocationPointer(_functionName, _allocationIndex);
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

        protected override LLVMValueRef GetAllocationPointer(IRBuilder builder) => _allocationSet.GetStateFieldPointer(builder, _fieldIndex);
    }

    internal class OutputParameterValueSource : StateFieldValueSource
    {
        public OutputParameterValueSource(string allocationName, FunctionAllocationSet allocationSet, int fieldIndex)
            : base(allocationName, allocationSet, fieldIndex)
        {
        }

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

    internal class CalleeStateValueSource : AllocationValueSource
    {
        private readonly FunctionAllocationSet _functionAllocationSet;
        private readonly int _index;

        public CalleeStateValueSource(string allocationName, FunctionAllocationSet functionAllocationSet, int index) : base(allocationName)
        {
            _functionAllocationSet = functionAllocationSet;
            _index = index;
        }

        protected override LLVMValueRef GetAllocationPointer(IRBuilder builder)
        {
            return _functionAllocationSet.GetCalleeStatePointer(builder, _index);
        }
    }

    internal static class ValueSourceExtensions
    {
        public static LLVMValueRef GetDereferencedValue(this ValueSource valueSource, IRBuilder builder)
        {
            var getDereferenceValueSource = valueSource as IGetDereferencedValueSource;
            if (getDereferenceValueSource != null)
            {
                return getDereferenceValueSource.GetDereferencedValue(builder);
            }

            LLVMValueRef address = valueSource.GetValue(builder);
            if (address.TypeOf().TypeKind != LLVMTypeKind.LLVMPointerTypeKind)
            {
                throw new InvalidOperationException("Trying to dereference non-pointer value");
            }
            return builder.CreateLoad(address, $"deref");
        }

        public static void UpdateDereferencedValue(this ValueSource valueSource, IRBuilder builder, LLVMValueRef value)
        {
            builder.CreateStore(value, valueSource.GetValue(builder));
        }
    }
}
