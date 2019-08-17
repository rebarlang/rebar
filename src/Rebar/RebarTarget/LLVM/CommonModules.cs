using System.Collections.Generic;
using LLVMSharp;

namespace Rebar.RebarTarget.LLVM
{
    internal static class CommonModules
    {
        public static Module StringModule { get; }
        public static Module RangeModule { get; }
        public static Module FileModule { get; }

        public static Dictionary<string, LLVMTypeRef> CommonModuleSignatures { get; }

        private static LLVMValueRef _copySliceToPointerFunction;
        private static LLVMValueRef _createEmptyStringFunction;
        private static LLVMValueRef _createNullTerminatedStringFromSliceFunction;
        private static LLVMValueRef _dropStringFunction;
        private static LLVMValueRef _stringAppendFunction;
        private static LLVMValueRef _stringFromSliceRetFunction;

        private static LLVMValueRef _dropFileHandleFunction;
        private static LLVMValueRef _readLineFromFileHandleFunction;

        public const string CopySliceToPointerName = "copy_slice_to_pointer";
        public const string CreateEmptyStringName = "create_empty_string";
        public const string CreateNullTerminatedStringFromSliceName = "create_null_terminated_string_from_slice";
        public const string DropStringName = "drop_string";
        public const string OutputStringSliceName = "output_string_slice";
        public const string StringFromSliceName = "string_from_slice";
        public const string StringToSliceRetName = "string_to_slice_ret";
        public const string StringToSliceName = "string_to_slice";
        public const string StringAppendName = "string_append";
        public const string StringConcatName = "string_concat";

        public const string RangeIteratorNextName = "range_iterator_next";
        public const string CreateRangeIteratorName = "create_range_iterator";

        public const string CreateFileLineIteratorName = "create_file_line_iterator";
        public const string DropFileHandleName = "drop_file_handle";
        public const string DropFileLineIteratorName = "drop_file_line_iterator";
        public const string FileLineIteratorNextName = "file_line_iterator_next";
        public const string OpenFileHandleName = "open_file_handle";
        public const string ReadLineFromFileHandleName = "read_line_from_file_handle";
        public const string WriteStringToFileHandleName = "write_string_to_file_handle";

        static CommonModules()
        {
            CommonModuleSignatures = new Dictionary<string, LLVMTypeRef>();

            StringModule = new Module("string");
            CreateStringModule(StringModule);
            RangeModule = new Module("range");
            CreateRangeModule(RangeModule);
            FileModule = new Module("file");
            CreateFileModule(FileModule);
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

            CommonModuleSignatures[CreateEmptyStringName] = LLVMSharp.LLVM.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[] { LLVMTypeRef.PointerType(LLVMExtensions.StringType, 0) },
                false);
            BuildCreateEmptyStringFunction(stringModule);

            CommonModuleSignatures[CreateNullTerminatedStringFromSliceName] = LLVMSharp.LLVM.FunctionType(
                LLVMExtensions.BytePointerType,
                new LLVMTypeRef[] { LLVMExtensions.StringSliceReferenceType },
                false);
            BuildCreateNullTerminatedStringFromSlice(stringModule);

            CommonModuleSignatures[DropStringName] = LLVMSharp.LLVM.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[] { LLVMTypeRef.PointerType(LLVMExtensions.StringType, 0) },
                false);
            BuildDropStringFunction(stringModule);

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
                slice = builder.BuildStringSliceReferenceValue(stringAllocationPtr, stringSize);
            builder.CreateRet(slice);
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
            _stringAppendFunction = stringModule.AddFunction(StringAppendName, CommonModuleSignatures[StringAppendName]);
            LLVMBasicBlockRef entryBlock = _stringAppendFunction.AppendBasicBlock("entry");
            var builder = new IRBuilder();
            builder.PositionBuilderAtEnd(entryBlock);

            LLVMValueRef stringPtr = _stringAppendFunction.GetParam(0u),
                sliceReference = _stringAppendFunction.GetParam(1u);

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

        private static void BuildDropStringFunction(Module stringModule)
        {
            _dropStringFunction = stringModule.AddFunction(DropStringName, CommonModuleSignatures[DropStringName]);
            LLVMBasicBlockRef entryBlock = _dropStringFunction.AppendBasicBlock("entry");
            var builder = new IRBuilder();
            builder.PositionBuilderAtEnd(entryBlock);

            LLVMValueRef stringPtr = _dropStringFunction.GetParam(0u),
                stringAllocationPtrPtr = builder.CreateStructGEP(stringPtr, 0u, "stringAllocationPtrPtr"),
                stringAllocationPtr = builder.CreateLoad(stringAllocationPtrPtr, "stringAllocationPtr");
            builder.CreateFree(stringAllocationPtr);
            builder.CreateRetVoid();
        }

        private static void BuildCreateNullTerminatedStringFromSlice(Module stringModule)
        {
            _createNullTerminatedStringFromSliceFunction = stringModule.AddFunction(CreateNullTerminatedStringFromSliceName, CommonModuleSignatures[CreateNullTerminatedStringFromSliceName]);
            LLVMBasicBlockRef entryBlock = _createNullTerminatedStringFromSliceFunction.AppendBasicBlock("entry");
            var builder = new IRBuilder();
            builder.PositionBuilderAtEnd(entryBlock);

            LLVMValueRef stringSliceReference = _createNullTerminatedStringFromSliceFunction.GetParam(0u),
                stringSliceAllocationPtr = builder.CreateExtractValue(stringSliceReference, 0u, "stringSliceAllocationPtr"),
                stringSliceLength = builder.CreateExtractValue(stringSliceReference, 1u, "stringSliceLength"),
                nullTerminatedStringLength = builder.CreateAdd(stringSliceLength, 1.AsLLVMValue(), "nullTerminatedStringLength"),
                nullTerminatedStringAllocationPtr = builder.CreateArrayMalloc(LLVMTypeRef.Int8Type(), nullTerminatedStringLength, "nullTerminatedStringAllocationPtr"),
                nullBytePtr = builder.CreateGEP(nullTerminatedStringAllocationPtr, new LLVMValueRef[] { stringSliceLength }, "nullBytePtr");
            builder.CreateCall(_copySliceToPointerFunction, new LLVMValueRef[] { stringSliceReference, nullTerminatedStringAllocationPtr }, string.Empty);
            builder.CreateStore(((byte)0).AsLLVMValue(), nullBytePtr);
            builder.CreateRet(nullTerminatedStringAllocationPtr);
        }

        private static void BuildCreateEmptyStringFunction(Module stringModule)
        {
            _createEmptyStringFunction = stringModule.AddFunction(CreateEmptyStringName, CommonModuleSignatures[CreateEmptyStringName]);
            LLVMBasicBlockRef entryBlock = _createEmptyStringFunction.AppendBasicBlock("entry");
            var builder = new IRBuilder();
            builder.PositionBuilderAtEnd(entryBlock);

            LLVMValueRef stringPtr = _createEmptyStringFunction.GetParam(0u),
                stringAllocationPtrPtr = builder.CreateStructGEP(stringPtr, 0u, "stringAllocationPtrPtr"),
                stringLengthPtr = builder.CreateStructGEP(stringPtr, 1u, "stringLengthPtr"),
                allocationPtr = builder.CreateArrayMalloc(LLVMTypeRef.Int8Type(), 4.AsLLVMValue(), "allocationPtr");
            builder.CreateStore(allocationPtr, stringAllocationPtrPtr);
            builder.CreateStore(0.AsLLVMValue(), stringLengthPtr);
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

        #region File Module

        private static void CreateFileModule(Module fileModule)
        {
            CommonExternalFunctions externalFunctions = new CommonExternalFunctions(fileModule);

            CommonModuleSignatures[CreateFileLineIteratorName] = LLVMSharp.LLVM.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[]
                {
                    LLVMExtensions.FileHandleType,
                    LLVMTypeRef.PointerType(LLVMExtensions.FileLineIteratorType, 0),
                },
                false);
            BuildCreateFileLineIteratorFunction(fileModule);

            CommonModuleSignatures[DropFileHandleName] = LLVMSharp.LLVM.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[]
                {
                    LLVMTypeRef.PointerType(LLVMExtensions.FileHandleType, 0),
                },
                false);
            BuildDropFileHandleFunction(fileModule, externalFunctions);

            CommonModuleSignatures[DropFileLineIteratorName] = LLVMSharp.LLVM.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[]
                {
                    LLVMTypeRef.PointerType(LLVMExtensions.FileLineIteratorType, 0),
                },
                false);
            BuildDropFileLineIteratorFunction(fileModule);

            CommonModuleSignatures[ReadLineFromFileHandleName] = LLVMSharp.LLVM.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[]
                {
                    LLVMTypeRef.PointerType(LLVMExtensions.FileHandleType, 0),
                    LLVMTypeRef.PointerType(LLVMExtensions.StringType.CreateLLVMOptionType(), 0),
                },
                false);
            BuildReadLineFromFileHandleFunction(fileModule, externalFunctions);

            CommonModuleSignatures[FileLineIteratorNextName] = LLVMSharp.LLVM.FunctionType(
                LLVMExtensions.StringType.CreateLLVMOptionType(),
                new LLVMTypeRef[]
                {
                    LLVMTypeRef.PointerType(LLVMExtensions.FileLineIteratorType, 0),
                },
                false);
            BuildFileLineIteratorNextFunction(fileModule);

            CommonModuleSignatures[OpenFileHandleName] = LLVMSharp.LLVM.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[]
                {
                    LLVMExtensions.StringSliceReferenceType,
                    LLVMTypeRef.PointerType(LLVMExtensions.FileHandleType.CreateLLVMOptionType(), 0),
                },
                false);
            BuildOpenFileHandleFunction(fileModule, externalFunctions);

            CommonModuleSignatures[WriteStringToFileHandleName] = LLVMSharp.LLVM.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[]
                {
                    LLVMTypeRef.PointerType(LLVMExtensions.FileHandleType, 0),
                    LLVMExtensions.StringSliceReferenceType,
                },
                false);
            BuildWriteStringToFileHandleFunction(fileModule, externalFunctions);
        }

        private static void BuildOpenFileHandleFunction(Module fileModule, CommonExternalFunctions externalFunctions)
        {
            LLVMValueRef openFileHandleFunction = fileModule.AddFunction(OpenFileHandleName, CommonModuleSignatures[OpenFileHandleName]);
            LLVMBasicBlockRef entryBlock = openFileHandleFunction.AppendBasicBlock("entry");
            var builder = new IRBuilder();
            builder.PositionBuilderAtEnd(entryBlock);

            LLVMValueRef pathSliceRef = openFileHandleFunction.GetParam(0u),
                nullTerminatedStringPtr = builder.CreateCall(_createNullTerminatedStringFromSliceFunction, new LLVMValueRef[] { pathSliceRef }, "nullTerminatedStringtPtr"),
                readWriteAccess = (0xC0000000u).AsLLVMValue(),
                noShareMode = 0u.AsLLVMValue(),
                openAlways = (0x4u).AsLLVMValue(),
                fileAttributeNormal = (0x80u).AsLLVMValue(),
                fileHandleOptionPtr = openFileHandleFunction.GetParam(1u),
                fileHandleIsSomePtr = builder.CreateStructGEP(fileHandleOptionPtr, 0u, "fileHandleIsSomePtr"),
                fileHandleInnerValuePtr = builder.CreateStructGEP(fileHandleOptionPtr, 1u, "fileHandleInnerValuePtr"),
                fileHandleInnerValueFileHandlePtr = builder.CreateStructGEP(fileHandleInnerValuePtr, 0u, "fileHandleInnerValueFileHandlePtr"),
                fileHandle = builder.CreateCall(
                    externalFunctions.CreateFileAFunction,
                    new LLVMValueRef[] { nullTerminatedStringPtr, readWriteAccess, noShareMode, LLVMExtensions.NullVoidPointer, openAlways, fileAttributeNormal, LLVMExtensions.NullVoidPointer },
                    "fileHandle");
            builder.CreateFree(nullTerminatedStringPtr);
            builder.CreateStore(true.AsLLVMValue(), fileHandleIsSomePtr);
            builder.CreateStore(fileHandle, fileHandleInnerValueFileHandlePtr);
            builder.CreateRetVoid();
        }

        private static void BuildReadLineFromFileHandleFunction(Module fileModule, CommonExternalFunctions externalFunctions)
        {
            _readLineFromFileHandleFunction = fileModule.AddFunction(ReadLineFromFileHandleName, CommonModuleSignatures[ReadLineFromFileHandleName]);
            LLVMBasicBlockRef entryBlock = _readLineFromFileHandleFunction.AppendBasicBlock("entry");
            var builder = new IRBuilder();
            builder.PositionBuilderAtEnd(entryBlock);

            LLVMBasicBlockRef loopStartBlock = _readLineFromFileHandleFunction.AppendBasicBlock("loopStart"),
                handleByteBlock = _readLineFromFileHandleFunction.AppendBasicBlock("handleByte"),
                byteIsCRBlock = _readLineFromFileHandleFunction.AppendBasicBlock("byteIsCR"),
                byteIsNotCRBlock = _readLineFromFileHandleFunction.AppendBasicBlock("byteIsNotCR"),
                notNewLineBlock = _readLineFromFileHandleFunction.AppendBasicBlock("notNewLine"),
                appendCRBlock = _readLineFromFileHandleFunction.AppendBasicBlock("appendCR"),
                appendByteBlock = _readLineFromFileHandleFunction.AppendBasicBlock("appendByte"),
                loopEndBlock = _readLineFromFileHandleFunction.AppendBasicBlock("loopEnd"),
                nonEmptyStringBlock = _readLineFromFileHandleFunction.AppendBasicBlock("nonEmptyString"),
                emptyStringBlock = _readLineFromFileHandleFunction.AppendBasicBlock("emptyString");

            LLVMValueRef fileHandlePtr = _readLineFromFileHandleFunction.GetParam(0u),
                stringPtr = builder.CreateAlloca(LLVMExtensions.StringType, "stringPtr"),
                carriageReturnPtr = builder.CreateAlloca(LLVMTypeRef.Int8Type(), "carriageReturnPtr"),
                carriageReturn = ((byte)0xD).AsLLVMValue(),
                byteReadPtr = builder.CreateAlloca(LLVMTypeRef.Int8Type(), "byteReadPtr"),
                bytesReadPtr = builder.CreateAlloca(LLVMTypeRef.Int32Type(), "bytesReadPtr"),
                nonEmptyStringPtr = builder.CreateAlloca(LLVMTypeRef.Int1Type(), "nonEmptyStringPtr"),
                seenCRPtr = builder.CreateAlloca(LLVMTypeRef.Int1Type(), "seenCRPtr");
            builder.CreateStore(carriageReturn, carriageReturnPtr);
            builder.CreateStore(false.AsLLVMValue(), seenCRPtr);
            builder.CreateStore(false.AsLLVMValue(), nonEmptyStringPtr);
            builder.CreateCall(_createEmptyStringFunction, new LLVMValueRef[] { stringPtr }, string.Empty);
            builder.CreateBr(loopStartBlock);

            builder.PositionBuilderAtEnd(loopStartBlock);
            LLVMValueRef hFilePtr = builder.CreateStructGEP(fileHandlePtr, 0u, "hFilePtr"),
                hFile = builder.CreateLoad(hFilePtr, "hFile");
            LLVMValueRef readFileResult = builder.CreateCall(
                externalFunctions.ReadFileFunction,
                new LLVMValueRef[] { hFile, byteReadPtr, 1.AsLLVMValue(), bytesReadPtr, LLVMExtensions.NullVoidPointer },
                "readFileResult"),
                readFileResultBool = builder.CreateICmp(LLVMIntPredicate.LLVMIntNE, readFileResult, 0.AsLLVMValue(), "readFileResultBool"),
                bytesRead = builder.CreateLoad(bytesReadPtr, "bytesRead"),
                zeroBytesRead = builder.CreateICmp(LLVMIntPredicate.LLVMIntEQ, bytesRead, 0.AsLLVMValue(), "zeroBytesRead"),
                eof = builder.CreateAnd(readFileResultBool, zeroBytesRead, "eof");
            builder.CreateCondBr(eof, loopEndBlock, handleByteBlock);

            builder.PositionBuilderAtEnd(handleByteBlock);
            LLVMValueRef byteRead = builder.CreateLoad(byteReadPtr, "byteRead"),
                byteReadIsCR = builder.CreateICmp(LLVMIntPredicate.LLVMIntEQ, byteRead, carriageReturn, "byteReadIsCR");
            builder.CreateCondBr(byteReadIsCR, byteIsCRBlock, byteIsNotCRBlock);

            builder.PositionBuilderAtEnd(byteIsCRBlock);
            builder.CreateStore(true.AsLLVMValue(), seenCRPtr);
            builder.CreateBr(loopStartBlock);

            builder.PositionBuilderAtEnd(byteIsNotCRBlock);
            LLVMValueRef byteIsLF = builder.CreateICmp(LLVMIntPredicate.LLVMIntEQ, byteRead, ((byte)0xA).AsLLVMValue(), "byteIsLF"),
                seenCR = builder.CreateLoad(seenCRPtr, "seenCR"),
                newLine = builder.CreateAnd(byteIsLF, seenCR, "newLine");
            builder.CreateCondBr(newLine, loopEndBlock, notNewLineBlock);

            builder.PositionBuilderAtEnd(notNewLineBlock);
            builder.CreateCondBr(seenCR, appendCRBlock, appendByteBlock);

            builder.PositionBuilderAtEnd(appendCRBlock);
            LLVMValueRef crSlice = builder.BuildStringSliceReferenceValue(carriageReturnPtr, 1.AsLLVMValue());
            builder.CreateCall(_stringAppendFunction, new LLVMValueRef[] { stringPtr, crSlice }, string.Empty);
            builder.CreateBr(appendByteBlock);

            builder.PositionBuilderAtEnd(appendByteBlock);
            LLVMValueRef byteSlice = builder.BuildStringSliceReferenceValue(byteReadPtr, 1.AsLLVMValue());
            builder.CreateCall(_stringAppendFunction, new LLVMValueRef[] { stringPtr, byteSlice }, string.Empty);
            builder.CreateStore(true.AsLLVMValue(), nonEmptyStringPtr);
            builder.CreateStore(false.AsLLVMValue(), seenCRPtr);
            builder.CreateBr(loopStartBlock);

            builder.PositionBuilderAtEnd(loopEndBlock);
            LLVMValueRef optionStringPtr = _readLineFromFileHandleFunction.GetParam(1u),
                optionStringIsSomePtr = builder.CreateStructGEP(optionStringPtr, 0u, "optionStringIsSomePtr"),
                nonEmptyString = builder.CreateLoad(nonEmptyStringPtr, "nonEmptyString");
            builder.CreateCondBr(nonEmptyString, nonEmptyStringBlock, emptyStringBlock);

            builder.PositionBuilderAtEnd(nonEmptyStringBlock);
            builder.CreateStore(true.AsLLVMValue(), optionStringIsSomePtr);
            LLVMValueRef optionStringInnerValuePtr = builder.CreateStructGEP(optionStringPtr, 1u, "optionStringInnerValuePtr"),
                stringValue = builder.CreateLoad(stringPtr, "string");
            builder.CreateStore(stringValue, optionStringInnerValuePtr);
            builder.CreateRetVoid();

            builder.PositionBuilderAtEnd(emptyStringBlock);
            builder.CreateStore(false.AsLLVMValue(), optionStringIsSomePtr);
            builder.CreateCall(_dropStringFunction, new LLVMValueRef[] { stringPtr }, string.Empty);
            builder.CreateRetVoid();
        }

        private static void BuildWriteStringToFileHandleFunction(Module fileModule, CommonExternalFunctions externalFunctions)
        {
            LLVMValueRef writeStringToFileHandleFunction = fileModule.AddFunction(WriteStringToFileHandleName, CommonModuleSignatures[WriteStringToFileHandleName]);
            LLVMBasicBlockRef entryBlock = writeStringToFileHandleFunction.AppendBasicBlock("entry");
            var builder = new IRBuilder();
            builder.PositionBuilderAtEnd(entryBlock);

            LLVMValueRef fileHandleStructPtr = writeStringToFileHandleFunction.GetParam(0u),
                fileHandlePtr = builder.CreateStructGEP(fileHandleStructPtr, 0u, "fileHandlePtr"),
                fileHandle = builder.CreateLoad(fileHandlePtr, "fileHandle"),
                stringSlice = writeStringToFileHandleFunction.GetParam(1u),
                stringSliceAllocationPtr = builder.CreateExtractValue(stringSlice, 0u, "stringSliceAllocationPtr"),
                stringSliceLength = builder.CreateExtractValue(stringSlice, 1u, "stringSliceLength"),
                bytesWrittenPtr = builder.CreateAlloca(LLVMTypeRef.Int32Type(), "bytesWrittenPtr");

            builder.CreateCall(
                externalFunctions.WriteFileFunction,
                new LLVMValueRef[] { fileHandle, stringSliceAllocationPtr, stringSliceLength, bytesWrittenPtr, LLVMExtensions.NullVoidPointer },
                "writeFileResult");

            builder.CreateRetVoid();
        }

        private static void BuildDropFileHandleFunction(Module fileModule, CommonExternalFunctions externalFunctions)
        {
            _dropFileHandleFunction = fileModule.AddFunction(DropFileHandleName, CommonModuleSignatures[DropFileHandleName]);
            LLVMBasicBlockRef entryBlock = _dropFileHandleFunction.AppendBasicBlock("entry");
            var builder = new IRBuilder();
            builder.PositionBuilderAtEnd(entryBlock);

            LLVMValueRef fileHandleStructPtr = _dropFileHandleFunction.GetParam(0u),
                fileHandlePtr = builder.CreateStructGEP(fileHandleStructPtr, 0u, "fileHandlePtr"),
                fileHandle = builder.CreateLoad(fileHandlePtr, "fileHandle");
            builder.CreateCall(
                externalFunctions.CloseHandleFunction,
                new LLVMValueRef[] { fileHandle },
                "closeHandleResult");
            builder.CreateRetVoid();
        }

        private static void BuildCreateFileLineIteratorFunction(Module fileModule)
        {
            LLVMValueRef createFileLineIteratorFunction = fileModule.AddFunction(CreateFileLineIteratorName, CommonModuleSignatures[CreateFileLineIteratorName]);
            LLVMBasicBlockRef entryBlock = createFileLineIteratorFunction.AppendBasicBlock("entry");
            var builder = new IRBuilder();
            builder.PositionBuilderAtEnd(entryBlock);

            LLVMValueRef fileHandle = createFileLineIteratorFunction.GetParam(0u),
                fileLineIteratorPtr = createFileLineIteratorFunction.GetParam(1u),
                fileLineIteratorFileHandlePtr = builder.CreateStructGEP(fileLineIteratorPtr, 0u, "fileLineIteratorFileHandlePtr"),
                fileLineIteratorBeforeEndPtr = builder.CreateStructGEP(fileLineIteratorPtr, 1u, "fileLineIteratorBeforeEndPtr");
            builder.CreateStore(fileHandle, fileLineIteratorFileHandlePtr);
            builder.CreateStore(true.AsLLVMValue(), fileLineIteratorBeforeEndPtr);
            builder.CreateRetVoid();
        }

        private static void BuildDropFileLineIteratorFunction(Module fileModule)
        {
            LLVMValueRef dropFileLineIteratorFunction = fileModule.AddFunction(DropFileLineIteratorName, CommonModuleSignatures[DropFileLineIteratorName]);
            LLVMBasicBlockRef entryBlock = dropFileLineIteratorFunction.AppendBasicBlock("entry");
            var builder = new IRBuilder();
            builder.PositionBuilderAtEnd(entryBlock);

            LLVMValueRef fileLineIteratorPtr = dropFileLineIteratorFunction.GetParam(0u),
                fileHandlePtr = builder.CreateStructGEP(fileLineIteratorPtr, 0u, "fileHandlePtr");
            builder.CreateCall(_dropFileHandleFunction, new LLVMValueRef[] { fileHandlePtr }, string.Empty);
            builder.CreateRetVoid();
        }

        private static void BuildFileLineIteratorNextFunction(Module fileModule)
        {
            LLVMValueRef fileLineIteratorNextFunction = fileModule.AddFunction(FileLineIteratorNextName, CommonModuleSignatures[FileLineIteratorNextName]);
            LLVMBasicBlockRef entryBlock = fileLineIteratorNextFunction.AppendBasicBlock("entry");
            var builder = new IRBuilder();
            builder.PositionBuilderAtEnd(entryBlock);

            LLVMBasicBlockRef beforeEndBlock = fileLineIteratorNextFunction.AppendBasicBlock("beforeEnd"),
                atEndBlock = fileLineIteratorNextFunction.AppendBasicBlock("atEnd"),
                returnBlock = fileLineIteratorNextFunction.AppendBasicBlock("return");

            LLVMValueRef optionStringPtr = builder.CreateAlloca(LLVMExtensions.StringType.CreateLLVMOptionType(), "optionStringPtr"),
                fileLineIteratorPtr = fileLineIteratorNextFunction.GetParam(0u),
                fileHandlePtr = builder.CreateStructGEP(fileLineIteratorPtr, 0u, "fileHandlePtr"),
                beforeEndPtr = builder.CreateStructGEP(fileLineIteratorPtr, 1u, "beforeEndPtr"),
                beforeEnd = builder.CreateLoad(beforeEndPtr, "beforeEnd"),
                optionStringIsSomePtr = builder.CreateStructGEP(optionStringPtr, 0u, "optionStringIsSomePtr");
            builder.CreateCondBr(beforeEnd, beforeEndBlock, atEndBlock);

            builder.PositionBuilderAtEnd(beforeEndBlock);
            builder.CreateCall(_readLineFromFileHandleFunction, new LLVMValueRef[] { fileHandlePtr, optionStringPtr }, string.Empty);
            LLVMValueRef optionStringIsSome = builder.CreateLoad(optionStringIsSomePtr, "optionStringIsSome");
            builder.CreateStore(optionStringIsSome, beforeEndPtr);
            builder.CreateBr(returnBlock);

            builder.PositionBuilderAtEnd(atEndBlock);
            builder.CreateStore(false.AsLLVMValue(), optionStringIsSomePtr);
            builder.CreateBr(returnBlock);

            builder.PositionBuilderAtEnd(returnBlock);
            LLVMValueRef optionString = builder.CreateLoad(optionStringPtr, "optionString");
            builder.CreateRet(optionString);
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

            OutputBoolFunction = CreateSingleParameterVoidFunction(addTo, LLVMTypeRef.Int1Type(), "output_bool");
            OutputInt8Function = CreateSingleParameterVoidFunction(addTo, LLVMTypeRef.Int8Type(), "output_int8");
            OutputUInt8Function = CreateSingleParameterVoidFunction(addTo, LLVMTypeRef.Int8Type(), "output_uint8");
            OutputInt16Function = CreateSingleParameterVoidFunction(addTo, LLVMTypeRef.Int16Type(), "output_int16");
            OutputUInt16Function = CreateSingleParameterVoidFunction(addTo, LLVMTypeRef.Int16Type(), "output_uint16");
            OutputInt32Function = CreateSingleParameterVoidFunction(addTo, LLVMTypeRef.Int32Type(), "output_int32");
            OutputUInt32Function = CreateSingleParameterVoidFunction(addTo, LLVMTypeRef.Int32Type(), "output_uint32");
            OutputInt64Function = CreateSingleParameterVoidFunction(addTo, LLVMTypeRef.Int64Type(), "output_int64");
            OutputUInt64Function = CreateSingleParameterVoidFunction(addTo, LLVMTypeRef.Int64Type(), "output_uint64");

            LLVMTypeRef outputStringFunctionType = LLVMSharp.LLVM.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[] { bytePointerType, LLVMTypeRef.Int32Type() },
                false);
            OutputStringFunction = addTo.AddFunction("output_string", outputStringFunctionType);
            OutputStringFunction.SetLinkage(LLVMLinkage.LLVMExternalLinkage);

            LLVMTypeRef closeHandleFunctionType = LLVMSharp.LLVM.FunctionType(
                LLVMTypeRef.Int32Type(),    // bool
                new LLVMTypeRef[] { LLVMExtensions.VoidPointerType },
                false);
            CloseHandleFunction = addTo.AddFunction("CloseHandle", closeHandleFunctionType);

            LLVMTypeRef createFileAFunctionType = LLVMSharp.LLVM.FunctionType(
                LLVMExtensions.VoidPointerType,
                new LLVMTypeRef[]
                {
                    bytePointerType,    // filename
                    LLVMTypeRef.Int32Type(),    // access
                    LLVMTypeRef.Int32Type(),    // share
                    LLVMExtensions.VoidPointerType,    // securityAttributes
                    LLVMTypeRef.Int32Type(),    // creationDisposition
                    LLVMTypeRef.Int32Type(),    // flagsAndAttributes
                    LLVMExtensions.VoidPointerType,    // templateFile
                },
                false);
            CreateFileAFunction = addTo.AddFunction("CreateFileA", createFileAFunctionType);

            LLVMTypeRef readFileFunctionType = LLVMSharp.LLVM.FunctionType(
                LLVMTypeRef.Int32Type(),    // bool
                new LLVMTypeRef[]
                {
                    LLVMExtensions.VoidPointerType,     // hFile
                    LLVMExtensions.VoidPointerType,     // lpBuffer
                    LLVMTypeRef.Int32Type(),            // nNumberOfBytesToRead
                    LLVMTypeRef.PointerType(LLVMTypeRef.Int32Type(), 0),   // lpNumberOfBytesRead
                    LLVMExtensions.VoidPointerType,     // lpOverlapped
                },
                false);
            ReadFileFunction = addTo.AddFunction("ReadFile", readFileFunctionType);

            LLVMTypeRef writeFileFunctionType = LLVMSharp.LLVM.FunctionType(
                LLVMTypeRef.Int32Type(),    // bool
                new LLVMTypeRef[]
                {
                    LLVMExtensions.VoidPointerType,    // hFile
                    LLVMExtensions.VoidPointerType,    // lpBuffer
                    LLVMTypeRef.Int32Type(),           // nNumberOfBytesToWrite,
                    LLVMTypeRef.PointerType(LLVMTypeRef.Int32Type(), 0),    // lpNumberOfBytesWritten
                    LLVMExtensions.VoidPointerType,    // lpOverlapped
                },
                false);
            WriteFileFunction = addTo.AddFunction("WriteFile", writeFileFunctionType);
        }

        private LLVMValueRef CreateSingleParameterVoidFunction(Module module, LLVMTypeRef parameterType, string name)
        {
            LLVMTypeRef functionType = LLVMSharp.LLVM.FunctionType(LLVMSharp.LLVM.VoidType(), new LLVMTypeRef[] { parameterType }, false);
            LLVMValueRef function = module.AddFunction(name, functionType);
            function.SetLinkage(LLVMLinkage.LLVMExternalLinkage);
            return function;
        }

        public LLVMValueRef CopyMemoryFunction { get; }

        public LLVMValueRef OutputBoolFunction { get; }

        public LLVMValueRef OutputInt8Function { get; }

        public LLVMValueRef OutputUInt8Function { get; }

        public LLVMValueRef OutputInt16Function { get; }

        public LLVMValueRef OutputUInt16Function { get; }

        public LLVMValueRef OutputInt32Function { get; }

        public LLVMValueRef OutputUInt32Function { get; }

        public LLVMValueRef OutputInt64Function { get; }

        public LLVMValueRef OutputUInt64Function { get; }

        public LLVMValueRef OutputStringFunction { get; }

        public LLVMValueRef CloseHandleFunction { get; }

        public LLVMValueRef CreateFileAFunction { get; }

        public LLVMValueRef ReadFileFunction { get; }

        public LLVMValueRef WriteFileFunction { get; }
    }
}
