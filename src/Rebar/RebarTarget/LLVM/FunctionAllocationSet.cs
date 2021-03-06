﻿using System;
using System.Collections.Generic;
using System.Linq;
using LLVMSharp;
using NationalInstruments.DataTypes;
using Rebar.Common;

namespace Rebar.RebarTarget.LLVM
{
    internal class FunctionAllocationSet
    {
        private class LocalAllocation
        {
            public LocalAllocation(string name, NIType type)
            {
                Name = name;
                Type = type;
            }

            public string Name { get; }

            public NIType Type { get; }

            public LLVMValueRef Pointer { get; set; }
        }

        private class StateFieldAllocation
        {
            public StateFieldAllocation(string name, NIType type)
            {
                Name = name;
                Type = type;
            }

            public string Name { get; }

            public NIType Type { get; }
        }

        public static LLVMTypeRef FunctionCompletionStatusType(ContextWrapper context) => context.Int8Type;

        private readonly Dictionary<string, List<LocalAllocation>> _functionLocalAllocations = new Dictionary<string, List<LocalAllocation>>();
        private readonly List<StateFieldAllocation> _stateFields = new List<StateFieldAllocation>();

        private const int FixedFieldCount = 2;
        public const int FirstParameterFieldIndex = FixedFieldCount;

        public FunctionAllocationSet(ContextWrapper context)
        {
            Context = context;
        }

        private ContextWrapper Context { get; }

        public LocalAllocationValueSource CreateLocalAllocation(string containingFunctionName, string allocationName, NIType allocationType)
        {
            List<LocalAllocation> functionLocals;
            if (!_functionLocalAllocations.TryGetValue(containingFunctionName, out functionLocals))
            {
                functionLocals = new List<LocalAllocation>();
                _functionLocalAllocations[containingFunctionName] = functionLocals;
            }
            int allocationIndex = functionLocals.Count;
            functionLocals.Add(new LocalAllocation(allocationName, allocationType));
            return new LocalAllocationValueSource(allocationName, this, containingFunctionName, allocationIndex);
        }

        public StateFieldValueSource CreateStateField(string allocationName, NIType allocationType)
        {
            int fieldIndex = _stateFields.Count;
            _stateFields.Add(new StateFieldAllocation(allocationName, allocationType));
            return new StateFieldValueSource(allocationName, this, fieldIndex);
        }

        public OutputParameterStateFieldValueSource CreateOutputParameterStateField(string allocationName, NIType allocationType)
        {
            int fieldIndex = _stateFields.Count;
            _stateFields.Add(new StateFieldAllocation(allocationName, allocationType.CreateMutableReference()));
            return new OutputParameterStateFieldValueSource(allocationName, this, fieldIndex);
        }

        public OutputParameterLocalAllocationValueSource CreateOutputParameterLocalAllocation(string containingFunctionName, string allocationName, NIType allocationType)
        {
            List<LocalAllocation> functionLocals;
            if (!_functionLocalAllocations.TryGetValue(containingFunctionName, out functionLocals))
            {
                functionLocals = new List<LocalAllocation>();
                _functionLocalAllocations[containingFunctionName] = functionLocals;
            }
            int allocationIndex = functionLocals.Count;
            functionLocals.Add(new LocalAllocation(allocationName, allocationType.CreateMutableReference()));
            return new OutputParameterLocalAllocationValueSource(allocationName, this, containingFunctionName, allocationIndex);
        }

        public void InitializeStateType(Module module, string functionName)
        {
            var stateFieldTypes = new List<LLVMTypeRef>();
            // fixed fields
            stateFieldTypes.Add(FunctionCompletionStatusType(Context));    // function completion status: 0 = not done, 1 = completed successfully, 2 = panic
            stateFieldTypes.Add(Context.WakerType());  // caller waker
            // end fixed fields
            stateFieldTypes.AddRange(_stateFields.Select(a => Context.AsLLVMType(a.Type)));
            StateType = Context.NamedStructType(functionName + "_state_t", stateFieldTypes.ToArray());
        }

        public void InitializeFunctionLocalAllocations(string functionName, IRBuilder builder)
        {
            List<LocalAllocation> functionLocals;
            if (!_functionLocalAllocations.TryGetValue(functionName, out functionLocals))
            {
                return;
            }
            if (functionLocals.Any(l => !l.Pointer.IsUninitialized()))
            {
                throw new InvalidOperationException("Already initialized allocations for function " + functionName);
            }
            foreach (LocalAllocation allocation in functionLocals)
            {
                allocation.Pointer = builder.CreateAlloca(Context.AsLLVMType(allocation.Type), allocation.Name);
            }
        }

        public LLVMTypeRef StateType { get; private set; }

        public FunctionCompilerState CompilerState { get; set; }

        public LLVMValueRef StatePointer => CompilerState.StatePointer;

        public LLVMValueRef GetLocalAllocationPointer(string functionName, int index)
        {
            return _functionLocalAllocations[functionName][index].Pointer;
        }

        internal LLVMValueRef GetStateDonePointer(IRBuilder builder)
        {
            StatePointer.ThrowIfNull();
            return builder.CreateStructGEP(StatePointer, 0u, "donePtr");
        }

        internal LLVMValueRef GetStateCallerWakerPointer(IRBuilder builder)
        {
            StatePointer.ThrowIfNull();
            return builder.CreateStructGEP(StatePointer, 1u, "callerWakerPtr");
        }

        internal LLVMValueRef GetStateFieldPointer(IRBuilder builder, int fieldIndex)
        {
            StatePointer.ThrowIfNull();
            return builder.CreateStructGEP(StatePointer, (uint)(fieldIndex + FixedFieldCount), _stateFields[fieldIndex].Name + "_fieldptr");
        }

        internal LLVMTypeRef GetStateFieldPointerType(int fieldIndex)
        {
            return LLVMTypeRef.PointerType(Context.AsLLVMType(_stateFields[fieldIndex].Type), 0u);
        }
    }
}
