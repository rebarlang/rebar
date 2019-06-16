using System.Collections.Generic;
using LLVMSharp;

namespace Rebar.RebarTarget.LLVM
{
    internal static class CommonModules
    {
        public static Module StringModule { get; }
        public static Module RangeModule { get; }

        public static Dictionary<string, LLVMTypeRef> CommonModuleSignatures { get; }

        private static LLVMValueRef _copySliceToPointerFunction;
        private static LLVMValueRef _stringFromSliceRetFunction;

        public const string CopySliceToPointerName = "copy_slice_to_pointer";
        public const string OutputStringSliceName = "output_string_slice";
        public const string StringFromSliceName = "string_from_slice";
        public const string StringToSliceRetName = "string_to_slice_ret";
        public const string StringToSliceName = "string_to_slice";
        public const string StringAppendName = "string_append";
        public const string StringConcatName = "string_concat";

        public const string RangeIteratorNextName = "range_iterator_next";
        public const string CreateRangeIteratorName = "create_range_iterator";

        static CommonModules()
        {
            CommonModuleSignatures = new Dictionary<string, LLVMTypeRef>();

            StringModule = new Module("string");
            CreateStringModule(StringModule);
            RangeModule = new Module("range");
            CreateRangeModule(RangeModule);
        }

        #region String Module

        private static void CreateStringModule(Module stringModule)
        {
            var externalFunctions = new CommonExternalFunctions(stringModule);

            CommonModuleSignatures[CopySliceToPointerName] = LLVMSharp.LLVM.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[] { LLVMExtensions.StringSliceReferenceType, LLVMTypeRef.PointerType(LLVMTypeRef.Int8Type(), 0) },
                false);
            BuildCopySliceToPointerFunction(stringModule, externalFunctions);

            CommonModuleSignatures[OutputStringSliceName] = LLVMSharp.LLVM.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[] { LLVMExtensions.StringSliceReferenceType },
                false);
            BuildOutputStringSliceFunction(stringModule, externalFunctions);

            CommonModuleSignatures[StringFromSliceName] = LLVMSharp.LLVM.FunctionType(
                LLVMSharp.LLVM.VoidType(), 
                new LLVMTypeRef[] { LLVMExtensions.StringSliceReferenceType, LLVMTypeRef.PointerType(LLVMExtensions.StringType, 0) }, 
                false);
            BuildStringFromSliceFunction(stringModule, externalFunctions);

            CommonModuleSignatures[StringToSliceRetName] = LLVMSharp.LLVM.FunctionType(
                LLVMExtensions.StringSliceReferenceType,
                new LLVMTypeRef[] { LLVMTypeRef.PointerType(LLVMExtensions.StringType, 0), },
                false);
            BuildStringToSliceRetFunction(stringModule);

            CommonModuleSignatures[StringToSliceName] = LLVMSharp.LLVM.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[] { LLVMTypeRef.PointerType(LLVMExtensions.StringType, 0), LLVMTypeRef.PointerType(LLVMExtensions.StringSliceReferenceType, 0) },
                false);
            BuildStringToSliceFunction(stringModule);

            CommonModuleSignatures[StringAppendName] = LLVMSharp.LLVM.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[] { LLVMTypeRef.PointerType(LLVMExtensions.StringType, 0), LLVMExtensions.StringSliceReferenceType },
                false);
            BuildStringAppendFunction(stringModule, externalFunctions);

            CommonModuleSignatures[StringConcatName] = LLVMSharp.LLVM.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[] { LLVMExtensions.StringSliceReferenceType, LLVMExtensions.StringSliceReferenceType, LLVMTypeRef.PointerType(LLVMExtensions.StringType, 0) },
                false);
            BuildStringConcatFunction(stringModule, externalFunctions);

            stringModule.VerifyAndThrowIfInvalid();
        }

        private static void BuildOutputStringSliceFunction(Module stringModule, CommonExternalFunctions externalFunctions)
        {
            LLVMValueRef outputStringSliceFunction = stringModule.AddFunction(OutputStringSliceName, CommonModuleSignatures[OutputStringSliceName]);
            LLVMBasicBlockRef entryBlock = outputStringSliceFunction.AppendBasicBlock("entry");
            var builder = new IRBuilder();
            builder.PositionBuilderAtEnd(entryBlock);

            LLVMValueRef stringSlice = outputStringSliceFunction.GetParam(0u);
            builder.CreateCall(
                externalFunctions.OutputStringFunction,
                new LLVMValueRef[]
                {
                    builder.CreateExtractValue(stringSlice, 0u, "stringBufferPtr"),
                    builder.CreateExtractValue(stringSlice, 1u, "stringSize")
                }, 
                string.Empty);
            builder.CreateRetVoid();
        }

        private static void BuildStringFromSliceFunction(Module stringModule, CommonExternalFunctions externalFunctions)
        {
            LLVMValueRef stringFromSliceFunction = stringModule.AddFunction(StringFromSliceName, CommonModuleSignatures[StringFromSliceName]);
            LLVMBasicBlockRef entryBlock = stringFromSliceFunction.AppendBasicBlock("entry");
            var builder = new IRBuilder();
            builder.PositionBuilderAtEnd(entryBlock);

            LLVMValueRef stringSliceReference = stringFromSliceFunction.GetParam(0u),
                stringPtr = stringFromSliceFunction.GetParam(1u),
                // sliceAllocationPtrPtr = builder.CreateStructGEP(stringSliceReference, 0u, "sliceAllocationPtrPtr"),
                // sliceSizePtr = builder.CreateStructGEP(stringSliceReference, 1u, "sliceSizePtr"),
                stringAllocationPtrPtr = builder.CreateStructGEP(stringPtr, 0u, "stringAllocationPtrPtr"),
                stringSizePtr = builder.CreateStructGEP(stringPtr, 1u, "stringSizePtr");

            LLVMValueRef sliceAllocationPtr = builder.CreateExtractValue(stringSliceReference, 0u, "sliceAllocationPtr");
            LLVMValueRef sliceSize = builder.CreateExtractValue(stringSliceReference, 1u, "sliceSize");

            // Get a pointer to a heap allocation big enough for the string
            LLVMValueRef allocationPtr = builder.CreateArrayMalloc(LLVMTypeRef.Int8Type(), sliceSize, "allocationPtr");
            builder.CreateStore(allocationPtr, stringAllocationPtrPtr);

            // Copy the data from the string slice to the heap allocation
            LLVMValueRef sizeExtend = builder.CreateSExt(sliceSize, LLVMTypeRef.Int64Type(), "sizeExtend");
            builder.CreateCall(externalFunctions.CopyMemoryFunction, new LLVMValueRef[] { allocationPtr, sliceAllocationPtr, sizeExtend }, string.Empty);

            // Copy actual size into string handle
            builder.CreateStore(sliceSize, stringSizePtr);

            builder.CreateRetVoid();
        }

        private static void BuildStringToSliceRetFunction(Module stringModule)
        {
            _stringFromSliceRetFunction = stringModule.AddFunction(StringToSliceRetName, CommonModuleSignatures[StringToSliceRetName]);
            LLVMBasicBlockRef entryBlock = _stringFromSliceRetFunction.AppendBasicBlock("entry");
            var builder = new IRBuilder();
            builder.PositionBuilderAtEnd(entryBlock);

            LLVMValueRef stringPtr = _stringFromSliceRetFunction.GetParam(0u),
                stringAllocationPtrPtr = builder.CreateStructGEP(stringPtr, 0u, "stringAllocationPtrPtr"),
                stringSizePtr = builder.CreateStructGEP(stringPtr, 1u, "stringSizePtr"),
                stringAllocationPtr = builder.CreateLoad(stringAllocationPtrPtr, "stringAllocationPtr"),
                stringSize = builder.CreateLoad(stringSizePtr, "stringSize"),
                slice0 = builder.CreateInsertValue(LLVMSharp.LLVM.GetUndef(LLVMExtensions.StringSliceReferenceType), stringAllocationPtr, 0u, "slice0"),
                slice1 = builder.CreateInsertValue(slice0, stringSize, 1u, "slice1");
            builder.CreateRet(slice1);
        }

        private static void BuildStringToSliceFunction(Module stringModule)
        {
            LLVMValueRef stringToSliceFunction = stringModule.AddFunction(StringToSliceName, CommonModuleSignatures[StringToSliceName]);
            LLVMBasicBlockRef entryBlock = stringToSliceFunction.AppendBasicBlock("entry");
            var builder = new IRBuilder();
            builder.PositionBuilderAtEnd(entryBlock);

            LLVMValueRef stringPtr = stringToSliceFunction.GetParam(0u),
                stringSliceReferencePtr = stringToSliceFunction.GetParam(1u);
            LLVMValueRef sliceReference = builder.CreateCall(_stringFromSliceRetFunction, new LLVMValueRef[] { stringPtr }, "sliceReference");
            builder.CreateStore(sliceReference, stringSliceReferencePtr);
            builder.CreateRetVoid();
        }

        private static void BuildStringAppendFunction(Module stringModule, CommonExternalFunctions externalFunctions)
        {
            LLVMValueRef stringAppendFunction = stringModule.AddFunction(StringAppendName, CommonModuleSignatures[StringAppendName]);
            LLVMBasicBlockRef entryBlock = stringAppendFunction.AppendBasicBlock("entry");
            var builder = new IRBuilder();
            builder.PositionBuilderAtEnd(entryBlock);

            LLVMValueRef stringPtr = stringAppendFunction.GetParam(0u),
                sliceReference = stringAppendFunction.GetParam(1u);

            // for now, always create a new allocation, rather than trying to use existing one
            // compute the new allocation size and allocate it
            LLVMValueRef stringAllocationPtrPtr = builder.CreateStructGEP(stringPtr, 0u, "stringAllocationPtrPtr"),
                stringSizePtr = builder.CreateStructGEP(stringPtr, 1u, "stringSizePtr"),
                stringAllocationPtr = builder.CreateLoad(stringAllocationPtrPtr, "stringAllocationPtr"),
                stringSize = builder.CreateLoad(stringSizePtr, "stringSize"),
                sliceSize = builder.CreateExtractValue(sliceReference, 1u, "sliceSize"),
                appendedSize = builder.CreateAdd(stringSize, sliceSize, "appendedSize"),
                newAllocationPtr = builder.CreateArrayMalloc(LLVMTypeRef.Int8Type(), appendedSize, "newAllocationPtr");

            // copy from old allocation to new allocation
            LLVMValueRef stringSlice = builder.CreateCall(_stringFromSliceRetFunction, new LLVMValueRef[] { stringPtr }, "stringSlice");
            builder.CreateCall(_copySliceToPointerFunction, new LLVMValueRef[] { stringSlice, newAllocationPtr }, string.Empty);

            // copy from slice to offset in new allocation
            LLVMValueRef newAllocationOffsetPtr = builder.CreateGEP(newAllocationPtr, new LLVMValueRef[] { stringSize }, "newAllocationOffsetPtr");
            builder.CreateCall(_copySliceToPointerFunction, new LLVMValueRef[] { sliceReference, newAllocationOffsetPtr }, string.Empty);

            // free old allocation and update string fields
            builder.CreateFree(stringAllocationPtr);
            builder.CreateStore(newAllocationPtr, stringAllocationPtrPtr);
            builder.CreateStore(appendedSize, stringSizePtr);

            builder.CreateRetVoid();
        }

        private static void BuildStringConcatFunction(Module stringModule, CommonExternalFunctions externalFunctions)
        {
            LLVMValueRef stringConcatFunction = stringModule.AddFunction(StringConcatName, CommonModuleSignatures[StringConcatName]);
            LLVMBasicBlockRef entryBlock = stringConcatFunction.AppendBasicBlock("entry");
            var builder = new IRBuilder();
            builder.PositionBuilderAtEnd(entryBlock);

            LLVMValueRef slice0 = stringConcatFunction.GetParam(0u),
                slice1 = stringConcatFunction.GetParam(1u),
                sliceSize0 = builder.CreateExtractValue(slice0, 1u, "sliceSize0"),
                sliceSize1 = builder.CreateExtractValue(slice1, 1u, "sliceSize1"),
                concatSize = builder.CreateAdd(sliceSize0, sliceSize1, "concatSize"),
                concatAllocationPtr = builder.CreateArrayMalloc(LLVMTypeRef.Int8Type(), concatSize, "concatAllocationPtr"),
                concatAllocationOffsetPtr = builder.CreateGEP(concatAllocationPtr, new LLVMValueRef[] { sliceSize0 }, "concatAllocationOffsetPtr"),
                stringPtr = stringConcatFunction.GetParam(2u),
                stringAllocationPtrPtr = builder.CreateStructGEP(stringPtr, 0u, "stringAllocationPtrPtr"),
                stringSizePtr = builder.CreateStructGEP(stringPtr, 1u, "stringSizePtr");
            builder.CreateCall(_copySliceToPointerFunction, new LLVMValueRef[] { slice0, concatAllocationPtr }, string.Empty);
            builder.CreateCall(_copySliceToPointerFunction, new LLVMValueRef[] { slice1, concatAllocationOffsetPtr }, string.Empty);
            builder.CreateStore(concatAllocationPtr, stringAllocationPtrPtr);
            builder.CreateStore(concatSize, stringSizePtr);
            builder.CreateRetVoid();
        }

        private static void BuildCopySliceToPointerFunction(Module stringModule, CommonExternalFunctions externalFunctions)
        {
            _copySliceToPointerFunction = stringModule.AddFunction(CopySliceToPointerName, CommonModuleSignatures[CopySliceToPointerName]);
            LLVMBasicBlockRef entryBlock = _copySliceToPointerFunction.AppendBasicBlock("entry");
            var builder = new IRBuilder();
            builder.PositionBuilderAtEnd(entryBlock);

            LLVMValueRef slice = _copySliceToPointerFunction.GetParam(0u),
                sourcePtr = builder.CreateExtractValue(slice, 0u, "sourcePtr"),
                size = builder.CreateExtractValue(slice, 1u, "size"),
                sizeExtend = builder.CreateSExt(size, LLVMTypeRef.Int64Type(), "sizeExtend"),
                destinationPtr = _copySliceToPointerFunction.GetParam(1u);
            builder.CreateCall(externalFunctions.CopyMemoryFunction, new LLVMValueRef[] { destinationPtr, sourcePtr, sizeExtend }, string.Empty);
            builder.CreateRetVoid();
        }

        #endregion

        #region Range Module

        private static void CreateRangeModule(Module rangeModule)
        {
            CommonModuleSignatures[RangeIteratorNextName] = LLVMSharp.LLVM.FunctionType(
                LLVMTypeRef.Int32Type().CreateLLVMOptionType(),
                new LLVMTypeRef[] 
                {
                    LLVMTypeRef.PointerType(LLVMExtensions.RangeIteratorType, 0)
                },
                false);
            BuildRangeIteratorNextFunction(rangeModule);

            CommonModuleSignatures[CreateRangeIteratorName] = LLVMSharp.LLVM.FunctionType(
                LLVMTypeRef.Int32Type().CreateLLVMOptionType(),
                new LLVMTypeRef[]
                {
                    LLVMTypeRef.Int32Type(),
                    LLVMTypeRef.Int32Type(),
                    LLVMTypeRef.PointerType(LLVMExtensions.RangeIteratorType, 0)
                },
                false);
            BuildCreateRangeIteratorFunction(rangeModule);
        }

        private static void BuildRangeIteratorNextFunction(Module rangeModule)
        {
            LLVMValueRef rangeIteratorNextFunction = rangeModule.AddFunction(RangeIteratorNextName, CommonModuleSignatures[RangeIteratorNextName]);
            LLVMBasicBlockRef entryBlock = rangeIteratorNextFunction.AppendBasicBlock("entry");
            var builder = new IRBuilder();
            builder.PositionBuilderAtEnd(entryBlock);

            LLVMValueRef rangeIteratorPtr = rangeIteratorNextFunction.GetParam(0u),
                rangeCurrentPtr = builder.CreateStructGEP(rangeIteratorPtr, 0u, "rangeCurrentPtr"),
                rangeHighPtr = builder.CreateStructGEP(rangeIteratorPtr, 1u, "rangeHighPtr"),
                rangeCurrent = builder.CreateLoad(rangeCurrentPtr, "rangeCurrent"),
                rangeHigh = builder.CreateLoad(rangeHighPtr, "rangeHigh"),
                rangeCurrentInc = builder.CreateAdd(rangeCurrent, 1.AsLLVMValue(), "rangeCurrentInc");
            builder.CreateStore(rangeCurrentInc, rangeCurrentPtr);
            LLVMValueRef inRange = builder.CreateICmp(LLVMIntPredicate.LLVMIntSLT, rangeCurrentInc, rangeHigh, "inRange");
            LLVMTypeRef optionType = LLVMTypeRef.Int32Type().CreateLLVMOptionType();
            LLVMValueRef option0 = builder.CreateInsertValue(LLVMSharp.LLVM.GetUndef(optionType), inRange, 0u, "option0"),
                option1 = builder.CreateInsertValue(option0, rangeCurrentInc, 1u, "option1");
            builder.CreateRet(option1);
        }

        private static void BuildCreateRangeIteratorFunction(Module rangeModule)
        {
            LLVMValueRef createRangeIteratorFunction = rangeModule.AddFunction(CreateRangeIteratorName, CommonModuleSignatures[CreateRangeIteratorName]);
            LLVMBasicBlockRef entryBlock = createRangeIteratorFunction.AppendBasicBlock("entry");
            var builder = new IRBuilder();
            builder.PositionBuilderAtEnd(entryBlock);

            LLVMValueRef lowValue = createRangeIteratorFunction.GetParam(0u),
                highValue = createRangeIteratorFunction.GetParam(1u),
                rangePtr = createRangeIteratorFunction.GetParam(2u),
                currentValuePtr = builder.CreateStructGEP(rangePtr, 0u, "currentValuePtr"),
                highValuePtr = builder.CreateStructGEP(rangePtr, 1u, "highValuePtr"),
                lowValueDecrement = builder.CreateSub(lowValue, 1.AsLLVMValue(), "lowValueDecrement");
            builder.CreateStore(lowValueDecrement, currentValuePtr);
            builder.CreateStore(highValue, highValuePtr);
            builder.CreateRetVoid();
        }

        #endregion
    }

    public class CommonExternalFunctions
    {
        public CommonExternalFunctions(Module addTo)
        {
            LLVMTypeRef bytePointerType = LLVMTypeRef.PointerType(LLVMTypeRef.Int8Type(), 0);

            // NB: this will get resolved to the Win32 RtlCopyMemory function.
            LLVMTypeRef copyMemoryFunctionType = LLVMSharp.LLVM.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[] { bytePointerType, bytePointerType, LLVMTypeRef.Int64Type() },
                false);
            CopyMemoryFunction = addTo.AddFunction("CopyMemory", copyMemoryFunctionType);
            CopyMemoryFunction.SetLinkage(LLVMLinkage.LLVMExternalLinkage);

            LLVMTypeRef outputIntFunctionType = LLVMSharp.LLVM.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[] { LLVMTypeRef.Int32Type() },
                false);
            OutputIntFunction = addTo.AddFunction("output_int", outputIntFunctionType);
            OutputIntFunction.SetLinkage(LLVMLinkage.LLVMExternalLinkage);

            LLVMTypeRef outputStringFunctionType = LLVMSharp.LLVM.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[] { bytePointerType, LLVMTypeRef.Int32Type() },
                false);
            OutputStringFunction = addTo.AddFunction("output_string", outputStringFunctionType);
            OutputStringFunction.SetLinkage(LLVMLinkage.LLVMExternalLinkage);
        }

        public LLVMValueRef CopyMemoryFunction { get; }

        public LLVMValueRef OutputIntFunction { get; }

        public LLVMValueRef OutputStringFunction { get; }
    }
}
