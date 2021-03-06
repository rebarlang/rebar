﻿using System;
using LLVMSharp;

namespace Rebar.RebarTarget.LLVM
{
    internal class AsyncStateGroupData
    {
        public AsyncStateGroupData(
            AsyncStateGroup asyncStateGroup,
            ContextWrapper context,
            LLVMValueRef function,
            LLVMBasicBlockRef initialBasicBlock,
            LLVMBasicBlockRef continueBasicBlock,
            LLVMBasicBlockRef exitBasicBlock,
            StateFieldValueSource fireCountStateField)
        {
            AsyncStateGroup = asyncStateGroup;
            Context = context;
            Function = function;
            InitialBasicBlock = initialBasicBlock;
            ContinueBasicBlock = continueBasicBlock;
            ExitBasicBlock = exitBasicBlock;
            FireCountStateField = fireCountStateField;
        }

        /// <summary>
        /// The associated <see cref="AsyncStateGroup"/>.
        /// </summary>
        public AsyncStateGroup AsyncStateGroup { get; }

        private ContextWrapper Context { get; }

        /// <summary>
        /// The LLVM function into which the <see cref="AsyncStateGroup"/> will be generated.
        /// </summary>
        public LLVMValueRef Function { get; }

        /// <summary>
        /// The initial basic block created for the <see cref="AsyncStateGroup"/>.
        /// </summary>
        public LLVMBasicBlockRef InitialBasicBlock { get; }

        /// <summary>
        /// The basic block in which the interior of the <see cref="AsyncStateGroup"/> will begin running,
        /// assuming there isn't a panic.
        /// </summary>
        public LLVMBasicBlockRef ContinueBasicBlock { get; }

        /// <summary>
        /// The basic block to skip to if the block needs to be skipped due to a panic.
        /// </summary>
        public LLVMBasicBlockRef ExitBasicBlock { get; }

        /// <summary>
        /// If the <see cref="AsyncStateGroup"/> needs to be scheduled by multiple predecessors, this
        /// contains a <see cref="StateFieldValueSource"/> for the group's fire count.
        /// </summary>
        public StateFieldValueSource FireCountStateField { get; }

        /// <summary>
        /// Generates code that will reset the fire count variable for this <see cref="AsyncStateGroup"/>
        /// in the state block to its maximum value.
        /// </summary>
        /// <param name="builder"></param>
        public void CreateFireCountReset(IRBuilder builder)
        {
            if (FireCountStateField != null)
            {
                ((IUpdateableValueSource)FireCountStateField).UpdateValue(builder, Context.AsLLVMValue(AsyncStateGroup.MaxFireCount));
            }
        }
    }
}
