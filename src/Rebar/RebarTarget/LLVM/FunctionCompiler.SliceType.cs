using System.Linq;
using LLVMSharp;
using NationalInstruments.DataTypes;
using Rebar.Common;

namespace Rebar.RebarTarget.LLVM
{
    internal partial class FunctionCompiler
    {
        internal static void CreateSliceIndexFunction(FunctionModuleContext moduleContext, NIType signature, LLVMValueRef sliceIndexFunction)
        {
            NIType elementType = signature.GetGenericParameters().First();
            LLVMTypeRef elementPtrOptionType = moduleContext.LLVMContext.CreateLLVMOptionType(LLVMTypeRef.PointerType(moduleContext.LLVMContext.AsLLVMType(elementType), 0u));

            LLVMBasicBlockRef entryBlock = sliceIndexFunction.AppendBasicBlock("entry"),
                validIndexBlock = sliceIndexFunction.AppendBasicBlock("validIndex"),
                invalidIndexBlock = sliceIndexFunction.AppendBasicBlock("invalidIndex");
            var builder = moduleContext.LLVMContext.CreateIRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef indexPtr = sliceIndexFunction.GetParam(0u),
                index = builder.CreateLoad(indexPtr, "index"),
                sliceRef = sliceIndexFunction.GetParam(1u),
                sliceLength = builder.CreateExtractValue(sliceRef, 1u, "sliceLength"),
                indexLessThanSliceLength = builder.CreateICmp(LLVMIntPredicate.LLVMIntSLT, index, sliceLength, "indexLTSliceLength"),
                indexNonNegative = builder.CreateICmp(LLVMIntPredicate.LLVMIntSGE, index, moduleContext.LLVMContext.AsLLVMValue(0), "indexNonNegative"),
                indexInBounds = builder.CreateAnd(indexLessThanSliceLength, indexNonNegative, "indexInBounds"),
                elementPtrOptionPtr = sliceIndexFunction.GetParam(2u);
            builder.CreateCondBr(indexInBounds, validIndexBlock, invalidIndexBlock);

            builder.PositionBuilderAtEnd(validIndexBlock);
            LLVMValueRef sliceBufferPtr = builder.CreateExtractValue(sliceRef, 0u, "sliceBufferPtr"),
                elementPtr = builder.CreateGEP(sliceBufferPtr, new LLVMValueRef[] { index }, "elementPtr"),
                someElementPtr = moduleContext.LLVMContext.BuildOptionValue(builder, elementPtrOptionType, elementPtr);
            builder.CreateStore(someElementPtr, elementPtrOptionPtr);
            builder.CreateRetVoid();

            builder.PositionBuilderAtEnd(invalidIndexBlock);
            LLVMValueRef noneElementPtr = moduleContext.LLVMContext.BuildOptionValue(builder, elementPtrOptionType, null);
            builder.CreateStore(noneElementPtr, elementPtrOptionPtr);
            builder.CreateRetVoid();
        }

        internal static void CreateSliceToIteratorFunction(FunctionModuleContext moduleContext, NIType signature, LLVMValueRef sliceToIteratorFunction)
        {
            LLVMBasicBlockRef entryBlock = sliceToIteratorFunction.AppendBasicBlock("entry");
            var builder = new IRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef sliceRef = sliceToIteratorFunction.GetParam(0u),
                sliceIteratorPtr = sliceToIteratorFunction.GetParam(1u),
                sliceIterator = builder.BuildStructValue(
                    sliceIteratorPtr.TypeOf().GetElementType(),
                    new LLVMValueRef[] { sliceRef, moduleContext.LLVMContext.AsLLVMValue(0) },
                    "sliceIterator");
            builder.CreateStore(sliceIterator, sliceIteratorPtr);
            builder.CreateRetVoid();
        }

        internal static void CreateSliceIteratorNextFunction(FunctionModuleContext moduleContext, NIType iteratorNextSignature, LLVMValueRef sliceToIteratorFunction)
        {
            NIType elementType = iteratorNextSignature.GetGenericParameters().First().GetGenericParameters().First();
            NIType sliceIndexSignature = Signatures.SliceIndexType.ReplaceGenericParameters(elementType, NIType.Unset, NIType.Unset, NIType.Unset);
            LLVMBasicBlockRef entryBlock = sliceToIteratorFunction.AppendBasicBlock("entry");
            var builder = new IRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            // For now, just call SliceToIndex and increment the index
            LLVMValueRef sliceIteratorRef = sliceToIteratorFunction.GetParam(0u),
                optionItemPtr = sliceToIteratorFunction.GetParam(1u),
                sliceRefPtr = builder.CreateStructGEP(sliceIteratorRef, 0u, "sliceRefPtr"),
                sliceRef = builder.CreateLoad(sliceRefPtr, "sliceRef"),
                indexPtr = builder.CreateStructGEP(sliceIteratorRef, 1u, "indexPtr"),
                sliceIndexFunction = moduleContext.GetSpecializedFunctionWithSignature(sliceIndexSignature, CreateSliceIndexFunction);
            builder.CreateCall(sliceIndexFunction, new LLVMValueRef[] { indexPtr, sliceRef, optionItemPtr }, string.Empty);
            LLVMValueRef index = builder.CreateLoad(indexPtr, "index"),
                incrementedIndex = builder.CreateAdd(index, moduleContext.LLVMContext.AsLLVMValue(1), "incrementedIndex");
            builder.CreateStore(incrementedIndex, indexPtr);
            builder.CreateRetVoid();
        }
    }
}
