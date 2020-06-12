using System.Linq;
using LLVMSharp;
using NationalInstruments;
using NationalInstruments.DataTypes;
using Rebar.Common;

namespace Rebar.RebarTarget.LLVM
{
    internal partial class FunctionCompiler
    {
        internal static void BuildVectorCreateFunction(FunctionModuleContext moduleContext, NIType signature, LLVMValueRef vectorCreateFunction)
        {
            LLVMTypeRef elementLLVMType = moduleContext.LLVMContext.AsLLVMType(signature.GetGenericParameters().First());

            LLVMBasicBlockRef entryBlock = vectorCreateFunction.AppendBasicBlock("entry");
            var builder = moduleContext.LLVMContext.CreateIRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef vectorPtr = vectorCreateFunction.GetParam(0u),
                vectorCapacity = moduleContext.LLVMContext.AsLLVMValue(4),
                allocationPtr = moduleContext.CreateArrayMalloc(builder, elementLLVMType, vectorCapacity, "allocationPtr"),
                vector = builder.BuildStructValue(
                    moduleContext.LLVMContext.CreateLLVMVectorType(elementLLVMType),
                    new LLVMValueRef[] { allocationPtr, moduleContext.LLVMContext.AsLLVMValue(0), vectorCapacity },
                    "vector");
            builder.CreateStore(vector, vectorPtr);
            builder.CreateRetVoid();
        }

        internal static void BuildVectorInitializeFunction(FunctionModuleContext moduleContext, NIType signature, LLVMValueRef vectorInitializeFunction)
        {
            LLVMTypeRef elementLLVMType = moduleContext.LLVMContext.AsLLVMType(signature.GetGenericParameters().First());
            LLVMTypeRef vectorLLVMType = moduleContext.LLVMContext.CreateLLVMVectorType(elementLLVMType);

            LLVMBasicBlockRef entryBlock = vectorInitializeFunction.AppendBasicBlock("entry");
            var builder = moduleContext.LLVMContext.CreateIRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef element = vectorInitializeFunction.GetParam(0u),
                size = vectorInitializeFunction.GetParam(1u),
                vectorPtr = vectorInitializeFunction.GetParam(2u),
                allocationPtr = moduleContext.CreateArrayMalloc(builder, elementLLVMType, size, "allocationPtr");
            LLVMBasicBlockRef loopStartBlock = vectorInitializeFunction.AppendBasicBlock("loopStart"),
                loopBodyBlock = vectorInitializeFunction.AppendBasicBlock("loopBody"),
                loopEndBlock = vectorInitializeFunction.AppendBasicBlock("loopEnd");
            builder.CreateBr(loopStartBlock);

            builder.PositionBuilderAtEnd(loopStartBlock);
            LLVMValueRef index = builder.CreatePhi(moduleContext.LLVMContext.Int32Type, "index");
            LLVMValueRef indexLessThanSize = builder.CreateICmp(LLVMIntPredicate.LLVMIntSLT, index, size, "indexLessThanSize");
            builder.CreateCondBr(indexLessThanSize, loopBodyBlock, loopEndBlock);

            builder.PositionBuilderAtEnd(loopBodyBlock);
            LLVMValueRef vectorIndexPtr = builder.CreateGEP(allocationPtr, new LLVMValueRef[] { index }, "vectorIndexPtr");
            builder.CreateStore(element, vectorIndexPtr);
            LLVMValueRef incrementIndex = builder.CreateAdd(index, moduleContext.LLVMContext.AsLLVMValue(1), "incrementIndex");
            builder.CreateBr(loopStartBlock);

            builder.PositionBuilderAtEnd(loopEndBlock);
            LLVMValueRef vector = builder.BuildStructValue(
                vectorLLVMType,
                new LLVMValueRef[] { allocationPtr, size, size },
                "vector");
            builder.CreateStore(vector, vectorPtr);
            builder.CreateRetVoid();

            index.AddIncoming(moduleContext.LLVMContext.AsLLVMValue(0), entryBlock);
            index.AddIncoming(incrementIndex, loopBodyBlock);
        }

        internal static void BuildVectorCloneFunction(FunctionModuleContext moduleContext, NIType signature, LLVMValueRef vectorCloneFunction)
        {
            NIType elementType;
            signature.GetGenericParameters().First().TryDestructureVectorType(out elementType);
            LLVMTypeRef elementLLVMType = moduleContext.LLVMContext.AsLLVMType(elementType);

            LLVMBasicBlockRef entryBlock = vectorCloneFunction.AppendBasicBlock("entry");
            var builder = moduleContext.LLVMContext.CreateIRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef existingVectorPtr = vectorCloneFunction.GetParam(0u),
                existingVector = builder.CreateLoad(existingVectorPtr, "existingVector"),
                existingVectorAllocationPtr = builder.CreateExtractValue(existingVector, 0u, "existingVectorAllocationPtr"),
                existingVectorSize = builder.CreateExtractValue(existingVector, 1u, "existingVectorSize"),
                existingVectorCapacity = builder.CreateExtractValue(existingVector, 2u, "existingVectorCapacity"),
                newVectorAllocationPtr = moduleContext.CreateArrayMalloc(builder, elementLLVMType, existingVectorCapacity, "newVectorAllocationPtr");

            LLVMValueRef elementCloneFunction;
            if (moduleContext.TryGetCloneFunction(elementType, out elementCloneFunction))
            {
                LLVMBasicBlockRef loopStartBlock = vectorCloneFunction.AppendBasicBlock("loopStart"),
                    loopBodyBlock = vectorCloneFunction.AppendBasicBlock("loopBody"),
                    loopEndBlock = vectorCloneFunction.AppendBasicBlock("loopEnd");
                builder.CreateBr(loopStartBlock);

                builder.PositionBuilderAtEnd(loopStartBlock);
                LLVMValueRef index = builder.CreatePhi(moduleContext.LLVMContext.Int32Type, "index");
                LLVMValueRef indexLessThanSize = builder.CreateICmp(LLVMIntPredicate.LLVMIntSLT, index, existingVectorSize, "indexLessThanSize");
                builder.CreateCondBr(indexLessThanSize, loopBodyBlock, loopEndBlock);

                builder.PositionBuilderAtEnd(loopBodyBlock);
                LLVMValueRef existingElementPtr = builder.CreateGEP(existingVectorAllocationPtr, new LLVMValueRef[] { index }, "existingElementPtr"),
                    newElementPtr = builder.CreateGEP(newVectorAllocationPtr, new LLVMValueRef[] { index }, "newElementPtr");
                builder.CreateCall(elementCloneFunction, new LLVMValueRef[] { existingElementPtr, newElementPtr }, string.Empty);
                LLVMValueRef incrementIndex = builder.CreateAdd(index, moduleContext.LLVMContext.AsLLVMValue(1), "incrementIndex");
                builder.CreateBr(loopStartBlock);

                index.AddIncoming(moduleContext.LLVMContext.AsLLVMValue(0), entryBlock);
                index.AddIncoming(incrementIndex, loopBodyBlock);

                builder.PositionBuilderAtEnd(loopEndBlock);
            }
            else
            {
                LLVMValueRef existingVectorSizeExtend = builder.CreateSExt(existingVectorSize, moduleContext.LLVMContext.Int64Type, "existingVectorSizeExtend"),
                    bytesToCopy = builder.CreateMul(existingVectorSizeExtend, elementLLVMType.SizeOf(), "bytesToCopy");
                moduleContext.CreateCallToCopyMemory(builder, newVectorAllocationPtr, existingVectorAllocationPtr, bytesToCopy);
            }

            LLVMValueRef newVector = builder.CreateInsertValue(existingVector, newVectorAllocationPtr, 0u, "newVector"),
                newVectorPtr = vectorCloneFunction.GetParam(1u);
            builder.CreateStore(newVector, newVectorPtr);
            builder.CreateRetVoid();
        }

        internal static void BuildVectorDropFunction(FunctionModuleContext moduleContext, NIType signature, LLVMValueRef vectorDropFunction)
        {
            NIType elementType;
            signature.GetGenericParameters().First().TryDestructureVectorType(out elementType);

            LLVMBasicBlockRef entryBlock = vectorDropFunction.AppendBasicBlock("entry");
            var builder = moduleContext.LLVMContext.CreateIRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef vectorPtr = vectorDropFunction.GetParam(0u),
                vectorAllocationPtrPtr = builder.CreateStructGEP(vectorPtr, 0u, "vectorAllocationPtrPtr"),
                vectorAllocationPtr = builder.CreateLoad(vectorAllocationPtrPtr, "vectorAllocationPtr");

            LLVMValueRef elementDropFunction;
            if (TraitHelpers.TryGetDropFunction(elementType, moduleContext, out elementDropFunction))
            {
                LLVMValueRef vectorSizePtr = builder.CreateStructGEP(vectorPtr, 1u, "vectorSizePtr"),
                    vectorSize = builder.CreateLoad(vectorSizePtr, "vectorSize");
                LLVMBasicBlockRef loopStartBlock = vectorDropFunction.AppendBasicBlock("loopStart"),
                    loopBodyBlock = vectorDropFunction.AppendBasicBlock("loopBody"),
                    loopEndBlock = vectorDropFunction.AppendBasicBlock("loopEnd");
                builder.CreateBr(loopStartBlock);

                builder.PositionBuilderAtEnd(loopStartBlock);
                LLVMValueRef index = builder.CreatePhi(moduleContext.LLVMContext.Int32Type, "index");
                LLVMValueRef indexLessThanSize = builder.CreateICmp(LLVMIntPredicate.LLVMIntSLT, index, vectorSize, "indexLessThanSize");
                builder.CreateCondBr(indexLessThanSize, loopBodyBlock, loopEndBlock);

                builder.PositionBuilderAtEnd(loopBodyBlock);
                LLVMValueRef elementPtr = builder.CreateGEP(vectorAllocationPtr, new LLVMValueRef[] { index }, "elementPtr");
                builder.CreateCall(elementDropFunction, new LLVMValueRef[] { elementPtr }, string.Empty);
                LLVMValueRef incrementIndex = builder.CreateAdd(index, moduleContext.LLVMContext.AsLLVMValue(1), "incrementIndex");
                builder.CreateBr(loopStartBlock);

                index.AddIncoming(moduleContext.LLVMContext.AsLLVMValue(0), entryBlock);
                index.AddIncoming(incrementIndex, loopBodyBlock);

                builder.PositionBuilderAtEnd(loopEndBlock);
            }

            builder.CreateFree(vectorAllocationPtr);
            builder.CreateRetVoid();
        }

        internal static void BuildVectorToSliceFunction(FunctionModuleContext moduleContext, NIType signature, LLVMValueRef vectorToSliceFunction)
        {
            LLVMTypeRef sliceElementType = moduleContext.LLVMContext.AsLLVMType(signature.GetGenericParameters().First());
            LLVMTypeRef sliceReferenceType = moduleContext.LLVMContext.CreateLLVMSliceReferenceType(sliceElementType);

            LLVMBasicBlockRef entryBlock = vectorToSliceFunction.AppendBasicBlock("entry");
            var builder = moduleContext.LLVMContext.CreateIRBuilder();
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

        internal static void BuildVectorAppendFunction(FunctionModuleContext moduleContext, NIType signature, LLVMValueRef vectorAppendFunction)
        {
            NIType elementType = signature.GetGenericParameters().First();

            LLVMBasicBlockRef entryBlock = vectorAppendFunction.AppendBasicBlock("entry"),
                growBlock = vectorAppendFunction.AppendBasicBlock("grow"),
                appendBlock = vectorAppendFunction.AppendBasicBlock("append");
            var builder = moduleContext.LLVMContext.CreateIRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef vectorPtr = vectorAppendFunction.GetParam(0u),
                vectorSizePtr = builder.CreateStructGEP(vectorPtr, 1u, "vectorSizePtr"),
                vectorSize = builder.CreateLoad(vectorSizePtr, "vectorSize"),
                vectorCapacityPtr = builder.CreateStructGEP(vectorPtr, 2u, "vectorCapacityPtr"),
                vectorCapacity = builder.CreateLoad(vectorCapacityPtr, "vectorCapacity"),
                vectorIsFull = builder.CreateICmp(LLVMIntPredicate.LLVMIntEQ, vectorSize, vectorCapacity, "vectorIsFull");
            builder.CreateCondBr(vectorIsFull, growBlock, appendBlock);

            builder.PositionBuilderAtEnd(growBlock);
            string specializedName = FunctionNames.MonomorphizeFunctionName("vector_grow", elementType.ToEnumerable());
            LLVMValueRef vectorGrowFunction = moduleContext.FunctionImporter.GetCachedFunction(
                specializedName,
                () => CreateVectorGrowFunction(moduleContext, specializedName, elementType));
            builder.CreateCall(vectorGrowFunction, new LLVMValueRef[] { vectorPtr }, string.Empty);
            builder.CreateBr(appendBlock);

            builder.PositionBuilderAtEnd(appendBlock);
            LLVMValueRef vectorAllocationPtrPtr = builder.CreateStructGEP(vectorPtr, 0u, "vectorAllocationPtrPtr"),
                vectorAllocationPtr = builder.CreateLoad(vectorAllocationPtrPtr, "vectorAllocationPtr"),
                elementPtr = builder.CreateGEP(vectorAllocationPtr, new LLVMValueRef[] { vectorSize }, "elementPtr"),
                incrementedSize = builder.CreateAdd(vectorSize, moduleContext.LLVMContext.AsLLVMValue(1), "incrementedSize");
            builder.CreateStore(vectorAppendFunction.GetParam(1u), elementPtr);
            builder.CreateStore(incrementedSize, vectorSizePtr);
            builder.CreateRetVoid();
        }

        private static LLVMValueRef CreateVectorGrowFunction(FunctionModuleContext moduleContext, string functionName, NIType elementType)
        {
            LLVMTypeRef elementLLVMType = moduleContext.LLVMContext.AsLLVMType(elementType);
            LLVMTypeRef vectorGrowFunctionType = LLVMTypeRef.FunctionType(
                moduleContext.LLVMContext.VoidType,
                new LLVMTypeRef[]
                {
                    LLVMTypeRef.PointerType(moduleContext.LLVMContext.CreateLLVMVectorType(elementLLVMType), 0u),
                },
                false);

            LLVMValueRef vectorGrowFunction = moduleContext.Module.AddFunction(functionName, vectorGrowFunctionType);
            LLVMBasicBlockRef entryBlock = vectorGrowFunction.AppendBasicBlock("entry");
            var builder = moduleContext.LLVMContext.CreateIRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef vectorPtr = vectorGrowFunction.GetParam(0u),
                vector = builder.CreateLoad(vectorPtr, "vector"),
                oldAllocationPtr = builder.CreateExtractValue(vector, 0u, "oldAllocationPtr"),
                oldVectorCapacity = builder.CreateExtractValue(vector, 2u, "oldVectorCapacity"),
                // TODO: ideally handle integer overflow; also there are ways this could be smarter
                newVectorCapacity = builder.CreateMul(oldVectorCapacity, moduleContext.LLVMContext.AsLLVMValue(2), "newVectorCapacity"),
                // TODO: handle the case where the allocation fails
                newAllocationPtr = moduleContext.CreateArrayMalloc(builder, elementLLVMType, newVectorCapacity, "newAllocationPtr"),
                oldVectorCapacityExtend = builder.CreateSExt(oldVectorCapacity, moduleContext.LLVMContext.Int64Type, "oldVectorCapacityExtend"),
                bytesToCopy = builder.CreateMul(oldVectorCapacityExtend, elementLLVMType.SizeOf(), "bytesToCopy");
            moduleContext.CreateCallToCopyMemory(builder, newAllocationPtr, oldAllocationPtr, bytesToCopy);
            LLVMValueRef newVector0 = builder.CreateInsertValue(vector, newAllocationPtr, 0u, "newVector0"),
                newVector = builder.CreateInsertValue(newVector0, newVectorCapacity, 2u, "newVector");
            builder.CreateStore(newVector, vectorPtr);
            builder.CreateFree(oldAllocationPtr);
            builder.CreateRetVoid();
            return vectorGrowFunction;
        }

        internal static void BuildVectorRemoveLastFunction(FunctionModuleContext moduleContext, NIType signature, LLVMValueRef vectorRemoveLastFunction)
        {
            NIType elementType = signature.GetGenericParameters().First();
            LLVMTypeRef elementLLVMType = moduleContext.LLVMContext.AsLLVMType(elementType),
                elementOptionLLVMType = moduleContext.LLVMContext.CreateLLVMOptionType(elementLLVMType);

            LLVMBasicBlockRef entryBlock = vectorRemoveLastFunction.AppendBasicBlock("entry"),
                hasElementsBlock = vectorRemoveLastFunction.AppendBasicBlock("hasElements"),
                noElementsBlock = vectorRemoveLastFunction.AppendBasicBlock("noElements"),
                endBlock = vectorRemoveLastFunction.AppendBasicBlock("end");
            var builder = moduleContext.LLVMContext.CreateIRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef vectorPtr = vectorRemoveLastFunction.GetParam(0u),
                vectorSizePtr = builder.CreateStructGEP(vectorPtr, 1u, "vectorSizePtr"),
                vectorSize = builder.CreateLoad(vectorSizePtr, "vectorSize"),
                optionElementPtr = vectorRemoveLastFunction.GetParam(1u),
                hasElements = builder.CreateICmp(LLVMIntPredicate.LLVMIntSGT, vectorSize, moduleContext.LLVMContext.AsLLVMValue(0), "hasElements");
            builder.CreateCondBr(hasElements, hasElementsBlock, noElementsBlock);

            builder.PositionBuilderAtEnd(hasElementsBlock);
            LLVMValueRef vectorAllocationPtrPtr = builder.CreateStructGEP(vectorPtr, 0u, "vectorAllocationPtrPtr"),
                vectorAllocationPtr = builder.CreateLoad(vectorAllocationPtrPtr, "vectorAllocationPtr"),
                lastIndex = builder.CreateSub(vectorSize, moduleContext.LLVMContext.AsLLVMValue(1), "lastIndex"),
                elementToRemovePtr = builder.CreateGEP(vectorAllocationPtr, new LLVMValueRef[] { lastIndex }, "elementToRemovePtr"),
                elementToRemove = builder.CreateLoad(elementToRemovePtr, "elementToRemove"),
                someElement = moduleContext.LLVMContext.BuildOptionValue(builder, elementOptionLLVMType, elementToRemove);
            builder.CreateStore(lastIndex, vectorSizePtr);
            builder.CreateBr(endBlock);

            builder.PositionBuilderAtEnd(noElementsBlock);
            LLVMValueRef noneElement = moduleContext.LLVMContext.BuildOptionValue(builder, elementOptionLLVMType, null);
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
