using System.Collections.Generic;
using System.Linq;
using LLVMSharp;
using NationalInstruments.DataTypes;

namespace Rebar.RebarTarget.LLVM
{
    internal class AsynchronousFunctionModuleBuilder : FunctionModuleBuilder
    {
        private readonly string _functionName;
        private readonly IEnumerable<AsyncStateGroup> _asyncStateGroups;

        public static readonly LLVMTypeRef PollFunctionType = LLVMTypeRef.FunctionType(
            LLVMTypeRef.VoidType(),
            new LLVMTypeRef[]
            {
                LLVMExtensions.VoidPointerType,
                LLVMTypeRef.PointerType(LLVMExtensions.ScheduledTaskFunctionType, 0u),
                LLVMExtensions.VoidPointerType
            },
            false);

        public AsynchronousFunctionModuleBuilder(
            Module module,
            FunctionCompilerSharedData sharedData,
            string functionName,
            IEnumerable<AsyncStateGroup> asyncStateGroups)
            : base(module, sharedData)
        {
            _functionName = functionName;
            _asyncStateGroups = asyncStateGroups;

            var fireCountFields = new Dictionary<AsyncStateGroup, StateFieldValueSource>();
            foreach (AsyncStateGroup asyncStateGroup in _asyncStateGroups)
            {
                string groupName = asyncStateGroup.Label;
                if (asyncStateGroup.MaxFireCount > 1)
                {
                    fireCountFields[asyncStateGroup] = SharedData.AllocationSet.CreateStateField($"{groupName}FireCount", PFTypes.Int32);
                }
            }
            SharedData.AllocationSet.InitializeStateType(module, functionName);
            LLVMTypeRef groupFunctionType = LLVMTypeRef.FunctionType(
                LLVMTypeRef.VoidType(),
                new LLVMTypeRef[] { LLVMTypeRef.PointerType(SharedData.AllocationSet.StateType, 0u) },
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

                LLVMBasicBlockRef groupBasicBlock = groupFunction.AppendBasicBlock($"{asyncStateGroup.Label}_begin");
                LLVMBasicBlockRef continueBasicBlock = asyncStateGroup.IsSkippable
                    ? groupFunction.AppendBasicBlock($"{asyncStateGroup.Label}_continue")
                    : default(LLVMBasicBlockRef);
                LLVMBasicBlockRef endBasicBlock = asyncStateGroup.IsSkippable
                    ? groupFunction.AppendBasicBlock($"{asyncStateGroup.Label}_end")
                    : default(LLVMBasicBlockRef);
                StateFieldValueSource fireCountStateField;
                fireCountFields.TryGetValue(asyncStateGroup, out fireCountStateField);
                AsyncStateGroups[asyncStateGroup] = new AsyncStateGroupData(asyncStateGroup, groupFunction, groupBasicBlock, continueBasicBlock, endBasicBlock, fireCountStateField);
            }
        }

        public override void CompileFunction()
        {
            foreach (AsyncStateGroup asyncStateGroup in _asyncStateGroups)
            {
                AsyncStateGroupData groupData = AsyncStateGroups[asyncStateGroup];
                CompileAsyncStateGroup(asyncStateGroup, new AsyncStateGroupCompilerState(groupData.Function, new IRBuilder()));
            }

            LLVMValueRef initializeStateFunction = BuildInitializeStateFunction(GetParameterLLVMTypes());
            LLVMValueRef pollFunction = BuildPollFunction();
        }

        private LLVMValueRef BuildInitializeStateFunction(IEnumerable<LLVMTypeRef> parameterTypes)
        {
            LLVMTypeRef initializeStateFunctionType = LLVMTypeRef.FunctionType(
                LLVMExtensions.VoidPointerType,
                parameterTypes.ToArray(),
                false);
            LLVMValueRef initializeStateFunction = Module.AddFunction(FunctionNames.GetInitializeStateFunctionName(_functionName), initializeStateFunctionType);
            var builder = new IRBuilder();
            LLVMBasicBlockRef entryBlock = initializeStateFunction.AppendBasicBlock("entry");

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef statePtr = builder.CreateMalloc(SharedData.AllocationSet.StateType, "statePtr");
            CurrentState = new OuterFunctionCompilerState(initializeStateFunction, builder) { StateMalloc = statePtr };
            GenerateStoreCompletionState(0);

            InitializeParameterAllocations(initializeStateFunction, builder);
            LLVMValueRef bitCastStatePtr = builder.CreateBitCast(statePtr, LLVMExtensions.VoidPointerType, "bitCastStatePtr");
            builder.CreateRet(bitCastStatePtr);

            return initializeStateFunction;
        }

        private LLVMValueRef BuildPollFunction()
        {
            LLVMValueRef pollFunction = Module.AddFunction(FunctionNames.GetPollFunctionName(_functionName), PollFunctionType);
            LLVMBasicBlockRef entryBlock = pollFunction.AppendBasicBlock("entry");
            var builder = new IRBuilder();

            var outerFunctionCompilerState = new OuterFunctionCompilerState(pollFunction, builder);
            CurrentState = outerFunctionCompilerState;
            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef stateVoidPtr = pollFunction.GetParam(0u),
                statePtr = builder.CreateBitCast(stateVoidPtr, LLVMTypeRef.PointerType(SharedData.AllocationSet.StateType, 0u), "statePtr");
            outerFunctionCompilerState.StateMalloc = statePtr;

            // store the caller waker in the state
            // TODO: create constants for these positions
            LLVMValueRef waker = builder.BuildStructValue(
                LLVMExtensions.WakerType,
                new LLVMValueRef[] { pollFunction.GetParam(1u), pollFunction.GetParam(2u) },
                "callerWaker");
            builder.CreateStore(waker, builder.CreateStructGEP(statePtr, 1u, "callerWakerPtr"));

            // set initial fire counts
            foreach (AsyncStateGroupData groupData in AsyncStateGroups.Values)
            {
                groupData.CreateFireCountReset(builder);
            }

            // schedule initial group(s)
            CreateScheduleCallsForAsyncStateGroups(
                builder,
                statePtr,
                AsyncStateGroups.Keys.Where(group => !group.Predecessors.Any()));

            builder.CreateRetVoid();
            return pollFunction;
        }
    }
}
