using System.Linq;
using LLVMSharp;
using NationalInstruments;
using NationalInstruments.DataTypes;
using Rebar.Common;

namespace Rebar.RebarTarget.LLVM
{
    internal partial class FunctionCompiler
    {
        private static void BuildVectorCreateFunction(FunctionCompiler compiler, NIType signature, LLVMValueRef vectorCreateFunction)
        {
            LLVMTypeRef elementLLVMType = compiler.Context.AsLLVMType(signature.GetGenericParameters().First());

            LLVMBasicBlockRef entryBlock = vectorCreateFunction.AppendBasicBlock("entry");
            var builder = compiler.Context.CreateIRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef vectorPtr = vectorCreateFunction.GetParam(0u),
                vectorCapacity = compiler.Context.AsLLVMValue(4),
                allocationPtr = builder.CreateArrayMalloc(elementLLVMType, vectorCapacity, "allocationPtr"),
                vector = builder.BuildStructValue(
                    compiler.Context.CreateLLVMVectorType(elementLLVMType),
                    new LLVMValueRef[] { allocationPtr, compiler.Context.AsLLVMValue(0), vectorCapacity },
                    "vector");
            builder.CreateStore(vector, vectorPtr);
            builder.CreateRetVoid();
        }

        private static void BuildVectorInitializeFunction(FunctionCompiler compiler, NIType signature, LLVMValueRef vectorInitializeFunction)
        {
            LLVMTypeRef elementLLVMType = compiler.Context.AsLLVMType(signature.GetGenericParameters().First());
            LLVMTypeRef vectorLLVMType = compiler.Context.CreateLLVMVectorType(elementLLVMType);

            LLVMBasicBlockRef entryBlock = vectorInitializeFunction.AppendBasicBlock("entry");
            var builder = compiler.Context.CreateIRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef element = vectorInitializeFunction.GetParam(0u),
                size = vectorInitializeFunction.GetParam(1u),
                vectorPtr = vectorInitializeFunction.GetParam(2u),
                allocationPtr = builder.CreateArrayMalloc(elementLLVMType, size, "allocationPtr");
            LLVMBasicBlockRef loopStartBlock = vectorInitializeFunction.AppendBasicBlock("loopStart"),
                loopBodyBlock = vectorInitializeFunction.AppendBasicBlock("loopBody"),
                loopEndBlock = vectorInitializeFunction.AppendBasicBlock("loopEnd");
            builder.CreateBr(loopStartBlock);

            builder.PositionBuilderAtEnd(loopStartBlock);
            LLVMValueRef index = builder.CreatePhi(compiler.Context.Int32Type, "index");
            LLVMValueRef indexLessThanSize = builder.CreateICmp(LLVMIntPredicate.LLVMIntSLT, index, size, "indexLessThanSize");
            builder.CreateCondBr(indexLessThanSize, loopBodyBlock, loopEndBlock);

            builder.PositionBuilderAtEnd(loopBodyBlock);
            LLVMValueRef vectorIndexPtr = builder.CreateGEP(allocationPtr, new LLVMValueRef[] { index }, "vectorIndexPtr");
            builder.CreateStore(element, vectorIndexPtr);
            LLVMValueRef incrementIndex = builder.CreateAdd(index, compiler.Context.AsLLVMValue(1), "incrementIndex");
            builder.CreateBr(loopStartBlock);

            builder.PositionBuilderAtEnd(loopEndBlock);
            LLVMValueRef vector = builder.BuildStructValue(
                vectorLLVMType,
                new LLVMValueRef[] { allocationPtr, size, size },
                "vector");
            builder.CreateStore(vector, vectorPtr);
            builder.CreateRetVoid();

            index.AddIncoming(compiler.Context.AsLLVMValue(0), entryBlock);
            index.AddIncoming(incrementIndex, loopBodyBlock);
        }

        private static void BuildVectorCloneFunction(FunctionCompiler compiler, NIType signature, LLVMValueRef vectorCloneFunction)
        {
            NIType elementType;
            signature.GetGenericParameters().First().TryDestructureVectorType(out elementType);
            LLVMTypeRef elementLLVMType = compiler.Context.AsLLVMType(elementType);

            LLVMBasicBlockRef entryBlock = vectorCloneFunction.AppendBasicBlock("entry");
            var builder = compiler.Context.CreateIRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef existingVectorPtr = vectorCloneFunction.GetParam(0u),
                existingVector = builder.CreateLoad(existingVectorPtr, "existingVector"),
                existingVectorAllocationPtr = builder.CreateExtractValue(existingVector, 0u, "existingVectorAllocationPtr"),
                existingVectorSize = builder.CreateExtractValue(existingVector, 1u, "existingVectorSize"),
                existingVectorCapacity = builder.CreateExtractValue(existingVector, 2u, "existingVectorCapacity"),
                newVectorAllocationPtr = builder.CreateArrayMalloc(elementLLVMType, existingVectorCapacity, "newVectorAllocationPtr");

            LLVMValueRef elementCloneFunction;
            if (compiler.TryGetCloneFunction(elementType, out elementCloneFunction))
            {
                LLVMBasicBlockRef loopStartBlock = vectorCloneFunction.AppendBasicBlock("loopStart"),
                    loopBodyBlock = vectorCloneFunction.AppendBasicBlock("loopBody"),
                    loopEndBlock = vectorCloneFunction.AppendBasicBlock("loopEnd");
                builder.CreateBr(loopStartBlock);

                builder.PositionBuilderAtEnd(loopStartBlock);
                LLVMValueRef index = builder.CreatePhi(compiler.Context.Int32Type, "index");
                LLVMValueRef indexLessThanSize = builder.CreateICmp(LLVMIntPredicate.LLVMIntSLT, index, existingVectorSize, "indexLessThanSize");
                builder.CreateCondBr(indexLessThanSize, loopBodyBlock, loopEndBlock);

                builder.PositionBuilderAtEnd(loopBodyBlock);
                LLVMValueRef existingElementPtr = builder.CreateGEP(existingVectorAllocationPtr, new LLVMValueRef[] { index }, "existingElementPtr"),
                    newElementPtr = builder.CreateGEP(newVectorAllocationPtr, new LLVMValueRef[] { index }, "newElementPtr");
                builder.CreateCall(elementCloneFunction, new LLVMValueRef[] { existingElementPtr, newElementPtr }, string.Empty);
                LLVMValueRef incrementIndex = builder.CreateAdd(index, compiler.Context.AsLLVMValue(1), "incrementIndex");
                builder.CreateBr(loopStartBlock);

                index.AddIncoming(compiler.Context.AsLLVMValue(0), entryBlock);
                index.AddIncoming(incrementIndex, loopBodyBlock);

                builder.PositionBuilderAtEnd(loopEndBlock);
            }
            else
            {
                LLVMValueRef existingVectorSizeExtend = builder.CreateSExt(existingVectorSize, compiler.Context.Int64Type, "existingVectorSizeExtend"),
                    bytesToCopy = builder.CreateMul(existingVectorSizeExtend, elementLLVMType.SizeOf(), "bytesToCopy");
                compiler.CreateCallToCopyMemory(builder, newVectorAllocationPtr, existingVectorAllocationPtr, bytesToCopy);
            }

            LLVMValueRef newVector = builder.CreateInsertValue(existingVector, newVectorAllocationPtr, 0u, "newVector"),
                newVectorPtr = vectorCloneFunction.GetParam(1u);
            builder.CreateStore(newVector, newVectorPtr);
            builder.CreateRetVoid();
        }

        internal static void BuildVectorDropFunction(FunctionCompiler compiler, NIType signature, LLVMValueRef vectorDropFunction)
        {
            NIType elementType;
            signature.GetGenericParameters().First().TryDestructureVectorType(out elementType);

            LLVMBasicBlockRef entryBlock = vectorDropFunction.AppendBasicBlock("entry");
            var builder = compiler.Context.CreateIRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef vectorPtr = vectorDropFunction.GetParam(0u),
                vectorAllocationPtrPtr = builder.CreateStructGEP(vectorPtr, 0u, "vectorAllocationPtrPtr"),
                vectorAllocationPtr = builder.CreateLoad(vectorAllocationPtrPtr, "vectorAllocationPtr");

            LLVMValueRef elementDropFunction;
            if (TraitHelpers.TryGetDropFunction(elementType, compiler, out elementDropFunction))
            {
                LLVMValueRef vectorSizePtr = builder.CreateStructGEP(vectorPtr, 1u, "vectorSizePtr"),
                    vectorSize = builder.CreateLoad(vectorSizePtr, "vectorSize");
                LLVMBasicBlockRef loopStartBlock = vectorDropFunction.AppendBasicBlock("loopStart"),
                    loopBodyBlock = vectorDropFunction.AppendBasicBlock("loopBody"),
                    loopEndBlock = vectorDropFunction.AppendBasicBlock("loopEnd");
                builder.CreateBr(loopStartBlock);

                builder.PositionBuilderAtEnd(loopStartBlock);
                LLVMValueRef index = builder.CreatePhi(compiler.Context.Int32Type, "index");
                LLVMValueRef indexLessThanSize = builder.CreateICmp(LLVMIntPredicate.LLVMIntSLT, index, vectorSize, "indexLessThanSize");
                builder.CreateCondBr(indexLessThanSize, loopBodyBlock, loopEndBlock);

                builder.PositionBuilderAtEnd(loopBodyBlock);
                LLVMValueRef elementPtr = builder.CreateGEP(vectorAllocationPtr, new LLVMValueRef[] { index }, "elementPtr");
                builder.CreateCall(elementDropFunction, new LLVMValueRef[] { elementPtr }, string.Empty);
                LLVMValueRef incrementIndex = builder.CreateAdd(index, compiler.Context.AsLLVMValue(1), "incrementIndex");
                builder.CreateBr(loopStartBlock);

                index.AddIncoming(compiler.Context.AsLLVMValue(0), entryBlock);
                index.AddIncoming(incrementIndex, loopBodyBlock);

                builder.PositionBuilderAtEnd(loopEndBlock);
            }

            builder.CreateFree(vectorAllocationPtr);
            builder.CreateRetVoid();
        }

        private static void BuildVectorToSliceFunction(FunctionCompiler compiler, NIType signature, LLVMValueRef vectorToSliceFunction)
        {
            LLVMTypeRef sliceElementType = compiler.Context.AsLLVMType(signature.GetGenericParameters().First());
            LLVMTypeRef sliceReferenceType = compiler.Context.CreateLLVMSliceReferenceType(sliceElementType);

            LLVMBasicBlockRef entryBlock = vectorToSliceFunction.AppendBasicBlock("entry");
            var builder = compiler.Context.CreateIRBuilder();
            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef vectorPtr = vectorToSliceFunction.GetParam(0u),
                vectorBufferPtrPtr = builder.CreateStructGEP(vectorPtr, 0u, "vectorBufferPtrPtr"),
                vectorBufferPtr = builder.CreateLoad(vectorBufferPtrPtr, "vectorBufferPtr"),
                vectorSizePtr = builder.CreateStructGEP(vectorPtr, 1u, "vectorSizePtr"),
                vectorSize = builder.CreateLoad(vectorSizePtr, "vectorSize"),
                sliceRef = builder.BuildSliceReferenceValue(sliceReferenceType, vectorBufferPtr, vectorSize);
            builder.CreateStore(sliceRef, vectorToSliceFunction.GetParam(1u));
            builder.CreateRetVoid();
        }

        private static void BuildVectorAppendFunction(FunctionCompiler compiler, NIType signature, LLVMValueRef vectorAppendFunction)
        {
            NIType elementType = signature.GetGenericParameters().First();

            LLVMBasicBlockRef entryBlock = vectorAppendFunction.AppendBasicBlock("entry"),
                growBlock = vectorAppendFunction.AppendBasicBlock("grow"),
                appendBlock = vectorAppendFunction.AppendBasicBlock("append");
            var builder = compiler.Context.CreateIRBuilder();

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
            LLVMValueRef vectorGrowFunction = compiler._sharedData.FunctionImporter.GetCachedFunction(
                specializedName,
                () => CreateVectorGrowFunction(compiler, specializedName, elementType));
            builder.CreateCall(vectorGrowFunction, new LLVMValueRef[] { vectorPtr }, string.Empty);
            builder.CreateBr(appendBlock);

            builder.PositionBuilderAtEnd(appendBlock);
            LLVMValueRef vectorAllocationPtrPtr = builder.CreateStructGEP(vectorPtr, 0u, "vectorAllocationPtrPtr"),
                vectorAllocationPtr = builder.CreateLoad(vectorAllocationPtrPtr, "vectorAllocationPtr"),
                elementPtr = builder.CreateGEP(vectorAllocationPtr, new LLVMValueRef[] { vectorSize }, "elementPtr"),
                incrementedSize = builder.CreateAdd(vectorSize, compiler.Context.AsLLVMValue(1), "incrementedSize");
            builder.CreateStore(vectorAppendFunction.GetParam(1u), elementPtr);
            builder.CreateStore(incrementedSize, vectorSizePtr);
            builder.CreateRetVoid();
        }

        private static LLVMValueRef CreateVectorGrowFunction(FunctionCompiler compiler, string functionName, NIType elementType)
        {
            LLVMTypeRef elementLLVMType = compiler.Context.AsLLVMType(elementType);
            LLVMTypeRef vectorGrowFunctionType = LLVMTypeRef.FunctionType(
                compiler.Context.VoidType,
                new LLVMTypeRef[]
                {
                    LLVMTypeRef.PointerType(compiler.Context.CreateLLVMVectorType(elementLLVMType), 0u),
                },
                false);

            LLVMValueRef vectorGrowFunction = compiler.Module.AddFunction(functionName, vectorGrowFunctionType);
            LLVMBasicBlockRef entryBlock = vectorGrowFunction.AppendBasicBlock("entry");
            var builder = compiler.Context.CreateIRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef vectorPtr = vectorGrowFunction.GetParam(0u),
                vector = builder.CreateLoad(vectorPtr, "vector"),
                oldAllocationPtr = builder.CreateExtractValue(vector, 0u, "oldAllocationPtr"),
                oldVectorCapacity = builder.CreateExtractValue(vector, 2u, "oldVectorCapacity"),
                // TODO: ideally handle integer overflow; also there are ways this could be smarter
                newVectorCapacity = builder.CreateMul(oldVectorCapacity, compiler.Context.AsLLVMValue(2), "newVectorCapacity"),
                // TODO: handle the case where the allocation fails
                newAllocationPtr = builder.CreateArrayMalloc(elementLLVMType, newVectorCapacity, "newAllocationPtr"),
                oldVectorCapacityExtend = builder.CreateSExt(oldVectorCapacity, compiler.Context.Int64Type, "oldVectorCapacityExtend"),
                bytesToCopy = builder.CreateMul(oldVectorCapacityExtend, elementLLVMType.SizeOf(), "bytesToCopy");
            compiler.CreateCallToCopyMemory(builder, newAllocationPtr, oldAllocationPtr, bytesToCopy);
            LLVMValueRef newVector0 = builder.CreateInsertValue(vector, newAllocationPtr, 0u, "newVector0"),
                newVector = builder.CreateInsertValue(newVector0, newVectorCapacity, 2u, "newVector");
            builder.CreateStore(newVector, vectorPtr);
            builder.CreateFree(oldAllocationPtr);
            builder.CreateRetVoid();
            return vectorGrowFunction;
        }

        private static void BuildVectorRemoveLastFunction(FunctionCompiler compiler, NIType signature, LLVMValueRef vectorRemoveLastFunction)
        {
            NIType elementType = signature.GetGenericParameters().First();
            LLVMTypeRef elementLLVMType = compiler.Context.AsLLVMType(elementType),
                elementOptionLLVMType = compiler.Context.CreateLLVMOptionType(elementLLVMType);

            LLVMBasicBlockRef entryBlock = vectorRemoveLastFunction.AppendBasicBlock("entry"),
                hasElementsBlock = vectorRemoveLastFunction.AppendBasicBlock("hasElements"),
                noElementsBlock = vectorRemoveLastFunction.AppendBasicBlock("noElements"),
                endBlock = vectorRemoveLastFunction.AppendBasicBlock("end");
            var builder = compiler.Context.CreateIRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef vectorPtr = vectorRemoveLastFunction.GetParam(0u),
                vectorSizePtr = builder.CreateStructGEP(vectorPtr, 1u, "vectorSizePtr"),
                vectorSize = builder.CreateLoad(vectorSizePtr, "vectorSize"),
                optionElementPtr = vectorRemoveLastFunction.GetParam(1u),
                hasElements = builder.CreateICmp(LLVMIntPredicate.LLVMIntSGT, vectorSize, compiler.Context.AsLLVMValue(0), "hasElements");
            builder.CreateCondBr(hasElements, hasElementsBlock, noElementsBlock);

            builder.PositionBuilderAtEnd(hasElementsBlock);
            LLVMValueRef vectorAllocationPtrPtr = builder.CreateStructGEP(vectorPtr, 0u, "vectorAllocationPtrPtr"),
                vectorAllocationPtr = builder.CreateLoad(vectorAllocationPtrPtr, "vectorAllocationPtr"),
                lastIndex = builder.CreateSub(vectorSize, compiler.Context.AsLLVMValue(1), "lastIndex"),
                elementToRemovePtr = builder.CreateGEP(vectorAllocationPtr, new LLVMValueRef[] { lastIndex }, "elementToRemovePtr"),
                elementToRemove = builder.CreateLoad(elementToRemovePtr, "elementToRemove"),
                someElement = compiler.Context.BuildOptionValue(builder, elementOptionLLVMType, elementToRemove);
            builder.CreateStore(lastIndex, vectorSizePtr);
            builder.CreateBr(endBlock);

            builder.PositionBuilderAtEnd(noElementsBlock);
            LLVMValueRef noneElement = compiler.Context.BuildOptionValue(builder, elementOptionLLVMType, null);
            builder.CreateBr(endBlock);

            builder.PositionBuilderAtEnd(endBlock);
            LLVMValueRef optionElement = builder.CreatePhi(elementOptionLLVMType, "optionElement");
            optionElement.AddIncoming(someElement, hasElementsBlock);
            optionElement.AddIncoming(noneElement, noElementsBlock);
            builder.CreateStore(optionElement, optionElementPtr);
            builder.CreateRetVoid();
        }
    }
}
