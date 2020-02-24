using System;
using LLVMSharp;

namespace Rebar.RebarTarget.LLVM
{
    internal class AsyncStateGroupData
    {
        public AsyncStateGroupData(
            AsyncStateGroup asyncStateGroup,
            LLVMValueRef function,
            LLVMBasicBlockRef initialBasicBlock,
            LLVMBasicBlockRef skipBasicBlock,
            StateFieldValueSource fireCountStateField)
        {
            AsyncStateGroup = asyncStateGroup;
            Function = function;
            InitialBasicBlock = initialBasicBlock;
            SkipBasicBlock = skipBasicBlock;
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
        /// The basic block to skip to if the block needs to be skipped due to a panic.
        /// </summary>
        public LLVMBasicBlockRef SkipBasicBlock { get; }

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
}
