using System;
using System.Collections.Generic;
using System.Linq;
using LLVMSharp;
using NationalInstruments;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

namespace Rebar.RebarTarget.LLVM
{
    internal abstract class FunctionModuleBuilder
    {
        protected FunctionModuleBuilder(Module module, FunctionCompilerSharedData sharedData)
        {
            Module = module;
            SharedData = sharedData;
            AsyncStateGroups = new Dictionary<AsyncStateGroup, AsyncStateGroupData>();
        }

        public Module Module { get; }

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
                        yield return parameterType.AsLLVMType();
                        break;
                    case Direction.Output:
                        yield return LLVMTypeRef.PointerType(parameterType.AsLLVMType(), 0u);
                        break;
                    default:
                        throw new NotImplementedException("Can only handle in and out parameters");
                }
            }
        }

        protected LLVMValueRef InitializeOuterFunction(string functionName, IEnumerable<LLVMTypeRef> parameterTypes)
        {
            var outerFunctionParameters = new List<LLVMTypeRef>();
            outerFunctionParameters.Add(LLVMTypeRef.PointerType(LLVMExtensions.ScheduledTaskFunctionType, 0u));
            outerFunctionParameters.Add(LLVMExtensions.VoidPointerType);
            outerFunctionParameters.AddRange(parameterTypes);

            LLVMTypeRef outerFunctionType = LLVMSharp.LLVM.FunctionType(LLVMSharp.LLVM.VoidType(), outerFunctionParameters.ToArray(), false);
            return Module.AddFunction(functionName, outerFunctionType);
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
                if (conditionalContinuation.SuccessorConditionGroups.Count != 2)
                {
                    throw new NotSupportedException("Only boolean conditions supported for continuations");
                }
                groupData.ContinuationConditionVariable = Builder.CreateAlloca(LLVMTypeRef.Int1Type(), "continuationStatePtr");
            }

            groupData.CreateFireCountReset(Builder);

            foreach (Visitation visitation in asyncStateGroup.Visitations)
            {
                visitation.Visit(SharedData.VisitationHandler);
            }

            Builder.CreateBr(groupData.SkipBasicBlock);
            Builder.PositionBuilderAtEnd(groupData.SkipBasicBlock);

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
                else if (this is AsynchronousFunctionModuleBuilder)
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
                    Builder.CreateCondBr(
                        condition,
                        AsyncStateGroups[singleTrueSuccessor].InitialBasicBlock,
                        AsyncStateGroups[singleFalseSuccessor].InitialBasicBlock);
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
                CreateScheduleCallsForAsyncStateGroups(Builder, CurrentState.StatePointer, successors);
            }
        }

        protected void CreateScheduleCallsForAsyncStateGroups(IRBuilder builder, LLVMValueRef statePointer, IEnumerable<AsyncStateGroup> asyncStateGroups)
        {
            LLVMTypeRef scheduledTaskType = LLVMExtensions.GetScheduledTaskType(Module);
            LLVMValueRef bitCastStatePtr = builder.CreateBitCast(statePointer, LLVMExtensions.VoidPointerType, "bitCastStatePtr");
            foreach (AsyncStateGroup successor in asyncStateGroups)
            {
                AsyncStateGroupData successorData = AsyncStateGroups[successor];
                LLVMValueRef bitCastSuccessorFunction = builder.CreateBitCast(
                    successorData.Function,
                    LLVMTypeRef.PointerType(LLVMExtensions.ScheduledTaskFunctionType, 0u),
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

        private void GenerateFunctionTerminator()
        {
            LLVMValueRef donePtr = AllocationSet.GetStateDonePointer(Builder);
            Builder.CreateStore(((byte)1).AsLLVMValue(), donePtr);

            LLVMValueRef callerWakerPtr = AllocationSet.GetStateCallerWakerPointer(Builder),
                callerWaker = Builder.CreateLoad(callerWakerPtr, "callerWaker");

            // activate the caller waker
            // TODO: invoke caller waker directly, or schedule?
            Builder.CreateCall(SharedData.FunctionImporter.GetImportedCommonFunction(CommonModules.InvokeName), new LLVMValueRef[] { callerWaker }, string.Empty);
        }
    }
}
