using System.Collections.Generic;
using LLVMSharp;
using NationalInstruments.DataTypes;

namespace Rebar.RebarTarget.LLVM
{
    internal class AsynchronousFunctionModuleBuilder : FunctionModuleBuilder
    {
        private readonly IEnumerable<AsyncStateGroup> _asyncStateGroups;
        private readonly FunctionAllocationSet _allocationSet;

        public AsynchronousFunctionModuleBuilder(
            Module module,
            FunctionCompiler functionCompiler,
            string functionName,
            IEnumerable<AsyncStateGroup> asyncStateGroups,
            FunctionAllocationSet allocationSet)
            : base(module)
        {
            _asyncStateGroups = asyncStateGroups;
            _allocationSet = allocationSet;

            var fireCountFields = new Dictionary<AsyncStateGroup, StateFieldValueSource>();
            foreach (AsyncStateGroup asyncStateGroup in _asyncStateGroups)
            {
                string groupName = asyncStateGroup.Label;
                if (asyncStateGroup.MaxFireCount > 1)
                {
                    fireCountFields[asyncStateGroup] = _allocationSet.CreateStateField($"{groupName}FireCount", PFTypes.Int32);
                }
            }
            _allocationSet.InitializeStateType(module, functionName);
            LLVMTypeRef groupFunctionType = LLVMTypeRef.FunctionType(
                LLVMTypeRef.VoidType(),
                new LLVMTypeRef[] { LLVMTypeRef.PointerType(_allocationSet.StateType, 0u) },
                false);

            var functions = new Dictionary<string, LLVMValueRef>();
            foreach (AsyncStateGroup asyncStateGroup in _asyncStateGroups)
            {
                LLVMValueRef groupFunction;
                if (!functions.TryGetValue(asyncStateGroup.FunctionId, out groupFunction))
                {
                    string groupFunctionName = $"{functionName}::{asyncStateGroup.FunctionId}";
                    groupFunction = Module.AddFunction(groupFunctionName, groupFunctionType);
                    functions[asyncStateGroup.FunctionId] = groupFunction;
                }

                LLVMBasicBlockRef groupBasicBlock = groupFunction.AppendBasicBlock(asyncStateGroup.Label);
                StateFieldValueSource fireCountStateField;
                fireCountFields.TryGetValue(asyncStateGroup, out fireCountStateField);
                functionCompiler.AsyncStateGroups[asyncStateGroup] = new FunctionCompiler.AsyncStateGroupData(asyncStateGroup, groupFunction, groupBasicBlock, fireCountStateField);
            }
        }
    }
}
