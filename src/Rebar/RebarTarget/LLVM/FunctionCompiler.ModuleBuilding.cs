using System;
using System.Collections.Generic;
using System.Linq;
using LLVMSharp;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Compiler;

namespace Rebar.RebarTarget.LLVM
{
    internal partial class FunctionCompiler
    {
        internal class AsyncStateGroupData
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

        internal Dictionary<AsyncStateGroup, AsyncStateGroupData> AsyncStateGroups { get; }

        public void CompileFunction(DfirRoot dfirRoot)
        {
            TargetDfir = dfirRoot;
            _moduleBuilder.CompileFunction();
        }

        internal List<LLVMTypeRef> GetParameterLLVMTypes()
        {
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
            return parameterTypes;
        }

        internal static string GetSynchronousFunctionName(string functionName)
        {
            return $"{functionName}::sync";
        }

        internal static string GetInitializeStateFunctionName(string runtimeFunctionName)
        {
            return $"{runtimeFunctionName}::InitializeState";
        }

        internal static string GetPollFunctionName(string runtimeFunctionName)
        {
            return $"{runtimeFunctionName}::Poll";
        }

        internal void InitializeParameterAllocations(LLVMValueRef function, IRBuilder builder)
        {
            uint parameterIndex = 0u;
            foreach (var dataItem in _parameterDataItems.OrderBy(d => d.ConnectorPaneIndex))
            {
                LLVMValueRef parameterAllocationPtr = GetAddress(_variableValues[dataItem.GetVariable()], builder);
                builder.CreateStore(function.GetParam(parameterIndex), parameterAllocationPtr);
                ++parameterIndex;
            }
        }
    }
}
