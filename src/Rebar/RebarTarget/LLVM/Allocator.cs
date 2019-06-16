using System;
using System.Collections.Generic;
using LLVMSharp;
using NationalInstruments.DataTypes;
using Rebar.Common;

namespace Rebar.RebarTarget.LLVM
{
    internal class Allocator : Allocator<ValueSource, LocalAllocationValueSource, ConstantLocalReferenceValueSource>
    {
        public Allocator(Dictionary<VariableReference, ValueSource> valueSources)
            : base(valueSources)
        {
        }

        protected override ConstantLocalReferenceValueSource CreateConstantLocalReference(VariableReference referencedVariable)
        {
            var localAllocation = (LocalAllocationValueSource)GetValueSourceForVariable(referencedVariable);
            return new ConstantLocalReferenceValueSource(localAllocation);
        }

        protected override LocalAllocationValueSource CreateLocalAllocation(VariableReference variable)
        {
            string name = $"v{variable.Id}";
            return new LocalAllocationValueSource(name, variable.Type);
        }
    }

    internal abstract class ValueSource
    {
        public abstract LLVMValueRef GetValue(IRBuilder builder);

        public abstract void UpdateValue(IRBuilder builder, LLVMValueRef value);

        public abstract LLVMValueRef GetDeferencedValue(IRBuilder builder);

        public abstract void UpdateDereferencedValue(IRBuilder builder, LLVMValueRef value);
    }

    internal class LocalAllocationValueSource : ValueSource
    {
        private int _loadCount;

        public LocalAllocationValueSource(string allocationName, NIType allocationNIType)
        {
            AllocationName = allocationName;
            AllocationNIType = allocationNIType;
        }

        public string AllocationName { get; }

        public NIType AllocationNIType { get; }

        public LLVMValueRef AllocationPointer { get; set; }

        public override LLVMValueRef GetDeferencedValue(IRBuilder builder)
        {
            return builder.CreateLoad(GetValue(builder), $"{AllocationName}_deref");
        }

        public override LLVMValueRef GetValue(IRBuilder builder)
        {
            string name = $"{AllocationName}_load_{_loadCount}";
            ++_loadCount;
            AllocationPointer.ThrowIfNull();
            return builder.CreateLoad(AllocationPointer, name);
        }

        public override void UpdateDereferencedValue(IRBuilder builder, LLVMValueRef value)
        {
            LLVMValueRef ptr = GetValue(builder);
            builder.CreateStore(value, ptr);
        }

        public override void UpdateValue(IRBuilder builder, LLVMValueRef value)
        {
            AllocationPointer.ThrowIfNull();
            builder.CreateStore(value, AllocationPointer);
        }

        public void UpdateStructValue(IRBuilder builder, LLVMValueRef[] fieldValues)
        {
            LLVMTypeRef structType = AllocationPointer.TypeOf().GetElementType();
            if (structType.TypeKind != LLVMTypeKind.LLVMStructTypeKind)
            {
                throw new InvalidOperationException("Cannot UpdateStructValue on a non-struct");
            }
            for (int i = 0; i < fieldValues.Length; ++i)
            {
                LLVMValueRef fieldPtr = builder.CreateStructGEP(AllocationPointer, (uint)i, "field");
                builder.CreateStore(fieldValues[i], fieldPtr);
            }
        }
    }

    internal class ConstantLocalReferenceValueSource : ValueSource
    {
        public ConstantLocalReferenceValueSource(LocalAllocationValueSource referencedAllocation)
        {
            ReferencedAllocation = referencedAllocation;
        }

        public LocalAllocationValueSource ReferencedAllocation { get; }

        public override LLVMValueRef GetDeferencedValue(IRBuilder builder)
        {
            return ReferencedAllocation.GetValue(builder);
        }

        public override LLVMValueRef GetValue(IRBuilder builder)
        {
            return ReferencedAllocation.AllocationPointer;
        }

        public override void UpdateDereferencedValue(IRBuilder builder, LLVMValueRef value)
        {
            ReferencedAllocation.UpdateValue(builder, value);
        }

        public override void UpdateValue(IRBuilder builder, LLVMValueRef value)
        {
            throw new InvalidOperationException("Cannot update a constant reference.");
        }
    }
}
