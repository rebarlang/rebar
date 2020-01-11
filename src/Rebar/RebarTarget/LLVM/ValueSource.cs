using System;
using System.Collections.Generic;
using LLVMSharp;

namespace Rebar.RebarTarget.LLVM
{
    internal abstract class ValueSource
    {
        private bool _initialized;

        public abstract LLVMValueRef GetValue(IRBuilder builder);
    }

    internal interface IAddressableValueSource
    {
        LLVMValueRef GetAddress(IRBuilder builder);

        LLVMTypeRef AddressType { get; }
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

    internal class ConstantValueSource : ValueSource
    {
        private readonly LLVMValueRef _value;

        public ConstantValueSource(LLVMValueRef value)
        {
            _value = value;
        }

        public override LLVMValueRef GetValue(IRBuilder builder) => _value;
    }

    internal class ReferenceToConstantValueSource : ValueSource, IGetDereferencedValueSource
    {
        private readonly ConstantValueSource _constantValueSource;

        public ReferenceToConstantValueSource(ConstantValueSource constantValueSource)
        {
            _constantValueSource = constantValueSource;
        }

        public LLVMValueRef GetDereferencedValue(IRBuilder builder)
        {
            return _constantValueSource.GetValue(builder);
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

        public abstract LLVMTypeRef AddressType { get; }

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
