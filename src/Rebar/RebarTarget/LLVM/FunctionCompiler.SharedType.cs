﻿using System.Linq;
using LLVMSharp;
using NationalInstruments;
using NationalInstruments.DataTypes;
using Rebar.Common;

namespace Rebar.RebarTarget.LLVM
{
    internal partial class FunctionCompiler
    {
        internal static void BuildSharedCreateFunction(FunctionModuleContext moduleContext, NIType signature, LLVMValueRef sharedCreateFunction)
        {
            LLVMTypeRef valueType = moduleContext.LLVMContext.AsLLVMType(signature.GetGenericParameters().First());

            LLVMBasicBlockRef entryBlock = sharedCreateFunction.AppendBasicBlock("entry");
            var builder = moduleContext.LLVMContext.CreateIRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMTypeRef refCountType = moduleContext.LLVMContext.CreateLLVMRefCountType(valueType);
            LLVMValueRef refCountAllocationPtr = moduleContext.CreateMalloc(builder, refCountType, "refCountAllocationPtr"),
                refCount = builder.BuildStructValue(
                    refCountType,
                    new LLVMValueRef[] { moduleContext.LLVMContext.AsLLVMValue(1), sharedCreateFunction.GetParam(0u) },
                    "refCount");
            builder.CreateStore(refCount, refCountAllocationPtr);
            LLVMValueRef sharedPtr = sharedCreateFunction.GetParam(1u);
            builder.CreateStore(refCountAllocationPtr, sharedPtr);
            builder.CreateRetVoid();
        }

        internal static void BuildSharedGetValueFunction(FunctionModuleContext functionCompiler, NIType signature, LLVMValueRef sharedGetValueFunction)
        {
            LLVMBasicBlockRef entryBlock = sharedGetValueFunction.AppendBasicBlock("entry");
            var builder = functionCompiler.LLVMContext.CreateIRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef shared = builder.CreateLoad(sharedGetValueFunction.GetParam(0u), "shared"),
                valuePtr = builder.CreateStructGEP(shared, 1u, "valuePtr"),
                valuePtrPtr = sharedGetValueFunction.GetParam(1u);
            builder.CreateStore(valuePtr, valuePtrPtr);
            builder.CreateRetVoid();
        }

        internal static void BuildSharedCloneFunction(FunctionModuleContext functionCompiler, NIType signature, LLVMValueRef sharedCloneFunction)
        {
            LLVMBasicBlockRef entryBlock = sharedCloneFunction.AppendBasicBlock("entry");
            var builder = functionCompiler.LLVMContext.CreateIRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef shared = builder.CreateLoad(sharedCloneFunction.GetParam(0u), "shared"),
                referenceCountPtr = builder.CreateStructGEP(shared, 0u, "referenceCountPtr");
            // TODO: ideally this should handle integer overflow
            builder.CreateAtomicRMW(
                LLVMAtomicRMWBinOp.LLVMAtomicRMWBinOpAdd,
                referenceCountPtr,
                functionCompiler.LLVMContext.AsLLVMValue(1),
                // Since the increment to the reference count does not affect the store we're performing afterwards,
                // we only need monotonic ordering.
                // See the documentation about atomic orderings here: https://llvm.org/docs/LangRef.html#atomic-memory-ordering-constraints
                LLVMAtomicOrdering.LLVMAtomicOrderingMonotonic,
                false);
            LLVMValueRef sharedClonePtr = sharedCloneFunction.GetParam(1u);
            builder.CreateStore(shared, sharedClonePtr);
            builder.CreateRetVoid();
        }

        internal static void BuildSharedDropFunction(FunctionModuleContext moduleContext, NIType signature, LLVMValueRef sharedDropFunction)
        {
            NIType valueType;
            signature.GetGenericParameters().First().TryDestructureSharedType(out valueType);

            LLVMBasicBlockRef entryBlock = sharedDropFunction.AppendBasicBlock("entry");
            var builder = moduleContext.LLVMContext.CreateIRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef decrementRefCountFunction = GetDecrementRefCountFunction(moduleContext, valueType),
                shared = builder.CreateLoad(sharedDropFunction.GetParam(0u), "shared");
            builder.CreateCall(decrementRefCountFunction, new LLVMValueRef[] { shared }, string.Empty);
            builder.CreateRetVoid();
        }

        private static LLVMValueRef GetDecrementRefCountFunction(FunctionModuleContext moduleContext, NIType valueType)
        {
            string specializedName = FunctionNames.MonomorphizeFunctionName("decrementRefCount", valueType.ToEnumerable());
            return moduleContext.FunctionImporter.GetCachedFunction(
                specializedName,
                () => BuildDecrementRefCountFunction(moduleContext, specializedName, valueType));
        }

        private static LLVMValueRef BuildDecrementRefCountFunction(FunctionModuleContext moduleContext, string functionName, NIType valueType)
        {
            LLVMTypeRef functionType = LLVMTypeRef.FunctionType(
                moduleContext.LLVMContext.VoidType,
                new LLVMTypeRef[]
                {
                    LLVMTypeRef.PointerType(moduleContext.LLVMContext.CreateLLVMRefCountType(moduleContext.LLVMContext.AsLLVMType(valueType)), 0u)
                },
                false);
            LLVMValueRef decrementRefCountFunction = moduleContext.Module.AddFunction(functionName, functionType);
            LLVMBasicBlockRef entryBlock = decrementRefCountFunction.AppendBasicBlock("entry"),
                noRefsRemainingBlock = decrementRefCountFunction.AppendBasicBlock("noRefsRemaining"),
                endBlock = decrementRefCountFunction.AppendBasicBlock("end");
            var builder = moduleContext.LLVMContext.CreateIRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef refCountObjectPtr = decrementRefCountFunction.GetParam(0u),
                referenceCountPtr = builder.CreateStructGEP(refCountObjectPtr, 0u, "referenceCountPtr"),
                one = moduleContext.LLVMContext.AsLLVMValue(1),
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
            moduleContext.CreateDropCallIfDropFunctionExists(builder, valueType, b => b.CreateStructGEP(refCountObjectPtr, 1u, "valuePtr"));
            builder.CreateFree(refCountObjectPtr);
            builder.CreateBr(endBlock);

            builder.PositionBuilderAtEnd(endBlock);
            builder.CreateRetVoid();
            return decrementRefCountFunction;
        }
    }
}
