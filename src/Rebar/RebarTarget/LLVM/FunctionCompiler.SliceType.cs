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
            LLVMTypeRef elementPtrOptionType = compiler.Context.CreateLLVMOptionType(LLVMTypeRef.PointerType(compiler.Context.AsLLVMType(elementType), 0u));

            LLVMBasicBlockRef entryBlock = sliceIndexFunction.AppendBasicBlock("entry"),
                validIndexBlock = sliceIndexFunction.AppendBasicBlock("validIndex"),
                invalidIndexBlock = sliceIndexFunction.AppendBasicBlock("invalidIndex");
            var builder = compiler.Context.CreateIRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef indexPtr = sliceIndexFunction.GetParam(0u),
                index = builder.CreateLoad(indexPtr, "index"),
                sliceRef = sliceIndexFunction.GetParam(1u),
                sliceLength = builder.CreateExtractValue(sliceRef, 1u, "sliceLength"),
                indexLessThanSliceLength = builder.CreateICmp(LLVMIntPredicate.LLVMIntSLT, index, sliceLength, "indexLTSliceLength"),
                indexNonNegative = builder.CreateICmp(LLVMIntPredicate.LLVMIntSGE, index, compiler.Context.AsLLVMValue(0), "indexNonNegative"),
                indexInBounds = builder.CreateAnd(indexLessThanSliceLength, indexNonNegative, "indexInBounds"),
                elementPtrOptionPtr = sliceIndexFunction.GetParam(2u);
            builder.CreateCondBr(indexInBounds, validIndexBlock, invalidIndexBlock);

            builder.PositionBuilderAtEnd(validIndexBlock);
            LLVMValueRef sliceBufferPtr = builder.CreateExtractValue(sliceRef, 0u, "sliceBufferPtr"),
                elementPtr = builder.CreateGEP(sliceBufferPtr, new LLVMValueRef[] { index }, "elementPtr"),
                someElementPtr = compiler.Context.BuildOptionValue(builder, elementPtrOptionType, elementPtr);
            builder.CreateStore(someElementPtr, elementPtrOptionPtr);
            builder.CreateRetVoid();

            builder.PositionBuilderAtEnd(invalidIndexBlock);
            LLVMValueRef noneElementPtr = compiler.Context.BuildOptionValue(builder, elementPtrOptionType, null);
            builder.CreateStore(noneElementPtr, elementPtrOptionPtr);
            builder.CreateRetVoid();
        }
    }
}
