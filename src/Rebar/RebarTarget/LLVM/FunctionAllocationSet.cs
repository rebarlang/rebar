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
