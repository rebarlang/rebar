using System;
using System.Collections.Generic;
using System.Linq;
using LLVMSharp;
using NationalInstruments;
using NationalInstruments.CommonModel;
using NationalInstruments.DataTypes;

namespace Rebar.RebarTarget.LLVM
{
    internal abstract class FunctionModuleBuilder
    {
        private LLVMValueRef _localDoneAllocationPtr;

        protected FunctionModuleBuilder(FunctionCompilerSharedData sharedData)
        {
            SharedData = sharedData;
            AsyncStateGroups = new Dictionary<AsyncStateGroup, AsyncStateGroupData>();
        }

        public Module Module => SharedData.Module;

        protected FunctionCompilerSharedData SharedData { get; }

        internal Dictionary<AsyncStateGroup, AsyncStateGroupData> AsyncStateGroups { get; }

        internal AsyncStateGroupData CurrentGroupData => AsyncStateGroups[CurrentGroup];

        protected AsyncStateGroup CurrentGroup { get; set; }

        protected FunctionCompilerState CurrentState
        {
            get { return SharedData.CurrentState; }
            set { SharedData.CurrentState = value; }
        }

        protected IRBuilder Builder => CurrentState.Builder;

        protected FunctionAllocationSet AllocationSet => SharedData.AllocationSet;

        public abstract void CompileFunction();

        protected void InitializeParameterAllocations(LLVMValueRef function, IRBuilder builder)
        {
            uint parameterIndex = 0u;
            foreach (ParameterInfo parameter in SharedData.OrderedParameters)
            {
                ValueSource parameterValueSource = SharedData.VariableStorage.GetValueSourceForVariable(parameter.ParameterVariable);
                LLVMValueRef parameterAllocationPtr = ((IAddressableValueSource)parameterValueSource).GetAddress(builder);
                builder.CreateStore(function.GetParam(parameterIndex), parameterAllocationPtr);
                ++parameterIndex;
            }
        }

        protected IEnumerable<LLVMTypeRef> GetParameterLLVMTypes()
        {
            foreach (var parameter in SharedData.OrderedParameters)
            {
                NIType parameterType = parameter.ParameterVariable.Type;
                switch (parameter.Direction)
                {
                    case Direction.Input:
                        yield return SharedData.Context.AsLLVMType(parameterType);
                        break;
                    case Direction.Output:
                        yield return LLVMTypeRef.PointerType(SharedData.Context.AsLLVMType(parameterType), 0u);
                        break;
                    default:
                        throw new NotImplementedException("Can only handle in and out parameters");
                }
            }
        }

        protected void CompileAsyncStateGroup(AsyncStateGroup asyncStateGroup, FunctionCompilerState compilerState)
        {
            FunctionCompilerState previousState = CurrentState;
            CurrentGroup = asyncStateGroup;
            CurrentState = compilerState;
            AsyncStateGroupData groupData = AsyncStateGroups[asyncStateGroup];
            LLVMValueRef groupFunction = groupData.Function;

            Builder.PositionBuilderAtEnd(groupData.InitialBasicBlock);

            // Here we are assuming that the group whose label matches the function name is also the entry group.
            if (asyncStateGroup.FunctionId == asyncStateGroup.Label && this is AsynchronousFunctionModuleBuilder)
            {
                AllocationSet.InitializeFunctionLocalAllocations(asyncStateGroup.FunctionId, Builder);
            }

            var conditionalContinuation = asyncStateGroup.Continuation as ConditionallyScheduleGroupsContinuation;
            if (conditionalContinuation != null)
            {
                var continuationConditionValueSource = (IInitializableValueSource)SharedData.VariableStorage.GetValueSourceForVariable(asyncStateGroup.ContinuationCondition);
                continuationConditionValueSource.InitializeValue(Builder, SharedData.Context.AsLLVMValue(false));
            }

            if (asyncStateGroup.IsSkippable)
            {
                if (asyncStateGroup.FunctionId == asyncStateGroup.Label && this is SynchronousFunctionModuleBuilder)
                {
                    _localDoneAllocationPtr = Builder.CreateAlloca(FunctionAllocationSet.FunctionCompletionStatusType(SharedData.Context), "donePtr");
                    Builder.CreateBr(groupData.ContinueBasicBlock);
                }
                else
                {
                    LLVMValueRef shouldContinue = GenerateContinueStateCheck();
                    Builder.CreateCondBr(shouldContinue, groupData.ContinueBasicBlock, groupData.ExitBasicBlock);
                }
                Builder.PositionBuilderAtEnd(groupData.ContinueBasicBlock);
            }

            groupData.CreateFireCountReset(Builder);

            foreach (Visitation visitation in asyncStateGroup.Visitations)
            {
                visitation.Visit<bool>(SharedData.VisitationHandler);
            }

            var unconditionalContinuation = asyncStateGroup.Continuation as UnconditionallySchduleGroupsContinuation;
            if (unconditionalContinuation != null 
                && !unconditionalContinuation.UnconditionalSuccessors.Any()
                && this is AsynchronousFunctionModuleBuilder)
            {
                GenerateStoreCompletionState(RuntimeConstants.FunctionCompletedNormallyStatus);
            }

            if (asyncStateGroup.IsSkippable)
            {
                Builder.CreateBr(groupData.ExitBasicBlock);
                Builder.PositionBuilderAtEnd(groupData.ExitBasicBlock);
            }

            bool returnAfterGroup = true;
            if (unconditionalContinuation != null)
            {
                if (unconditionalContinuation.UnconditionalSuccessors.Any())
                {
                    AsyncStateGroup singleSuccessor;
                    if (unconditionalContinuation.UnconditionalSuccessors.TryGetSingleElement(out singleSuccessor) && singleSuccessor.FunctionId == asyncStateGroup.FunctionId)
                    {
                        Builder.CreateBr(AsyncStateGroups[singleSuccessor].InitialBasicBlock);
                        returnAfterGroup = false;
                    }
                    else
                    {
                        CreateInvokeOrScheduleOfSuccessors(unconditionalContinuation.UnconditionalSuccessors);
                    }
                }
                else if (this is AsynchronousFunctionModuleBuilder)
                {
                    GenerateFunctionTerminator();
                }
            }
            if (conditionalContinuation != null)
            {
                LLVMValueRef condition = SharedData.VariableStorage.GetValueSourceForVariable(asyncStateGroup.ContinuationCondition).GetValue(Builder);

                bool isBooleanCondition = conditionalContinuation.SuccessorConditionGroups.Count == 2;
                bool allSynchronousContinuations = conditionalContinuation.SuccessorConditionGroups.All(group =>
                {
                    AsyncStateGroup singleSuccessor;
                    return group.TryGetSingleElement(out singleSuccessor) && singleSuccessor.FunctionId == asyncStateGroup.FunctionId;
                });

                if (allSynchronousContinuations)
                {
                    LLVMBasicBlockRef[] initialBlocks = conditionalContinuation.SuccessorConditionGroups
                        .Select(g => AsyncStateGroups[g.First()].InitialBasicBlock)
                        .ToArray();
                    if (isBooleanCondition)
                    {
                        Builder.CreateCondBr(condition, initialBlocks[1], initialBlocks[0]);
                    }
                    else
                    {
                        LLVMValueRef conditionSwitch = Builder.CreateSwitch(condition, initialBlocks[0], (uint)(initialBlocks.Length - 1));
                        for (int i = 1; i < initialBlocks.Length; ++i)
                        {
                            conditionSwitch.AddCase(SharedData.Context.AsLLVMValue((byte)i), initialBlocks[i]);
                        }
                    }
                    returnAfterGroup = false;
                }
                else
                {
                    LLVMBasicBlockRef[] continuationConditionBlocks = Enumerable.Range(0, conditionalContinuation.SuccessorConditionGroups.Count)
                        .Select(i => groupFunction.AppendBasicBlock($"continuationCondition{i}"))
                        .ToArray();
                    LLVMBasicBlockRef exitBlock = groupFunction.AppendBasicBlock("exit");
                    if (isBooleanCondition)
                    {
                        Builder.CreateCondBr(condition, continuationConditionBlocks[1], continuationConditionBlocks[0]);
                    }
                    else
                    {
                        LLVMValueRef conditionSwitch = Builder.CreateSwitch(condition, continuationConditionBlocks[0], (uint)(conditionalContinuation.SuccessorConditionGroups.Count - 1));
                        for (int i = 1; i < continuationConditionBlocks.Length; ++i)
                        {
                            conditionSwitch.AddCase(SharedData.Context.AsLLVMValue((byte)i), continuationConditionBlocks[i]);
                        }
                    }

                    for (int i = 0; i < continuationConditionBlocks.Length; ++i)
                    {
                        Builder.PositionBuilderAtEnd(continuationConditionBlocks[i]);
                        CreateInvokeOrScheduleOfSuccessors(conditionalContinuation.SuccessorConditionGroups[i]);
                        Builder.CreateBr(exitBlock);
                    }

                    Builder.PositionBuilderAtEnd(exitBlock);
                }
            }

            if (returnAfterGroup)
            {
                Builder.CreateRetVoid();
            }

            CurrentGroup = null;
            CurrentState = previousState;
        }

        /// <summary>
        /// Generates code that will cause a set of successor <see cref="AsyncStateGroup"/>s to be run.
        /// </summary>
        /// <param name="successors">The successor groups to be run.</param>
        /// <remarks>If there is a single successor that has a single unconditional predecessor, the generated
        /// code will invoke the successor directly; otherwise, it will generate schedule or partial_schedule
        /// calls for each successor as appropriate.</remarks>
        private void CreateInvokeOrScheduleOfSuccessors(IEnumerable<AsyncStateGroup> successors)
        {
            AsyncStateGroup singleSuccessor;
            if (successors.TryGetSingleElement(out singleSuccessor)
                && !singleSuccessor.SignaledConditionally
                && singleSuccessor.Predecessors.HasExactly(1))
            {
                // our single successor only has us as a predecessor, so we can tail call it directly
                Builder.CreateCall(AsyncStateGroups[singleSuccessor].Function, new LLVMValueRef[] { CurrentState.StatePointer }, string.Empty);
            }
            else
            {
                CreateScheduleCallsForAsyncStateGroups(Builder, CurrentState.StatePointer, successors);
            }
        }

        protected void CreateScheduleCallsForAsyncStateGroups(IRBuilder builder, LLVMValueRef statePointer, IEnumerable<AsyncStateGroup> asyncStateGroups)
        {
            LLVMTypeRef scheduledTaskType = SharedData.Context.GetScheduledTaskType();
            LLVMValueRef bitCastStatePtr = builder.CreateBitCast(statePointer, SharedData.Context.VoidPointerType(), "bitCastStatePtr");
            foreach (AsyncStateGroup successor in asyncStateGroups)
            {
                AsyncStateGroupData successorData = AsyncStateGroups[successor];
                LLVMValueRef bitCastSuccessorFunction = builder.CreateBitCast(
                    successorData.Function,
                    LLVMTypeRef.PointerType(SharedData.Context.ScheduledTaskFunctionType(), 0u),
                    "bitCastFunction");
                if (!successor.SignaledConditionally && successor.Predecessors.HasMoreThan(1))
                {
                    LLVMValueRef fireCountPtr = ((IAddressableValueSource)successorData.FireCountStateField).GetAddress(builder);
                    builder.CreateCall(
                        SharedData.FunctionImporter.GetImportedCommonFunction(CommonModules.PartialScheduleName),
                        new LLVMValueRef[] { bitCastStatePtr, fireCountPtr, bitCastSuccessorFunction },
                        string.Empty);
                }
                else
                {
                    builder.CreateCall(
                        SharedData.FunctionImporter.GetImportedCommonFunction(CommonModules.ScheduleName),
                        new LLVMValueRef[] { bitCastSuccessorFunction, bitCastStatePtr },
                        string.Empty);
                }
            }
        }

        private LLVMValueRef GetStateDonePointer()
        {
            return this is SynchronousFunctionModuleBuilder
                ? _localDoneAllocationPtr
                : AllocationSet.GetStateDonePointer(Builder);
        }

        private LLVMValueRef GenerateContinueStateCheck()
        {
            LLVMValueRef done = Builder.CreateLoad(GetStateDonePointer(), "done"),
                shouldContinue = Builder.CreateICmp(LLVMIntPredicate.LLVMIntEQ, done, SharedData.Context.AsLLVMValue((byte)0), "shouldContinue");
            return shouldContinue;
        }

        public void GenerateStoreCompletionState(byte completionState)
        {
            Builder.CreateStore(SharedData.Context.AsLLVMValue(completionState), GetStateDonePointer());
        }

        private void GenerateFunctionTerminator()
        {
            LLVMValueRef callerWakerPtr = AllocationSet.GetStateCallerWakerPointer(Builder),
                callerWaker = Builder.CreateLoad(callerWakerPtr, "callerWaker");

            // activate the caller waker
            // TODO: invoke caller waker directly, or schedule?
            Builder.CreateCall(SharedData.FunctionImporter.GetImportedCommonFunction(CommonModules.InvokeName), new LLVMValueRef[] { callerWaker }, string.Empty);
        }
    }
}
