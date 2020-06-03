using System.Linq;
using LLVMSharp;
using NationalInstruments.DataTypes;

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
    }
}
