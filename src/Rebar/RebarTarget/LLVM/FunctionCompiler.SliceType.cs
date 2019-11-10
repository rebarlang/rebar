using System.Linq;
using LLVMSharp;
using NationalInstruments.DataTypes;

namespace Rebar.RebarTarget.LLVM
{
    internal partial class FunctionCompiler
    {
        private static void CreateSliceIndexFunction(FunctionCompiler compiler, NIType signature, LLVMValueRef sliceIndexFunction)
        {
            NIType elementType = signature.GetGenericParameters().First();
            LLVMTypeRef elementPtrOptionType = LLVMTypeRef.PointerType(elementType.AsLLVMType(), 0u).CreateLLVMOptionType();

            LLVMBasicBlockRef entryBlock = sliceIndexFunction.AppendBasicBlock("entry"),
                validIndexBlock = sliceIndexFunction.AppendBasicBlock("validIndex"),
                invalidIndexBlock = sliceIndexFunction.AppendBasicBlock("invalidIndex");
            var builder = new IRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef indexPtr = sliceIndexFunction.GetParam(0u),
                index = builder.CreateLoad(indexPtr, "index"),
                sliceRef = sliceIndexFunction.GetParam(1u),
                sliceLength = builder.CreateExtractValue(sliceRef, 1u, "sliceLength"),
                indexLessThanSliceLength = builder.CreateICmp(LLVMIntPredicate.LLVMIntSLT, index, sliceLength, "indexLTSliceLength"),
                indexNonNegative = builder.CreateICmp(LLVMIntPredicate.LLVMIntSGE, index, 0.AsLLVMValue(), "indexNonNegative"),
                indexInBounds = builder.CreateAnd(indexLessThanSliceLength, indexNonNegative, "indexInBounds"),
                elementPtrOptionPtr = sliceIndexFunction.GetParam(2u);
            builder.CreateCondBr(indexInBounds, validIndexBlock, invalidIndexBlock);

            builder.PositionBuilderAtEnd(validIndexBlock);
            LLVMValueRef sliceBufferPtr = builder.CreateExtractValue(sliceRef, 0u, "sliceBufferPtr"),
                elementPtr = builder.CreateGEP(sliceBufferPtr, new LLVMValueRef[] { index }, "elementPtr"),
                someElementPtr = builder.BuildOptionValue(elementPtrOptionType, elementPtr);
            builder.CreateStore(someElementPtr, elementPtrOptionPtr);
            builder.CreateRetVoid();

            builder.PositionBuilderAtEnd(invalidIndexBlock);
            LLVMValueRef noneElementPtr = builder.BuildOptionValue(elementPtrOptionType, null);
            builder.CreateStore(noneElementPtr, elementPtrOptionPtr);
            builder.CreateRetVoid();
        }
    }
}
