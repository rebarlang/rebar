using System;
using System.Collections.Generic;
using System.Linq;
using LLVMSharp;
using NationalInstruments;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

namespace Rebar.RebarTarget.LLVM
{
    internal partial class FunctionCompiler
    {
        private class AsyncStateGroupData
        {
            public AsyncStateGroupData(
                AsyncStateGroup asyncStateGroup,
                LLVMValueRef function,
                LLVMBasicBlockRef initialBasicBlock,
                StateFieldValueSource fireCountStateField)
            {
                AsyncStateGroup = asyncStateGroup;
                Function = function;
                InitialBasicBlock = initialBasicBlock;
                FireCountStateField = fireCountStateField;
            }

            /// <summary>
            /// The associated <see cref="AsyncStateGroup"/>.
            /// </summary>
            public AsyncStateGroup AsyncStateGroup { get; }

            /// <summary>
            /// The LLVM function into which the <see cref="AsyncStateGroup"/> will be generated.
            /// </summary>
            public LLVMValueRef Function { get; }

            /// <summary>
            /// The initial basic block created for the <see cref="AsyncStateGroup"/>.
            /// </summary>
            public LLVMBasicBlockRef InitialBasicBlock { get; }

            /// <summary>
            /// If the <see cref="AsyncStateGroup"/> needs to be scheduled by multiple predecessors, this
            /// contains a <see cref="StateFieldValueSource"/> for the group's fire count.
            /// </summary>
            public StateFieldValueSource FireCountStateField { get; }

            /// <summary>
            /// If the <see cref="AsyncStateGroup"/> may schedule different sets of successors conditionally, this
            /// contains an alloca pointer for the condition variable that will determine which successors to
            /// schedule.
            /// </summary>
            public LLVMValueRef ContinuationConditionVariable { get; set; }

            /// <summary>
            /// Generates code that will reset the fire count variable for this <see cref="AsyncStateGroup"/>
            /// in the state block to its maximum value.
            /// </summary>
            /// <param name="builder"></param>
            public void CreateFireCountReset(IRBuilder builder)
            {
                if (FireCountStateField != null)
                {
                    ((IUpdateableValueSource)FireCountStateField).UpdateValue(builder, AsyncStateGroup.MaxFireCount.AsLLVMValue());
                }
            }

            /// <summary>
            /// Generates code that will set the value of the local variable that determines which successors to schedule.
            /// </summary>
            /// <param name="builder"></param>
            /// <param name="value">A value corresponding to a particular set of successors.</param>
            public void CreateContinuationStateChange(IRBuilder builder, LLVMValueRef stateValue)
            {
                if (stateValue.TypeOf().GetIntTypeWidth() != 1)
                {
                    throw new ArgumentException("Expected a boolean value.");
                }
                builder.CreateStore(stateValue, ContinuationConditionVariable);
            }
        }

        private Dictionary<AsyncStateGroup, AsyncStateGroupData> AsyncStateGroups { get; }

        public void CompileFunction(DfirRoot dfirRoot)
        {
            TargetDfir = dfirRoot;
            foreach (AsyncStateGroup asyncStateGroup in _asyncStateGroups)
            {
                CompileAsyncStateGroup(asyncStateGroup);
            }

            var parameterTypes = new List<LLVMTypeRef>();
            foreach (var dataItem in _parameterDataItems.OrderBy(d => d.ConnectorPaneIndex))
            {
                if (dataItem.ConnectorPaneInputPassingRule == NIParameterPassingRule.Required
                    && dataItem.ConnectorPaneOutputPassingRule == NIParameterPassingRule.NotAllowed)
                {
                    parameterTypes.Add(dataItem.DataType.AsLLVMType());
                }
                else if (dataItem.ConnectorPaneInputPassingRule == NIParameterPassingRule.NotAllowed
                    && dataItem.ConnectorPaneOutputPassingRule == NIParameterPassingRule.Optional)
                {
                    parameterTypes.Add(LLVMTypeRef.PointerType(dataItem.DataType.AsLLVMType(), 0u));
                }
                else
                {
                    throw new NotImplementedException("Can only handle in and out parameters");
                }
            }

            LLVMValueRef allocateStateFunction = BuildAllocateStateFunction(),
                initializeStateFunction = BuildInitializeStateFunction(parameterTypes),
                pollFunction = BuildPollFunction();
            BuildOuterFunction(parameterTypes, allocateStateFunction, initializeStateFunction, pollFunction);
        }

        private void CompileAsyncStateGroup(AsyncStateGroup asyncStateGroup)
        {
            FunctionCompilerState previousState = CurrentState;
            CurrentGroup = asyncStateGroup;
            AsyncStateGroupData groupData = AsyncStateGroups[asyncStateGroup];
            LLVMValueRef groupFunction = groupData.Function;
            CurrentState = new AsyncStateGroupCompilerState(groupFunction, new IRBuilder());

            Builder.PositionBuilderAtEnd(groupData.InitialBasicBlock);

            // Here we are assuming that the group whose label matches the function name is also the entry group.
            if (asyncStateGroup.FunctionId == asyncStateGroup.Label)
            {
                _allocationSet.InitializeFunctionLocalAllocations(asyncStateGroup.FunctionId, Builder);
            }

            var conditionalContinuation = asyncStateGroup.Continuation as ConditionallyScheduleGroupsContinuation;
            if (conditionalContinuation != null)
            {
                if (conditionalContinuation.SuccessorConditionGroups.Count != 2)
                {
                    throw new NotSupportedException("Only boolean conditions supported for continuations");
                }
                groupData.ContinuationConditionVariable = Builder.CreateAlloca(LLVMTypeRef.Int1Type(), "continuationStatePtr");
            }

            groupData.CreateFireCountReset(Builder);

            foreach (Visitation visitation in asyncStateGroup.Visitations)
            {
                visitation.Visit(this);
            }

            bool returnAfterGroup = true;
            var unconditionalContinuation = asyncStateGroup.Continuation as UnconditionallySchduleGroupsContinuation;
            if (unconditionalContinuation != null)
            {
                if (unconditionalContinuation.Successors.Any())
                {
                    AsyncStateGroup singleSuccessor;
                    if (unconditionalContinuation.Successors.TryGetSingleElement(out singleSuccessor) && singleSuccessor.FunctionId == asyncStateGroup.FunctionId)
                    {
                        Builder.CreateBr(AsyncStateGroups[singleSuccessor].InitialBasicBlock);
                        returnAfterGroup = false;
                    }
                    else
                    {
                        CreateInvokeOrScheduleOfSuccessors(unconditionalContinuation.Successors);
                    }
                }
                else
                {
                    GenerateFunctionTerminator();
                }
            }
            if (conditionalContinuation != null)
            {
                LLVMValueRef condition = Builder.CreateLoad(groupData.ContinuationConditionVariable, "condition");

                AsyncStateGroup singleTrueSuccessor, singleFalseSuccessor;
                if (conditionalContinuation.SuccessorConditionGroups[0].TryGetSingleElement(out singleFalseSuccessor)
                    && conditionalContinuation.SuccessorConditionGroups[1].TryGetSingleElement(out singleTrueSuccessor)
                    && singleFalseSuccessor.FunctionId == asyncStateGroup.FunctionId
                    && singleTrueSuccessor.FunctionId == asyncStateGroup.FunctionId)
                {
                    Builder.CreateCondBr(condition, AsyncStateGroups[singleTrueSuccessor].InitialBasicBlock, AsyncStateGroups[singleFalseSuccessor].InitialBasicBlock);
                    returnAfterGroup = false;
                }
                else
                {
                    LLVMBasicBlockRef continuationConditionFalseBlock = groupFunction.AppendBasicBlock("continuationConditionFalse"),
                        continuationConditionTrueBlock = groupFunction.AppendBasicBlock("continuationConditionTrue"),
                        exitBlock = groupFunction.AppendBasicBlock("exit");
                    Builder.CreateCondBr(condition, continuationConditionTrueBlock, continuationConditionFalseBlock);

                    Builder.PositionBuilderAtEnd(continuationConditionFalseBlock);
                    CreateInvokeOrScheduleOfSuccessors(conditionalContinuation.SuccessorConditionGroups[0]);
                    Builder.CreateBr(exitBlock);

                    Builder.PositionBuilderAtEnd(continuationConditionTrueBlock);
                    CreateInvokeOrScheduleOfSuccessors(conditionalContinuation.SuccessorConditionGroups[1]);
                    Builder.CreateBr(exitBlock);

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
                CreateScheduleCallsForAsyncStateGroups(CurrentState.StatePointer, successors);
            }
        }

        private void CreateScheduleCallsForAsyncStateGroups(LLVMValueRef statePointer, IEnumerable<AsyncStateGroup> asyncStateGroups)
        {
            LLVMTypeRef scheduledTaskType = LLVMExtensions.GetScheduledTaskType(Module);
            LLVMValueRef bitCastStatePtr = Builder.CreateBitCast(statePointer, LLVMExtensions.VoidPointerType, "bitCastStatePtr");
            foreach (AsyncStateGroup successor in asyncStateGroups)
            {
                AsyncStateGroupData successorData = AsyncStateGroups[successor];
                LLVMValueRef bitCastSuccessorFunction = Builder.CreateBitCast(
                    successorData.Function,
                    LLVMTypeRef.PointerType(LLVMExtensions.ScheduledTaskFunctionType, 0u),
                    "bitCastFunction");
                if (!successor.SignaledConditionally && successor.Predecessors.HasMoreThan(1))
                {
                    LLVMValueRef fireCountPtr = GetAddress(successorData.FireCountStateField, Builder);
                    Builder.CreateCall(
                        GetImportedCommonFunction(CommonModules.PartialScheduleName),
                        new LLVMValueRef[] { bitCastStatePtr, fireCountPtr, bitCastSuccessorFunction },
                        string.Empty);
                }
                else
                {
                    Builder.CreateCall(
                        _commonExternalFunctions.ScheduleFunction,
                        new LLVMValueRef[] { bitCastSuccessorFunction, bitCastStatePtr },
                        string.Empty);
                }
            }
        }

        private void GenerateFunctionTerminator()
        {
            LLVMValueRef donePtr = _allocationSet.GetStateDonePointer(Builder);
            Builder.CreateStore(true.AsLLVMValue(), donePtr);

            LLVMValueRef callerWakerPtr = _allocationSet.GetStateCallerWakerPointer(Builder),
                callerWaker = Builder.CreateLoad(callerWakerPtr, "callerWaker");

            // activate the caller waker
            // TODO: invoke caller waker directly, or schedule?
            Builder.CreateCall(GetImportedCommonFunction(CommonModules.InvokeName), new LLVMValueRef[] { callerWaker }, string.Empty);
        }

        private static string GetAllocateStateFunctionName(string runtimeFunctionName)
        {
            return $"{runtimeFunctionName}::AllocateState";
        }

        private static string GetInitializeStateFunctionName(string runtimeFunctionName)
        {
            return $"{runtimeFunctionName}::InitializeState";
        }

        private static string GetPollFunctionName(string runtimeFunctionName)
        {
            return $"{runtimeFunctionName}::Poll";
        }

        private LLVMValueRef BuildAllocateStateFunction()
        {
            LLVMTypeRef allocateStateFunctionType = LLVMTypeRef.FunctionType(
                LLVMExtensions.VoidPointerType,
                new LLVMTypeRef[0],
                false);
            LLVMValueRef allocateStateFunction = Module.AddFunction(GetAllocateStateFunctionName(_functionName), allocateStateFunctionType);
            var builder = new IRBuilder();
            LLVMBasicBlockRef entryBlock = allocateStateFunction.AppendBasicBlock("entry");

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef statePtr = builder.CreateMalloc(_allocationSet.StateType, "statePtr"),
                voidStatePtr = builder.CreateBitCast(statePtr, LLVMExtensions.VoidPointerType, "voidStatePtr");
            builder.CreateRet(voidStatePtr);
            return allocateStateFunction;
        }

        private LLVMValueRef BuildInitializeStateFunction(IEnumerable<LLVMTypeRef> parameterTypes)
        {
            List<LLVMTypeRef> initializeStateFunctionParameterTypes = new List<LLVMTypeRef>() { LLVMExtensions.VoidPointerType };
            initializeStateFunctionParameterTypes.AddRange(parameterTypes);
            LLVMTypeRef initializeStateFunctionType = LLVMTypeRef.FunctionType(
                LLVMTypeRef.VoidType(),
                initializeStateFunctionParameterTypes.ToArray(),
                false);
            LLVMValueRef initializeStateFunction = Module.AddFunction(GetInitializeStateFunctionName(_functionName), initializeStateFunctionType);
            var builder = new IRBuilder();
            LLVMBasicBlockRef entryBlock = initializeStateFunction.AppendBasicBlock("entry");

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef voidStatePtr = initializeStateFunction.GetParam(0u);
            LLVMValueRef statePtr = builder.CreateBitCast(voidStatePtr, LLVMTypeRef.PointerType(_allocationSet.StateType, 0u), "statePtr");
            builder.CreateStore(false.AsLLVMValue(), builder.CreateStructGEP(statePtr, 0u, "donePtr"));

            uint parameterStructFieldIndex = FunctionAllocationSet.FirstParameterFieldIndex;
            uint parameterIndex = 1u;
            foreach (var dataItem in _parameterDataItems.OrderBy(d => d.ConnectorPaneIndex))
            {
                builder.CreateStore(
                    initializeStateFunction.GetParam(parameterIndex),
                    builder.CreateStructGEP(statePtr, parameterStructFieldIndex, string.Empty));
                ++parameterStructFieldIndex;
                ++parameterIndex;
            }
            builder.CreateRetVoid();

            return initializeStateFunction;
        }

        private LLVMValueRef BuildPollFunction()
        {
            LLVMValueRef pollFunction = Module.AddFunction(GetPollFunctionName(_functionName), LLVMExtensions.ScheduledTaskFunctionType);
            LLVMBasicBlockRef entryBlock = pollFunction.AppendBasicBlock("entry");
            var builder = new IRBuilder();

            var outerFunctionCompilerState = new OuterFunctionCompilerState(pollFunction, builder);
            CurrentState = outerFunctionCompilerState;
            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef stateVoidPtr = pollFunction.GetParam(0u),
                statePtr = builder.CreateBitCast(stateVoidPtr, LLVMTypeRef.PointerType(_allocationSet.StateType, 0u), "statePtr");
            outerFunctionCompilerState.StateMalloc = statePtr;

            // set initial fire counts
            foreach (AsyncStateGroupData groupData in AsyncStateGroups.Values)
            {
                groupData.CreateFireCountReset(Builder);
            }

            // schedule initial group(s)
            CreateScheduleCallsForAsyncStateGroups(
                statePtr,
                AsyncStateGroups.Keys.Where(group => !group.Predecessors.Any()));

            Builder.CreateRetVoid();
            return pollFunction;
        }

        private void BuildOuterFunction(List<LLVMTypeRef> parameterTypes, LLVMValueRef allocateStateFunction, LLVMValueRef initializeStateFunction, LLVMValueRef pollFunction)
        {
            var outerFunctionParameters = new List<LLVMTypeRef>();
            outerFunctionParameters.Add(LLVMTypeRef.PointerType(LLVMExtensions.ScheduledTaskFunctionType, 0u));
            outerFunctionParameters.Add(LLVMExtensions.VoidPointerType);
            outerFunctionParameters.AddRange(parameterTypes);

            LLVMTypeRef outerFunctionType = LLVMSharp.LLVM.FunctionType(LLVMSharp.LLVM.VoidType(), outerFunctionParameters.ToArray(), false);
            LLVMValueRef outerFunction = Module.AddFunction(_functionName, outerFunctionType);
            var outerFunctionCompilerState = new OuterFunctionCompilerState(outerFunction, new IRBuilder());
            CurrentState = outerFunctionCompilerState;
            LLVMBasicBlockRef outerEntryBlock = outerFunction.AppendBasicBlock("entry");
            Builder.PositionBuilderAtEnd(outerEntryBlock);

            // TODO: deallocate state block for the top-level case!
            LLVMValueRef voidStatePtr = Builder.CreateCall(
                allocateStateFunction,
                new LLVMValueRef[0],
                "statePtr");
            var initializeArguments = new List<LLVMValueRef>() { voidStatePtr };
            initializeArguments.AddRange(outerFunction.GetParams().Skip(2));
            Builder.CreateCall(
                initializeStateFunction,
                initializeArguments.ToArray(),
                string.Empty);
            LLVMValueRef statePtr = Builder.CreateBitCast(voidStatePtr, LLVMTypeRef.PointerType(_allocationSet.StateType, 0u), "statePtr");
            outerFunctionCompilerState.StateMalloc = statePtr;
            // TODO: create constants for these positions
            LLVMValueRef waker = Builder.BuildStructValue(
                LLVMExtensions.WakerType,
                new LLVMValueRef[] { outerFunction.GetParam(0u), outerFunction.GetParam(1u) },
                "callerWaker");
            Builder.CreateStore(waker, Builder.CreateStructGEP(statePtr, 1u, "callerWakerPtr"));

            Builder.CreateCall(
                pollFunction,
                new LLVMValueRef[] { voidStatePtr },
                string.Empty);
            Builder.CreateRetVoid();
        }
    }
}
