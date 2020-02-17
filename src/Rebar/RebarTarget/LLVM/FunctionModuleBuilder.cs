using System;
using System.Collections.Generic;
using System.Linq;
using LLVMSharp;
using NationalInstruments;

namespace Rebar.RebarTarget.LLVM
{
    internal abstract class FunctionModuleBuilder
    {
        protected FunctionModuleBuilder(Module module, FunctionCompiler functionCompiler)
        {
            Module = module;
            FunctionCompiler = functionCompiler;
            AsyncStateGroups = new Dictionary<AsyncStateGroup, AsyncStateGroupData>();
        }

        public Module Module { get; }

        protected FunctionCompiler FunctionCompiler { get; }

        internal Dictionary<AsyncStateGroup, AsyncStateGroupData> AsyncStateGroups { get; }

        internal AsyncStateGroupData CurrentGroupData => AsyncStateGroups[CurrentGroup];

        protected AsyncStateGroup CurrentGroup { get; set; }

        public abstract void CompileFunction();

        protected LLVMValueRef InitializeOuterFunction(string functionName, List<LLVMTypeRef> parameterTypes)
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
            FunctionCompilerState previousState = FunctionCompiler.CurrentState;
            CurrentGroup = asyncStateGroup;
            FunctionCompiler.CurrentState = compilerState;
            AsyncStateGroupData groupData = AsyncStateGroups[asyncStateGroup];
            LLVMValueRef groupFunction = groupData.Function;

            FunctionCompiler.Builder.PositionBuilderAtEnd(groupData.InitialBasicBlock);

            // Here we are assuming that the group whose label matches the function name is also the entry group.
            if (asyncStateGroup.FunctionId == asyncStateGroup.Label && this is AsynchronousFunctionModuleBuilder)
            {
                FunctionCompiler.AllocationSet.InitializeFunctionLocalAllocations(asyncStateGroup.FunctionId, FunctionCompiler.Builder);
            }

            var conditionalContinuation = asyncStateGroup.Continuation as ConditionallyScheduleGroupsContinuation;
            if (conditionalContinuation != null)
            {
                if (conditionalContinuation.SuccessorConditionGroups.Count != 2)
                {
                    throw new NotSupportedException("Only boolean conditions supported for continuations");
                }
                groupData.ContinuationConditionVariable = FunctionCompiler.Builder.CreateAlloca(LLVMTypeRef.Int1Type(), "continuationStatePtr");
            }

            groupData.CreateFireCountReset(FunctionCompiler.Builder);

            foreach (Visitation visitation in asyncStateGroup.Visitations)
            {
                visitation.Visit(FunctionCompiler);
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
                        FunctionCompiler.Builder.CreateBr(AsyncStateGroups[singleSuccessor].InitialBasicBlock);
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
                LLVMValueRef condition = FunctionCompiler.Builder.CreateLoad(groupData.ContinuationConditionVariable, "condition");

                AsyncStateGroup singleTrueSuccessor, singleFalseSuccessor;
                if (conditionalContinuation.SuccessorConditionGroups[0].TryGetSingleElement(out singleFalseSuccessor)
                    && conditionalContinuation.SuccessorConditionGroups[1].TryGetSingleElement(out singleTrueSuccessor)
                    && singleFalseSuccessor.FunctionId == asyncStateGroup.FunctionId
                    && singleTrueSuccessor.FunctionId == asyncStateGroup.FunctionId)
                {
                    FunctionCompiler.Builder.CreateCondBr(
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
                    FunctionCompiler.Builder.CreateCondBr(condition, continuationConditionTrueBlock, continuationConditionFalseBlock);

                    FunctionCompiler.Builder.PositionBuilderAtEnd(continuationConditionFalseBlock);
                    CreateInvokeOrScheduleOfSuccessors(conditionalContinuation.SuccessorConditionGroups[0]);
                    FunctionCompiler.Builder.CreateBr(exitBlock);

                    FunctionCompiler.Builder.PositionBuilderAtEnd(continuationConditionTrueBlock);
                    CreateInvokeOrScheduleOfSuccessors(conditionalContinuation.SuccessorConditionGroups[1]);
                    FunctionCompiler.Builder.CreateBr(exitBlock);

                    FunctionCompiler.Builder.PositionBuilderAtEnd(exitBlock);
                }
            }

            if (returnAfterGroup)
            {
                FunctionCompiler.Builder.CreateRetVoid();
            }

            CurrentGroup = null;
            FunctionCompiler.CurrentState = previousState;
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
                FunctionCompiler.Builder.CreateCall(AsyncStateGroups[singleSuccessor].Function, new LLVMValueRef[] { FunctionCompiler.CurrentState.StatePointer }, string.Empty);
            }
            else
            {
                CreateScheduleCallsForAsyncStateGroups(FunctionCompiler.Builder, FunctionCompiler.CurrentState.StatePointer, successors);
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
                    LLVMValueRef fireCountPtr = FunctionCompiler.GetAddress(successorData.FireCountStateField, builder);
                    builder.CreateCall(
                        FunctionCompiler.GetImportedCommonFunction(CommonModules.PartialScheduleName),
                        new LLVMValueRef[] { bitCastStatePtr, fireCountPtr, bitCastSuccessorFunction },
                        string.Empty);
                }
                else
                {
                    builder.CreateCall(
                        FunctionCompiler.CommonExternalFunctions.ScheduleFunction,
                        new LLVMValueRef[] { bitCastSuccessorFunction, bitCastStatePtr },
                        string.Empty);
                }
            }
        }

        private void GenerateFunctionTerminator()
        {
            LLVMValueRef donePtr = FunctionCompiler.AllocationSet.GetStateDonePointer(FunctionCompiler.Builder);
            FunctionCompiler.Builder.CreateStore(true.AsLLVMValue(), donePtr);

            LLVMValueRef callerWakerPtr = FunctionCompiler.AllocationSet.GetStateCallerWakerPointer(FunctionCompiler.Builder),
                callerWaker = FunctionCompiler.Builder.CreateLoad(callerWakerPtr, "callerWaker");

            // activate the caller waker
            // TODO: invoke caller waker directly, or schedule?
            FunctionCompiler.Builder.CreateCall(FunctionCompiler.GetImportedCommonFunction(CommonModules.InvokeName), new LLVMValueRef[] { callerWaker }, string.Empty);
        }
    }
}
