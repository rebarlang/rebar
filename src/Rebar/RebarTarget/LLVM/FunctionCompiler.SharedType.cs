using LLVMSharp;
using NationalInstruments;
using NationalInstruments.DataTypes;
using Rebar.Common;
using Rebar.Compiler.Nodes;

namespace Rebar.RebarTarget.LLVM
{
    internal partial class FunctionCompiler
    {
        private static void CompileSharedCreate(FunctionCompiler compiler, FunctionalNode sharedCreateNode)
        {
            var valueSource = (LocalAllocationValueSource)compiler.GetTerminalValueSource(sharedCreateNode.InputTerminals[0]);
            NIType elementType = valueSource.AllocationNIType;
            string specializedName = MonomorphizeFunctionName("shared_create", elementType.ToEnumerable());
            LLVMValueRef sharedCreateFunction = compiler.GetSpecializedFunction(
                specializedName,
                () => CreateSharedCreateFunction(compiler.Module, specializedName, elementType.AsLLVMType()));
            compiler.CreateCallForFunctionalNode(sharedCreateFunction, sharedCreateNode);
        }

        private static LLVMValueRef CreateSharedCreateFunction(Module module, string functionName, LLVMTypeRef valueType)
        {
            LLVMTypeRef sharedType = valueType.CreateLLVMSharedType();
            LLVMTypeRef sharedCreateFunctionType = LLVMTypeRef.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[]
                {
                    valueType,
                    LLVMTypeRef.PointerType(sharedType, 0u)
                },
                false);

            LLVMValueRef sharedCreateFunction = module.AddFunction(functionName, sharedCreateFunctionType);
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

            return sharedCreateFunction;
        }

        private static void CompileSharedGetValue(FunctionCompiler compiler, FunctionalNode sharedGetValueNode)
        {
            var valueSource = (LocalAllocationValueSource)compiler.GetTerminalValueSource(sharedGetValueNode.OutputTerminals[0]);
            NIType elementType = valueSource.AllocationNIType.GetReferentType();
            string specializedName = MonomorphizeFunctionName("shared_getvalue", elementType.ToEnumerable());
            LLVMValueRef sharedGetValueFunction = compiler.GetSpecializedFunction(
                specializedName,
                () => CreateSharedGetValueFunction(compiler.Module, specializedName, elementType.AsLLVMType()));
            compiler.CreateCallForFunctionalNode(sharedGetValueFunction, sharedGetValueNode);
        }

        private static LLVMValueRef CreateSharedGetValueFunction(Module module, string functionName, LLVMTypeRef valueType)
        {
            LLVMTypeRef sharedType = valueType.CreateLLVMSharedType();
            LLVMTypeRef sharedGetValueFunctionType = LLVMTypeRef.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[]
                {
                    LLVMTypeRef.PointerType(sharedType, 0u),
                    LLVMTypeRef.PointerType(LLVMTypeRef.PointerType(valueType, 0u), 0u)
                },
                false);

            LLVMValueRef sharedGetValueFunction = module.AddFunction(functionName, sharedGetValueFunctionType);
            LLVMBasicBlockRef entryBlock = sharedGetValueFunction.AppendBasicBlock("entry");
            var builder = new IRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef shared = builder.CreateLoad(sharedGetValueFunction.GetParam(0u), "shared"),
                valuePtr = builder.CreateStructGEP(shared, 1u, "valuePtr"),
                valuePtrPtr = sharedGetValueFunction.GetParam(1u);
            builder.CreateStore(valuePtr, valuePtrPtr);
            builder.CreateRetVoid();
            return sharedGetValueFunction;
        }

        private static LLVMValueRef CreateSharedCloneFunction(Module module, string functionName, LLVMTypeRef valueType)
        {
            LLVMTypeRef sharedType = valueType.CreateLLVMSharedType();
            LLVMTypeRef sharedCloneFunctionType = LLVMTypeRef.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[]
                {
                    LLVMTypeRef.PointerType(sharedType, 0u),
                    LLVMTypeRef.PointerType(sharedType, 0u),
                },
                false);

            LLVMValueRef sharedCloneFunction = module.AddFunction(functionName, sharedCloneFunctionType);
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
            return sharedCloneFunction;
        }

        private static LLVMValueRef CreateSharedDropFunction(FunctionCompiler compiler, Module module, string functionName, NIType valueType)
        {
            LLVMTypeRef valueLLVMType = valueType.AsLLVMType();
            LLVMTypeRef sharedType = valueLLVMType.CreateLLVMSharedType();
            LLVMTypeRef sharedCloneFunctionType = LLVMTypeRef.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[]
                {
                    LLVMTypeRef.PointerType(sharedType, 0u),
                },
                false);

            LLVMValueRef sharedCloneFunction = module.AddFunction(functionName, sharedCloneFunctionType);
            LLVMBasicBlockRef entryBlock = sharedCloneFunction.AppendBasicBlock("entry"),
                noRefsRemainingBlock = sharedCloneFunction.AppendBasicBlock("noRefsRemaining"),
                endBlock = sharedCloneFunction.AppendBasicBlock("end");
            var builder = new IRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef shared = builder.CreateLoad(sharedCloneFunction.GetParam(0u), "shared"),
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
            return sharedCloneFunction;
        }
    }
}
