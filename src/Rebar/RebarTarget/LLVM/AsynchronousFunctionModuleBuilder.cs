using System.Collections.Generic;
using System.Linq;
using LLVMSharp;
using NationalInstruments.DataTypes;

namespace Rebar.RebarTarget.LLVM
{
    internal class AsynchronousFunctionModuleBuilder : FunctionModuleBuilder
    {
        private readonly FunctionCompiler _functionCompiler;
        private readonly string _functionName;
        private readonly IEnumerable<AsyncStateGroup> _asyncStateGroups;
        private readonly FunctionAllocationSet _allocationSet;

        public AsynchronousFunctionModuleBuilder(
            Module module,
            FunctionCompiler functionCompiler,
            string functionName,
            IEnumerable<AsyncStateGroup> asyncStateGroups)
            : base(module, functionCompiler)
        {
            _functionCompiler = functionCompiler;
            _functionName = functionName;
            _asyncStateGroups = asyncStateGroups;
            _allocationSet = functionCompiler.AllocationSet;

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
                functionCompiler.AsyncStateGroups[asyncStateGroup] = new AsyncStateGroupData(asyncStateGroup, groupFunction, groupBasicBlock, fireCountStateField);
            }
        }

        public override void CompileFunction()
        {
            List<LLVMTypeRef> parameterTypes = _functionCompiler.GetParameterLLVMTypes();
            foreach (AsyncStateGroup asyncStateGroup in _asyncStateGroups)
            {
                AsyncStateGroupData groupData = _functionCompiler.AsyncStateGroups[asyncStateGroup];
                CompileAsyncStateGroup(asyncStateGroup, new AsyncStateGroupCompilerState(groupData.Function, new IRBuilder()));
            }

            LLVMValueRef initializeStateFunction = BuildInitializeStateFunction(parameterTypes);
            LLVMValueRef pollFunction = BuildPollFunction();

            BuildOuterFunction(parameterTypes, initializeStateFunction, pollFunction);
        }

        private void BuildOuterFunction(List<LLVMTypeRef> parameterTypes, LLVMValueRef initializeStateFunction, LLVMValueRef pollFunction)
        {
            LLVMValueRef outerFunction = InitializeOuterFunction(_functionName, parameterTypes);
            var builder = new IRBuilder();
            _functionCompiler.CurrentState = new OuterFunctionCompilerState(outerFunction, builder);
            LLVMBasicBlockRef outerEntryBlock = outerFunction.AppendBasicBlock("entry");
            builder.PositionBuilderAtEnd(outerEntryBlock);

            // TODO: deallocate state block for the top-level case!
            LLVMValueRef voidStatePtr = builder.CreateCall(
                initializeStateFunction,
                outerFunction.GetParams().Skip(2).ToArray(),
                "voidStatePtr"),
                statePtr = builder.CreateBitCast(voidStatePtr, LLVMTypeRef.PointerType(_allocationSet.StateType, 0u), "statePtr");
            ((OuterFunctionCompilerState)_functionCompiler.CurrentState).StateMalloc = statePtr;
            // TODO: create constants for these positions
            LLVMValueRef waker = builder.BuildStructValue(
                LLVMExtensions.WakerType,
                new LLVMValueRef[] { outerFunction.GetParam(0u), outerFunction.GetParam(1u) },
                "callerWaker");
            builder.CreateStore(waker, builder.CreateStructGEP(statePtr, 1u, "callerWakerPtr"));

            builder.CreateCall(
                pollFunction,
                new LLVMValueRef[] { voidStatePtr },
                string.Empty);
            builder.CreateRetVoid();
        }

        private LLVMValueRef BuildInitializeStateFunction(List<LLVMTypeRef> parameterTypes)
        {
            LLVMTypeRef initializeStateFunctionType = LLVMTypeRef.FunctionType(
                LLVMExtensions.VoidPointerType,
                parameterTypes.ToArray(),
                false);
            LLVMValueRef initializeStateFunction = Module.AddFunction(FunctionCompiler.GetInitializeStateFunctionName(_functionName), initializeStateFunctionType);
            var builder = new IRBuilder();
            LLVMBasicBlockRef entryBlock = initializeStateFunction.AppendBasicBlock("entry");

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef statePtr = builder.CreateMalloc(_allocationSet.StateType, "statePtr");
            _functionCompiler.CurrentState = new OuterFunctionCompilerState(initializeStateFunction, builder) { StateMalloc = statePtr };
            builder.CreateStore(false.AsLLVMValue(), builder.CreateStructGEP(statePtr, 0u, "donePtr"));

            _functionCompiler.InitializeParameterAllocations(initializeStateFunction, builder);
            LLVMValueRef bitCastStatePtr = builder.CreateBitCast(statePtr, LLVMExtensions.VoidPointerType, "bitCastStatePtr");
            builder.CreateRet(bitCastStatePtr);

            return initializeStateFunction;
        }

        private LLVMValueRef BuildPollFunction()
        {
            LLVMValueRef pollFunction = Module.AddFunction(FunctionCompiler.GetPollFunctionName(_functionName), LLVMExtensions.ScheduledTaskFunctionType);
            LLVMBasicBlockRef entryBlock = pollFunction.AppendBasicBlock("entry");
            var builder = new IRBuilder();

            var outerFunctionCompilerState = new OuterFunctionCompilerState(pollFunction, builder);
            _functionCompiler.CurrentState = outerFunctionCompilerState;
            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef stateVoidPtr = pollFunction.GetParam(0u),
                statePtr = builder.CreateBitCast(stateVoidPtr, LLVMTypeRef.PointerType(_allocationSet.StateType, 0u), "statePtr");
            outerFunctionCompilerState.StateMalloc = statePtr;

            // set initial fire counts
            foreach (AsyncStateGroupData groupData in _functionCompiler.AsyncStateGroups.Values)
            {
                groupData.CreateFireCountReset(builder);
            }

            // schedule initial group(s)
            CreateScheduleCallsForAsyncStateGroups(
                builder,
                statePtr,
                _functionCompiler.AsyncStateGroups.Keys.Where(group => !group.Predecessors.Any()));

            builder.CreateRetVoid();
            return pollFunction;
        }
    }
}
