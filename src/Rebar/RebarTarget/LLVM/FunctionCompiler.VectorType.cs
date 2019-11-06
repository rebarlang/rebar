using LLVMSharp;
using NationalInstruments;
using NationalInstruments.DataTypes;
using Rebar.Common;
using Rebar.Compiler.Nodes;

namespace Rebar.RebarTarget.LLVM
{
    internal partial class FunctionCompiler
    {
        private static void CompileVectorCreate(FunctionCompiler compiler, FunctionalNode vectorCreateNode)
        {
            var vectorSource = (LocalAllocationValueSource)compiler.GetTerminalValueSource(vectorCreateNode.OutputTerminals[0]);
            NIType elementType;
            vectorSource.AllocationNIType.TryDestructureVectorType(out elementType);
            string specializedName = MonomorphizeFunctionName("vector_create", elementType.ToEnumerable());
            LLVMValueRef vectorCreateFunction = compiler.GetSpecializedFunction(
                specializedName,
                () => CreateVectorCreateFunction(compiler.Module, specializedName, elementType.AsLLVMType()));
            compiler.CreateCallForFunctionalNode(vectorCreateFunction, vectorCreateNode);
        }

        private static LLVMValueRef CreateVectorCreateFunction(Module module, string functionName, LLVMTypeRef elementType)
        {
            LLVMTypeRef vectorCreateFunctionType = LLVMTypeRef.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[]
                {
                    LLVMTypeRef.PointerType(elementType.CreateLLVMVectorType(), 0u)
                },
                false);

            LLVMValueRef vectorCreateFunction = module.AddFunction(functionName, vectorCreateFunctionType);
            LLVMBasicBlockRef entryBlock = vectorCreateFunction.AppendBasicBlock("entry");
            var builder = new IRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef vectorPtr = vectorCreateFunction.GetParam(0u),
                vectorCapacity = 4.AsLLVMValue(),
                allocationPtr = builder.CreateArrayMalloc(elementType, vectorCapacity, "allocationPtr"),
                vectorAllocationPtrPtr = builder.CreateStructGEP(vectorPtr, 0u, "vectorAllocationPtrPtr"),
                vectorSizePtr = builder.CreateStructGEP(vectorPtr, 1u, "vectorSizePtr"),
                vectorCapacityPtr = builder.CreateStructGEP(vectorPtr, 2u, "vectorCapacityPtr");
            builder.CreateStore(allocationPtr, vectorAllocationPtrPtr);
            builder.CreateStore(0.AsLLVMValue(), vectorSizePtr);
            builder.CreateStore(vectorCapacity, vectorCapacityPtr);
            builder.CreateRetVoid();
            return vectorCreateFunction;
        }

        private static void CompileVectorInitialize(FunctionCompiler compiler, FunctionalNode vectorInitializeNode)
        {
            var elementSource = (LocalAllocationValueSource)compiler.GetTerminalValueSource(vectorInitializeNode.InputTerminals[0]);
            NIType elementType = elementSource.AllocationNIType;
            string specializedName = MonomorphizeFunctionName("vector_initialize", elementType.ToEnumerable());
            LLVMValueRef vectorInitializeFunction = compiler.GetSpecializedFunction(
                specializedName,
                () => CreateVectorInitializeFunction(compiler.Module, specializedName, elementType.AsLLVMType()));
            compiler.CreateCallForFunctionalNode(vectorInitializeFunction, vectorInitializeNode);
        }

        private static LLVMValueRef CreateVectorInitializeFunction(Module module, string functionName, LLVMTypeRef elementType)
        {
            LLVMTypeRef vectorInitializeFunctionType = LLVMTypeRef.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[]
                {
                    elementType,
                    LLVMTypeRef.Int32Type(),
                    LLVMTypeRef.PointerType(elementType.CreateLLVMVectorType(), 0u)
                },
                false);

            LLVMValueRef vectorInitializeFunction = module.AddFunction(functionName, vectorInitializeFunctionType);
            LLVMBasicBlockRef entryBlock = vectorInitializeFunction.AppendBasicBlock("entry");
            var builder = new IRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef element = vectorInitializeFunction.GetParam(0u),
                size = vectorInitializeFunction.GetParam(1u),
                vectorPtr = vectorInitializeFunction.GetParam(2u);
            LLVMValueRef allocationPtr = builder.CreateArrayMalloc(elementType, size, "allocationPtr");
            LLVMValueRef vectorAllocationPtrPtr = builder.CreateStructGEP(vectorPtr, 0u, "vectorAllocationPtrPtr"),
                vectorSizePtr = builder.CreateStructGEP(vectorPtr, 1u, "vectorSizePtr"),
                vectorCapacityPtr = builder.CreateStructGEP(vectorPtr, 2u, "vectorCapacityPtr");
            builder.CreateStore(allocationPtr, vectorAllocationPtrPtr);
            builder.CreateStore(size, vectorSizePtr);
            builder.CreateStore(size, vectorCapacityPtr);
            LLVMBasicBlockRef loopStartBlock = vectorInitializeFunction.AppendBasicBlock("loopStart"),
                loopBodyBlock = vectorInitializeFunction.AppendBasicBlock("loopBody"),
                loopEndBlock = vectorInitializeFunction.AppendBasicBlock("loopEnd");
            builder.CreateBr(loopStartBlock);

            builder.PositionBuilderAtEnd(loopStartBlock);
            LLVMValueRef index = builder.CreatePhi(LLVMTypeRef.Int32Type(), "index");
            LLVMValueRef indexLessThanSize = builder.CreateICmp(LLVMIntPredicate.LLVMIntSLT, index, size, "indexLessThanSize");
            builder.CreateCondBr(indexLessThanSize, loopBodyBlock, loopEndBlock);

            builder.PositionBuilderAtEnd(loopBodyBlock);
            LLVMValueRef vectorIndexPtr = builder.CreateGEP(allocationPtr, new LLVMValueRef[] { index }, "vectorIndexPtr");
            builder.CreateStore(element, vectorIndexPtr);
            LLVMValueRef incrementIndex = builder.CreateAdd(index, 1.AsLLVMValue(), "incrementIndex");
            builder.CreateBr(loopStartBlock);

            builder.PositionBuilderAtEnd(loopEndBlock);
            builder.CreateRetVoid();

            LLVMValueRef[] indexIncomingValues = new LLVMValueRef[2] { 0.AsLLVMValue(), incrementIndex };
            LLVMBasicBlockRef[] indexIncomingBlocks = new LLVMBasicBlockRef[] { entryBlock, loopBodyBlock };
            index.AddIncoming(indexIncomingValues, indexIncomingBlocks, 2u);

            return vectorInitializeFunction;
        }

        private static void CompileVectorToSlice(FunctionCompiler compiler, FunctionalNode vectorToSliceNode)
        {
            var sliceReferenceSource = (LocalAllocationValueSource)compiler.GetTerminalValueSource(vectorToSliceNode.OutputTerminals[0]);
            NIType elementType;
            sliceReferenceSource.AllocationNIType.GetReferentType().TryDestructureSliceType(out elementType);
            string specializedName = MonomorphizeFunctionName("vector_to_slice", elementType.ToEnumerable());
            LLVMValueRef vectorInitializeFunction = compiler.GetSpecializedFunction(
                specializedName,
                () => CreateVectorToSliceFunction(compiler.Module, specializedName, elementType.AsLLVMType()));
            compiler.CreateCallForFunctionalNode(vectorInitializeFunction, vectorToSliceNode);
        }

        private static LLVMValueRef CreateVectorToSliceFunction(Module module, string functionName, LLVMTypeRef elementType)
        {
            LLVMTypeRef sliceReferenceType = elementType.CreateLLVMSliceReferenceType();
            LLVMTypeRef vectorToSliceFunctionType = LLVMTypeRef.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[]
                {
                    LLVMTypeRef.PointerType(elementType.CreateLLVMVectorType(), 0u),
                    LLVMTypeRef.PointerType(sliceReferenceType, 0u)
                },
                false);

            LLVMValueRef vectorToSliceFunction = module.AddFunction(functionName, vectorToSliceFunctionType);
            LLVMBasicBlockRef entryBlock = vectorToSliceFunction.AppendBasicBlock("entry");
            var builder = new IRBuilder();
            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef vectorPtr = vectorToSliceFunction.GetParam(0u),
                vectorBufferPtrPtr = builder.CreateStructGEP(vectorPtr, 0u, "vectorBufferPtrPtr"),
                vectorBufferPtr = builder.CreateLoad(vectorBufferPtrPtr, "vectorBufferPtr"),
                vectorSizePtr = builder.CreateStructGEP(vectorPtr, 1u, "vectorSizePtr"),
                vectorSize = builder.CreateLoad(vectorSizePtr, "vectorSize"),
                sliceRef = builder.BuildSliceReferenceValue(sliceReferenceType, vectorBufferPtr, vectorSize);
            builder.CreateStore(sliceRef, vectorToSliceFunction.GetParam(1u));
            builder.CreateRetVoid();
            return vectorToSliceFunction;
        }
    }
}
