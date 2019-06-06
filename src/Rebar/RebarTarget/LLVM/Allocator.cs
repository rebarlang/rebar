using System;
using System.Collections.Generic;
using LLVMSharp;
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
            return new LocalAllocationValueSource(name);
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
        private readonly string _allocationName;
        private int _loadCount;

        public LocalAllocationValueSource(string allocationName)
        {
            _allocationName = allocationName;
        }

        public LLVMValueRef AllocationPointer { get; set; }

        public override LLVMValueRef GetDeferencedValue(IRBuilder builder)
        {
            throw new NotImplementedException();
        }

        public override LLVMValueRef GetValue(IRBuilder builder)
        {
            string name = $"{_allocationName}_load_{_loadCount}";
            ++_loadCount;
            return builder.CreateLoad(AllocationPointer, name);
        }

        public override void UpdateDereferencedValue(IRBuilder builder, LLVMValueRef value)
        {
            throw new NotImplementedException();
        }

        public override void UpdateValue(IRBuilder builder, LLVMValueRef value)
        {
            builder.CreateStore(value, AllocationPointer);
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
