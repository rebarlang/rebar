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
                () => CreateVectorCreateFunction(compiler, specializedName, elementType.AsLLVMType()));
            compiler.CreateCallForFunctionalNode(vectorCreateFunction, vectorCreateNode);
        }

        private static LLVMValueRef CreateVectorCreateFunction(FunctionCompiler compiler, string functionName, LLVMTypeRef elementType)
        {
            LLVMTypeRef vectorCreateFunctionType = LLVMTypeRef.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[]
                {
                    LLVMTypeRef.PointerType(elementType.CreateLLVMVectorType(), 0u)
                },
                false);

            LLVMValueRef vectorCreateFunction = compiler.Module.AddFunction(functionName, vectorCreateFunctionType);
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
                () => CreateVectorInitializeFunction(compiler, specializedName, elementType.AsLLVMType()));
            compiler.CreateCallForFunctionalNode(vectorInitializeFunction, vectorInitializeNode);
        }

        private static LLVMValueRef CreateVectorInitializeFunction(FunctionCompiler compiler, string functionName, LLVMTypeRef elementType)
        {
            LLVMTypeRef vectorLLVMType = elementType.CreateLLVMVectorType();
            LLVMTypeRef vectorInitializeFunctionType = LLVMTypeRef.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[]
                {
                    elementType,
                    LLVMTypeRef.Int32Type(),
                    LLVMTypeRef.PointerType(vectorLLVMType, 0u)
                },
                false);

            LLVMValueRef vectorInitializeFunction = compiler.Module.AddFunction(functionName, vectorInitializeFunctionType);
            LLVMBasicBlockRef entryBlock = vectorInitializeFunction.AppendBasicBlock("entry");
            var builder = new IRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef element = vectorInitializeFunction.GetParam(0u),
                size = vectorInitializeFunction.GetParam(1u),
                vectorPtr = vectorInitializeFunction.GetParam(2u),
                allocationPtr = builder.CreateArrayMalloc(elementType, size, "allocationPtr");
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
            LLVMValueRef vector = builder.BuildStructValue(
                vectorLLVMType,
                new LLVMValueRef[] { allocationPtr, size, size },
                "vector");
            builder.CreateStore(vector, vectorPtr);
            builder.CreateRetVoid();

            LLVMValueRef[] indexIncomingValues = new LLVMValueRef[2] { 0.AsLLVMValue(), incrementIndex };
            LLVMBasicBlockRef[] indexIncomingBlocks = new LLVMBasicBlockRef[] { entryBlock, loopBodyBlock };
            index.AddIncoming(indexIncomingValues, indexIncomingBlocks, 2u);

            return vectorInitializeFunction;
        }

        private static LLVMValueRef CreateVectorCloneFunction(FunctionCompiler compiler, string functionName, NIType elementType)
        {
            LLVMTypeRef elementLLVMType = elementType.AsLLVMType();
            LLVMTypeRef vectorCloneFunctionType = LLVMTypeRef.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[]
                {
                    LLVMTypeRef.PointerType(elementLLVMType.CreateLLVMVectorType(), 0u),
                    LLVMTypeRef.PointerType(elementLLVMType.CreateLLVMVectorType(), 0u),
                },
                false);

            LLVMValueRef vectorDropFunction = compiler.Module.AddFunction(functionName, vectorCloneFunctionType);
            LLVMBasicBlockRef entryBlock = vectorDropFunction.AppendBasicBlock("entry");
            var builder = new IRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef existingVectorPtr = vectorDropFunction.GetParam(0u),
                existingVector = builder.CreateLoad(existingVectorPtr, "existingVector"),
                existingVectorAllocationPtr = builder.CreateExtractValue(existingVector, 0u, "existingVectorAllocationPtr"),
                existingVectorSize = builder.CreateExtractValue(existingVector, 1u, "existingVectorSize"),
                existingVectorCapacity = builder.CreateExtractValue(existingVector, 2u, "existingVectorCapacity"),
                newVectorAllocationPtr = builder.CreateArrayMalloc(elementLLVMType, existingVectorCapacity, "newVectorAllocationPtr");

            LLVMValueRef elementCloneFunction;
            if (compiler.TryGetCloneFunction(elementType, out elementCloneFunction))
            {
                LLVMBasicBlockRef loopStartBlock = vectorDropFunction.AppendBasicBlock("loopStart"),
                    loopBodyBlock = vectorDropFunction.AppendBasicBlock("loopBody"),
                    loopEndBlock = vectorDropFunction.AppendBasicBlock("loopEnd");
                builder.CreateBr(loopStartBlock);

                builder.PositionBuilderAtEnd(loopStartBlock);
                LLVMValueRef index = builder.CreatePhi(LLVMTypeRef.Int32Type(), "index");
                LLVMValueRef indexLessThanSize = builder.CreateICmp(LLVMIntPredicate.LLVMIntSLT, index, existingVectorSize, "indexLessThanSize");
                builder.CreateCondBr(indexLessThanSize, loopBodyBlock, loopEndBlock);

                builder.PositionBuilderAtEnd(loopBodyBlock);
                LLVMValueRef existingElementPtr = builder.CreateGEP(existingVectorAllocationPtr, new LLVMValueRef[] { index }, "existingElementPtr"),
                    newElementPtr = builder.CreateGEP(newVectorAllocationPtr, new LLVMValueRef[] { index }, "newElementPtr");
                builder.CreateCall(elementCloneFunction, new LLVMValueRef[] { existingElementPtr, newElementPtr }, string.Empty);
                LLVMValueRef incrementIndex = builder.CreateAdd(index, 1.AsLLVMValue(), "incrementIndex");
                builder.CreateBr(loopStartBlock);

                LLVMValueRef[] indexIncomingValues = new LLVMValueRef[2] { 0.AsLLVMValue(), incrementIndex };
                LLVMBasicBlockRef[] indexIncomingBlocks = new LLVMBasicBlockRef[] { entryBlock, loopBodyBlock };
                index.AddIncoming(indexIncomingValues, indexIncomingBlocks, 2u);

                builder.PositionBuilderAtEnd(loopEndBlock);
            }
            else
            {
                LLVMValueRef existingVectorSizeExtend = builder.CreateSExt(existingVectorSize, LLVMTypeRef.Int64Type(), "existingVectorSizeExtend"),
                    bytesToCopyExtend = builder.CreateMul(existingVectorSizeExtend, elementLLVMType.SizeOf(), "bytesToCopyExtend"),
                    existingAllocationPtrCast = builder.CreateBitCast(existingVectorAllocationPtr, LLVMExtensions.BytePointerType, "existingAllocationPtrCast"),
                    newAllocationPtrCast = builder.CreateBitCast(newVectorAllocationPtr, LLVMExtensions.BytePointerType, "newAllocationPtrCast");
                builder.CreateCall(compiler._commonExternalFunctions.CopyMemoryFunction, new LLVMValueRef[] { existingAllocationPtrCast, newAllocationPtrCast, bytesToCopyExtend }, string.Empty);
            }

            LLVMValueRef newVector = builder.CreateInsertValue(existingVector, newVectorAllocationPtr, 0u, "newVector"),
                newVectorPtr = vectorDropFunction.GetParam(1u);
            builder.CreateStore(newVector, newVectorPtr);
            builder.CreateRetVoid();
            return vectorDropFunction;
        }

        private static LLVMValueRef CreateVectorDropFunction(FunctionCompiler compiler, string functionName, NIType elementType)
        {
            LLVMTypeRef vectorDropFunctionType = LLVMTypeRef.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[]
                {
                    LLVMTypeRef.PointerType(elementType.AsLLVMType().CreateLLVMVectorType(), 0u),
                },
                false);

            LLVMValueRef vectorDropFunction = compiler.Module.AddFunction(functionName, vectorDropFunctionType);
            LLVMBasicBlockRef entryBlock = vectorDropFunction.AppendBasicBlock("entry");
            var builder = new IRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef vectorPtr = vectorDropFunction.GetParam(0u),
                vectorAllocationPtrPtr = builder.CreateStructGEP(vectorPtr, 0u, "vectorAllocationPtrPtr"),
                vectorAllocationPtr = builder.CreateLoad(vectorAllocationPtrPtr, "vectorAllocationPtr");

            LLVMValueRef elementDropFunction;
            if (compiler.TryGetDropFunction(elementType, out elementDropFunction))
            {
                LLVMValueRef vectorSizePtr = builder.CreateStructGEP(vectorPtr, 1u, "vectorSizePtr"),
                    vectorSize = builder.CreateLoad(vectorSizePtr, "vectorSize");
                LLVMBasicBlockRef loopStartBlock = vectorDropFunction.AppendBasicBlock("loopStart"),
                    loopBodyBlock = vectorDropFunction.AppendBasicBlock("loopBody"),
                    loopEndBlock = vectorDropFunction.AppendBasicBlock("loopEnd");
                builder.CreateBr(loopStartBlock);

                builder.PositionBuilderAtEnd(loopStartBlock);
                LLVMValueRef index = builder.CreatePhi(LLVMTypeRef.Int32Type(), "index");
                LLVMValueRef indexLessThanSize = builder.CreateICmp(LLVMIntPredicate.LLVMIntSLT, index, vectorSize, "indexLessThanSize");
                builder.CreateCondBr(indexLessThanSize, loopBodyBlock, loopEndBlock);

                builder.PositionBuilderAtEnd(loopBodyBlock);
                LLVMValueRef elementPtr = builder.CreateGEP(vectorAllocationPtr, new LLVMValueRef[] { index }, "elementPtr");
                builder.CreateCall(elementDropFunction, new LLVMValueRef[] { elementPtr }, string.Empty);
                LLVMValueRef incrementIndex = builder.CreateAdd(index, 1.AsLLVMValue(), "incrementIndex");
                builder.CreateBr(loopStartBlock);

                LLVMValueRef[] indexIncomingValues = new LLVMValueRef[2] { 0.AsLLVMValue(), incrementIndex };
                LLVMBasicBlockRef[] indexIncomingBlocks = new LLVMBasicBlockRef[] { entryBlock, loopBodyBlock };
                index.AddIncoming(indexIncomingValues, indexIncomingBlocks, 2u);

                builder.PositionBuilderAtEnd(loopEndBlock);
            }

            builder.CreateFree(vectorAllocationPtr);
            builder.CreateRetVoid();
            return vectorDropFunction;
        }

        private static void CompileVectorToSlice(FunctionCompiler compiler, FunctionalNode vectorToSliceNode)
        {
            var sliceReferenceSource = (LocalAllocationValueSource)compiler.GetTerminalValueSource(vectorToSliceNode.OutputTerminals[0]);
            NIType elementType;
            sliceReferenceSource.AllocationNIType.GetReferentType().TryDestructureSliceType(out elementType);
            string specializedName = MonomorphizeFunctionName("vector_to_slice", elementType.ToEnumerable());
            LLVMValueRef vectorToSliceFunction = compiler.GetSpecializedFunction(
                specializedName,
                () => CreateVectorToSliceFunction(compiler, specializedName, elementType.AsLLVMType()));
            compiler.CreateCallForFunctionalNode(vectorToSliceFunction, vectorToSliceNode);
        }

        private static LLVMValueRef CreateVectorToSliceFunction(FunctionCompiler compiler, string functionName, LLVMTypeRef elementType)
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

            LLVMValueRef vectorToSliceFunction = compiler.Module.AddFunction(functionName, vectorToSliceFunctionType);
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

        private static void CompileVectorAppend(FunctionCompiler compiler, FunctionalNode vectorAppendNode)
        {
            var elementSource = (LocalAllocationValueSource)compiler.GetTerminalValueSource(vectorAppendNode.InputTerminals[1]);
            NIType elementType = elementSource.AllocationNIType;
            string specializedName = MonomorphizeFunctionName("vector_append", elementType.ToEnumerable());
            LLVMValueRef vectorAppendFunction = compiler.GetSpecializedFunction(
                specializedName,
                () => CreateVectorAppendFunction(compiler, specializedName, elementType));
            compiler.CreateCallForFunctionalNode(vectorAppendFunction, vectorAppendNode);
        }

        private static LLVMValueRef CreateVectorAppendFunction(FunctionCompiler compiler, string functionName, NIType elementType)
        {
            LLVMTypeRef elementLLVMType = elementType.AsLLVMType();
            LLVMTypeRef vectorAppendFunctionType = LLVMTypeRef.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[]
                {
                    LLVMTypeRef.PointerType(elementLLVMType.CreateLLVMVectorType(), 0u),
                    elementLLVMType
                },
                false);

            LLVMValueRef vectorAppendFunction = compiler.Module.AddFunction(functionName, vectorAppendFunctionType);
            LLVMBasicBlockRef entryBlock = vectorAppendFunction.AppendBasicBlock("entry"),
                growBlock = vectorAppendFunction.AppendBasicBlock("grow"),
                appendBlock = vectorAppendFunction.AppendBasicBlock("append");
            var builder = new IRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef vectorPtr = vectorAppendFunction.GetParam(0u),
                vectorSizePtr = builder.CreateStructGEP(vectorPtr, 1u, "vectorSizePtr"),
                vectorSize = builder.CreateLoad(vectorSizePtr, "vectorSize"),
                vectorCapacityPtr = builder.CreateStructGEP(vectorPtr, 2u, "vectorCapacityPtr"),
                vectorCapacity = builder.CreateLoad(vectorCapacityPtr, "vectorCapacity"),
                vectorIsFull = builder.CreateICmp(LLVMIntPredicate.LLVMIntEQ, vectorSize, vectorCapacity, "vectorIsFull");
            builder.CreateCondBr(vectorIsFull, growBlock, appendBlock);

            builder.PositionBuilderAtEnd(growBlock);
            string specializedName = MonomorphizeFunctionName("vector_grow", elementType.ToEnumerable());
            LLVMValueRef vectorGrowFunction = compiler.GetSpecializedFunction(
                specializedName,
                () => CreateVectorGrowFunction(compiler, specializedName, elementType));
            builder.CreateCall(vectorGrowFunction, new LLVMValueRef[] { vectorPtr }, string.Empty);
            builder.CreateBr(appendBlock);

            builder.PositionBuilderAtEnd(appendBlock);
            LLVMValueRef vectorAllocationPtrPtr = builder.CreateStructGEP(vectorPtr, 0u, "vectorAllocationPtrPtr"),
                vectorAllocationPtr = builder.CreateLoad(vectorAllocationPtrPtr, "vectorAllocationPtr"),
                elementPtr = builder.CreateGEP(vectorAllocationPtr, new LLVMValueRef[] { vectorSize }, "elementPtr"),
                incrementedSize = builder.CreateAdd(vectorSize, 1.AsLLVMValue(), "incrementedSize");
            builder.CreateStore(vectorAppendFunction.GetParam(1u), elementPtr);
            builder.CreateStore(incrementedSize, vectorSizePtr);
            builder.CreateRetVoid();
            return vectorAppendFunction;
        }

        private static LLVMValueRef CreateVectorGrowFunction(FunctionCompiler compiler, string functionName, NIType elementType)
        {
            LLVMTypeRef elementLLVMType = elementType.AsLLVMType();
            LLVMTypeRef vectorGrowFunctionType = LLVMTypeRef.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[]
                {
                    LLVMTypeRef.PointerType(elementLLVMType.CreateLLVMVectorType(), 0u),
                },
                false);

            LLVMValueRef vectorGrowFunction = compiler.Module.AddFunction(functionName, vectorGrowFunctionType);
            LLVMBasicBlockRef entryBlock = vectorGrowFunction.AppendBasicBlock("entry");
            var builder = new IRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef vectorPtr = vectorGrowFunction.GetParam(0u),
                vectorAllocationPtrPtr = builder.CreateStructGEP(vectorPtr, 0u, "vectorAllocationPtrPtr"),
                oldAllocationPtr = builder.CreateLoad(vectorAllocationPtrPtr, "oldAllocationPtr"),
                vectorCapacityPtr = builder.CreateStructGEP(vectorPtr, 2u, "vector"),
                oldVectorCapacity = builder.CreateLoad(vectorCapacityPtr, "oldVectorCapacity"),
                // TODO: ideally handle integer overflow; also there are ways this could be smarter
                newVectorCapacity = builder.CreateMul(oldVectorCapacity, 2.AsLLVMValue(), "newVectorCapacity"),
                // TODO: handle the case where the allocation fails
                newAllocationPtr = builder.CreateArrayMalloc(elementLLVMType, newVectorCapacity, "newAllocationPtr"),
                oldVectorCapacityExtend = builder.CreateSExt(oldVectorCapacity, LLVMTypeRef.Int64Type(), "oldVectorCapacityExtend"),
                bytesToCopyExtend = builder.CreateMul(oldVectorCapacityExtend, elementLLVMType.SizeOf(), "bytesToCopyExtend"),
                oldAllocationPtrCast = builder.CreateBitCast(oldAllocationPtr, LLVMExtensions.BytePointerType, "oldAllocationPtrCast"),
                newAllocationPtrCast = builder.CreateBitCast(newAllocationPtr, LLVMExtensions.BytePointerType, "newAllocationPtrCast");
            builder.CreateCall(compiler._commonExternalFunctions.CopyMemoryFunction, new LLVMValueRef[] { oldAllocationPtrCast, newAllocationPtrCast, bytesToCopyExtend }, string.Empty);
            builder.CreateStore(newAllocationPtr, vectorAllocationPtrPtr);
            builder.CreateStore(newVectorCapacity, vectorCapacityPtr);
            builder.CreateFree(oldAllocationPtr);
            builder.CreateRetVoid();
            return vectorGrowFunction;
        }

        private static void CompileVectorRemoveLast(FunctionCompiler compiler, FunctionalNode vectorRemoveLastNode)
        {
            var elementSource = (LocalAllocationValueSource)compiler.GetTerminalValueSource(vectorRemoveLastNode.OutputTerminals[1]);
            NIType elementType;
            elementSource.AllocationNIType.TryDestructureOptionType(out elementType);
            string specializedName = MonomorphizeFunctionName("vector_remove_last", elementType.ToEnumerable());
            LLVMValueRef vectorRemoveLastFunction = compiler.GetSpecializedFunction(
                specializedName,
                () => CreateVectorRemoveLastFunction(compiler, specializedName, elementType));
            compiler.CreateCallForFunctionalNode(vectorRemoveLastFunction, vectorRemoveLastNode);
        }

        private static LLVMValueRef CreateVectorRemoveLastFunction(FunctionCompiler compiler, string functionName, NIType elementType)
        {
            LLVMTypeRef elementLLVMType = elementType.AsLLVMType(),
                elementOptionLLVMType = elementLLVMType.CreateLLVMOptionType();
            LLVMTypeRef vectorRemoveLastFunctionType = LLVMTypeRef.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[]
                {
                    LLVMTypeRef.PointerType(elementLLVMType.CreateLLVMVectorType(), 0u),
                    LLVMTypeRef.PointerType(elementOptionLLVMType, 0)
                },
                false);

            LLVMValueRef vectorRemoveLastFunction = compiler.Module.AddFunction(functionName, vectorRemoveLastFunctionType);
            LLVMBasicBlockRef entryBlock = vectorRemoveLastFunction.AppendBasicBlock("entry"),
                hasElementsBlock = vectorRemoveLastFunction.AppendBasicBlock("hasElements"),
                noElementsBlock = vectorRemoveLastFunction.AppendBasicBlock("noElements"),
                endBlock = vectorRemoveLastFunction.AppendBasicBlock("end");
            var builder = new IRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef vectorPtr = vectorRemoveLastFunction.GetParam(0u),
                vectorSizePtr = builder.CreateStructGEP(vectorPtr, 1u, "vectorSizePtr"),
                vectorSize = builder.CreateLoad(vectorSizePtr, "vectorSize"),
                optionElementPtr = vectorRemoveLastFunction.GetParam(1u),
                hasElements = builder.CreateICmp(LLVMIntPredicate.LLVMIntSGT, vectorSize, 0.AsLLVMValue(), "hasElements");
            builder.CreateCondBr(hasElements, hasElementsBlock, noElementsBlock);

            builder.PositionBuilderAtEnd(hasElementsBlock);
            LLVMValueRef vectorAllocationPtrPtr = builder.CreateStructGEP(vectorPtr, 0u, "vectorAllocationPtrPtr"),
                vectorAllocationPtr = builder.CreateLoad(vectorAllocationPtrPtr, "vectorAllocationPtr"),
                lastIndex = builder.CreateSub(vectorSize, 1.AsLLVMValue(), "lastIndex"),
                elementToRemovePtr = builder.CreateGEP(vectorAllocationPtr, new LLVMValueRef[] { lastIndex }, "elementToRemovePtr"),
                elementToRemove = builder.CreateLoad(elementToRemovePtr, "elementToRemove"),
                someElement = builder.BuildOptionValue(elementOptionLLVMType, elementToRemove);
            builder.CreateStore(lastIndex, vectorSizePtr);
            builder.CreateBr(endBlock);

            builder.PositionBuilderAtEnd(noElementsBlock);
            LLVMValueRef noneElement = builder.BuildOptionValue(elementOptionLLVMType, null);
            builder.CreateBr(endBlock);

            builder.PositionBuilderAtEnd(endBlock);
            LLVMValueRef optionElement = builder.CreatePhi(elementOptionLLVMType, "optionElement");
            LLVMValueRef[] incomingValues = new[] { someElement, noneElement };
            LLVMBasicBlockRef[] incomingBlocks = new[] { hasElementsBlock, noElementsBlock };
            optionElement.AddIncoming(incomingValues, incomingBlocks, 2u);
            builder.CreateStore(optionElement, optionElementPtr);
            builder.CreateRetVoid();
            return vectorRemoveLastFunction;
        }
    }
}
