using System.Linq;
using LLVMSharp;
using NationalInstruments;
using NationalInstruments.DataTypes;
using Rebar.Common;
using Rebar.Compiler.Nodes;

namespace Rebar.RebarTarget.LLVM
{
    internal partial class FunctionCompiler
    {
        private static void BuildSharedCreateFunction(FunctionCompiler functionCompiler, NIType signature, LLVMValueRef sharedCreateFunction)
        {
            LLVMTypeRef valueType = signature.GetGenericParameters().First().AsLLVMType();

            LLVMBasicBlockRef entryBlock = sharedCreateFunction.AppendBasicBlock("entry");
            var builder = new IRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMTypeRef refCountType = valueType.CreateLLVMRefCountType();
            LLVMValueRef refCountAllocationPtr = builder.CreateMalloc(refCountType, "refCountAllocationPtr"),
                refCount = builder.BuildStructValue(
                    refCountType,
                    new LLVMValueRef[] { 1.AsLLVMValue(), sharedCreateFunction.GetParam(0u) },
                    "refCount");
            builder.CreateStore(refCount, refCountAllocationPtr);
            LLVMValueRef sharedPtr = sharedCreateFunction.GetParam(1u);
            builder.CreateStore(refCountAllocationPtr, sharedPtr);
            builder.CreateRetVoid();
        }

        private static void BuildSharedGetValueFunction(FunctionCompiler functionCompiler, NIType signature, LLVMValueRef sharedGetValueFunction)
        {
            LLVMBasicBlockRef entryBlock = sharedGetValueFunction.AppendBasicBlock("entry");
            var builder = new IRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef shared = builder.CreateLoad(sharedGetValueFunction.GetParam(0u), "shared"),
                valuePtr = builder.CreateStructGEP(shared, 1u, "valuePtr"),
                valuePtrPtr = sharedGetValueFunction.GetParam(1u);
            builder.CreateStore(valuePtr, valuePtrPtr);
            builder.CreateRetVoid();
        }

        private static void BuildSharedCloneFunction(FunctionCompiler functionCompiler, NIType signature, LLVMValueRef sharedCloneFunction)
        {
            LLVMBasicBlockRef entryBlock = sharedCloneFunction.AppendBasicBlock("entry");
            var builder = new IRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef shared = builder.CreateLoad(sharedCloneFunction.GetParam(0u), "shared"),
                referenceCountPtr = builder.CreateStructGEP(shared, 0u, "referenceCountPtr");
            // TODO: ideally this should handle integer overflow
            builder.CreateAtomicRMW(
                LLVMAtomicRMWBinOp.LLVMAtomicRMWBinOpAdd,
                referenceCountPtr,
                1.AsLLVMValue(),
                // Since the increment to the reference count does not affect the store we're performing afterwards,
                // we only need monotonic ordering.
                // See the documentation about atomic orderings here: https://llvm.org/docs/LangRef.html#atomic-memory-ordering-constraints
                LLVMAtomicOrdering.LLVMAtomicOrderingMonotonic,
                false);
            LLVMValueRef sharedClonePtr = sharedCloneFunction.GetParam(1u);
            builder.CreateStore(shared, sharedClonePtr);
            builder.CreateRetVoid();
        }

        private static void BuildSharedDropFunction(FunctionCompiler compiler, NIType signature, LLVMValueRef sharedDropFunction)
        {
            NIType valueType;
            signature.GetGenericParameters().First().TryDestructureSharedType(out valueType);

            LLVMBasicBlockRef entryBlock = sharedDropFunction.AppendBasicBlock("entry"),
                noRefsRemainingBlock = sharedDropFunction.AppendBasicBlock("noRefsRemaining"),
                endBlock = sharedDropFunction.AppendBasicBlock("end");
            var builder = new IRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef shared = builder.CreateLoad(sharedDropFunction.GetParam(0u), "shared"),
                referenceCountPtr = builder.CreateStructGEP(shared, 0u, "referenceCountPtr"),
                one = 1.AsLLVMValue(),
                previousReferenceCount = builder.CreateAtomicRMW(
                    LLVMAtomicRMWBinOp.LLVMAtomicRMWBinOpSub,
                    referenceCountPtr,
                    one,
                    // Since the decrement to the reference count does not have any effect until after we branch,
                    // we only need monotonic ordering here.
                    // See the documentation about atomic orderings here: https://llvm.org/docs/LangRef.html#atomic-memory-ordering-constraints
                    LLVMAtomicOrdering.LLVMAtomicOrderingMonotonic,
                    false),
                noRefsRemaining = builder.CreateICmp(LLVMIntPredicate.LLVMIntEQ, previousReferenceCount, one, "noRefsRemaining");
            builder.CreateCondBr(noRefsRemaining, noRefsRemainingBlock, endBlock);

            builder.PositionBuilderAtEnd(noRefsRemainingBlock);
            compiler.CreateDropCallIfDropFunctionExists(builder, valueType, b => b.CreateStructGEP(shared, 1u, "valuePtr"));
            builder.CreateFree(shared);
            builder.CreateBr(endBlock);

            builder.PositionBuilderAtEnd(endBlock);
            builder.CreateRetVoid();
        }
    }
}
