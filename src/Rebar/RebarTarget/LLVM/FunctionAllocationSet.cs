using System;
using System.Collections.Generic;
using System.Linq;
using LLVMSharp;
using NationalInstruments.DataTypes;
using Rebar.Common;

namespace Rebar.RebarTarget.LLVM
{
    internal class FunctionAllocationSet
    {
        private readonly Dictionary<string, List<Tuple<string, NIType>>> _functionLocalAllocationTypes = new Dictionary<string, List<Tuple<string, NIType>>>();
        private readonly List<Tuple<string, NIType>> _stateFieldTypes = new List<Tuple<string, NIType>>();
        private readonly Dictionary<string, LLVMValueRef[]> _functionLocalAllocationPointers = new Dictionary<string, LLVMValueRef[]>();

        private const int FixedFieldCount = 2;
        public const int FirstParameterFieldIndex = FixedFieldCount;

        public LocalAllocationValueSource CreateLocalAllocation(string containingFunctionName, string allocationName, NIType allocationType)
        {
            List<Tuple<string, NIType>> functionLocals;
            if (!_functionLocalAllocationTypes.TryGetValue(containingFunctionName, out functionLocals))
            {
                functionLocals = new List<Tuple<string, NIType>>();
                _functionLocalAllocationTypes[containingFunctionName] = functionLocals;
            }
            int allocationIndex = functionLocals.Count;
            functionLocals.Add(new Tuple<string, NIType>(allocationName, allocationType));
            return new LocalAllocationValueSource(allocationName, this, containingFunctionName, allocationIndex);
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
            stateFieldTypes.Add(LLVMExtensions.WakerType);  // caller waker
            // end fixed fields
            stateFieldTypes.AddRange(_stateFieldTypes.Select(a => a.Item2.AsLLVMType()));
            StateType.StructSetBody(stateFieldTypes.ToArray(), false);
        }

        public void InitializeFunctionLocalAllocations(string functionName, IRBuilder builder)
        {
            List<Tuple<string, NIType>> functionLocals;
            if (_functionLocalAllocationTypes.TryGetValue(functionName, out functionLocals))
            {
                if (_functionLocalAllocationPointers.ContainsKey(functionName))
                {
                    throw new InvalidOperationException("Already initialized allocations for function " + functionName);
                }
                _functionLocalAllocationPointers[functionName] = functionLocals.Select(a => builder.CreateAlloca(a.Item2.AsLLVMType(), a.Item1)).ToArray();
            }
        }

        public LLVMTypeRef StateType { get; private set; }

        public FunctionCompilerState CompilerState { get; set; }

        public LLVMValueRef StatePointer => CompilerState.StatePointer;

        public LLVMValueRef GetLocalAllocationPointer(string functionName, int index)
        {
            return _functionLocalAllocationPointers[functionName][index];
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
            return builder.CreateStructGEP(StatePointer, (uint)(fieldIndex + FixedFieldCount), _stateFieldTypes[fieldIndex].Item1 + "_fieldptr");
        }

        internal LLVMTypeRef GetStateFieldPointerType(int fieldIndex)
        {
            return LLVMTypeRef.PointerType(_stateFieldTypes[fieldIndex].Item2.AsLLVMType(), 0u);
        }
    }
}
