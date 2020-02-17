using System;
using System.Collections.Generic;
using System.IO;
using LLVMSharp;

namespace Rebar.RebarTarget.LLVM
{
    internal static class CommonModules
    {
        private static readonly Dictionary<string, Module> _modules = new Dictionary<string, Module>();

        public static Dictionary<string, LLVMTypeRef> CommonModuleSignatures { get; }
        private static readonly Dictionary<string, string> _functionModuleNames = new Dictionary<string, string>();

        public const string FakeDropModuleName = "fakedrop";
        public const string SchedulerModuleName = "scheduler";
        public const string StringModuleName = "string";
        public const string OutputModuleName = "output";
        public const string RangeModuleName = "range";
        public const string FileModuleName = "file";

        private static LLVMValueRef _copySliceToPointerFunction;
        private static LLVMValueRef _createEmptyStringFunction;
        private static LLVMValueRef _createNullTerminatedStringFromSliceFunction;
        private static LLVMValueRef _dropStringFunction;
        private static LLVMValueRef _stringAppendFunction;
        private static LLVMValueRef _stringFromSliceFunction;
        private static LLVMValueRef _stringToSliceRetFunction;

        public const string FakeDropCreateName = "fakedrop_create";
        public const string FakeDropDropName = "fakedrop_drop";

        public const string PartialScheduleName = "partial_schedule";
        public const string InvokeName = "invoke";

        public const string CopySliceToPointerName = "copy_slice_to_pointer";
        public const string CreateEmptyStringName = "create_empty_string";
        public const string CreateNullTerminatedStringFromSliceName = "create_null_terminated_string_from_slice";
        public const string DropStringName = "drop_string";
        public const string StringCloneName = "string_clone";
        public const string OutputStringSliceName = "output_string_slice";
        public const string StringFromSliceName = "string_from_slice";
        public const string StringToSliceRetName = "string_to_slice_ret";
        public const string StringToSliceName = "string_to_slice";
        public const string StringAppendName = "string_append";
        public const string StringConcatName = "string_concat";
        public const string StringSliceToStringSplitIteratorName = "string_slice_to_string_split_iterator";
        public const string StringSplitIteratorNextName = "string_split_iterator_next";

        public const string OutputBoolName = "output_bool";
        public const string OutputInt8Name = "output_int8";
        public const string OutputUInt8Name = "output_uint8";
        public const string OutputInt16Name = "output_int16";
        public const string OutputUInt16Name = "output_uint16";
        public const string OutputInt32Name = "output_int32";
        public const string OutputUInt32Name = "output_uint32";
        public const string OutputInt64Name = "output_int64";
        public const string OutputUInt64Name = "output_uint64";

        public const string RangeIteratorNextName = "range_iterator_next";
        public const string CreateRangeIteratorName = "create_range_iterator";

        public const string DropFileHandleName = "drop_file_handle";
        public const string OpenFileHandleName = "open_file_handle";
        public const string ReadLineFromFileHandleName = "read_line_from_file_handle";
        public const string WriteStringToFileHandleName = "write_string_to_file_handle";

        static CommonModules()
        {
            CommonModuleSignatures = new Dictionary<string, LLVMTypeRef>();

            CreateModule(FakeDropModuleName, CreateFakeDropModule);
            CreateModule(SchedulerModuleName, CreateSchedulerModule);
            CreateModule(StringModuleName, CreateStringModule);
            CreateModule(RangeModuleName, CreateRangeModule);
            CreateModule(FileModuleName, CreateFileModule);

            string assemblyDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string llvmResourcesPath = Path.Combine(assemblyDirectory, "RebarTarget", "LLVM", "Resources");

            string outputModulePath = Path.Combine(llvmResourcesPath, "output.bc");
            Module outputModule = File.ReadAllBytes(outputModulePath).DeserializeModuleAsBitcode();
            _modules[OutputModuleName] = outputModule;
            var outputNames = new string[]
            {
                OutputBoolName,
                OutputInt8Name,
                OutputUInt8Name,
                OutputInt16Name,
                OutputUInt16Name,
                OutputInt32Name,
                OutputUInt32Name,
                OutputInt64Name,
                OutputUInt64Name,
            };
            foreach (string name in outputNames)
            {
                AddFunction(name, OutputModuleName, outputModule.GetNamedFunction(name).TypeOf().GetElementType());
            }
        }

        private static Module CreateModule(string name, Action<Module> moduleCreator)
        {
            var module = new Module(name);
            _modules[name] = module;
            moduleCreator(module);
            return module;
        }

        public static Module GetModule(string moduleName)
        {
            return _modules[moduleName];
        }

        public static Tuple<LLVMTypeRef, string> GetCommonFunction(string functionName)
        {
            return new Tuple<LLVMTypeRef, string>(CommonModuleSignatures[functionName], _functionModuleNames[functionName]);
        }

        private static void AddFunction(string functionName, string moduleName, LLVMTypeRef functionSignature)
        {
            CommonModuleSignatures[functionName] = functionSignature;
            _functionModuleNames[functionName] = moduleName;
        }

        #region FakeDrop Module

        private static void CreateFakeDropModule(Module fakeDropModule)
        {
            var externalFunctions = new CommonExternalFunctions(fakeDropModule);

            AddFunction(FakeDropCreateName, FakeDropModuleName, LLVMSharp.LLVM.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[] 
                {
                    LLVMTypeRef.Int32Type(),
                    LLVMTypeRef.PointerType(LLVMExtensions.FakeDropType, 0)
                },
                false));
            CreateFakeDropCreateFunction(fakeDropModule, externalFunctions);

            AddFunction(FakeDropDropName, FakeDropModuleName, LLVMSharp.LLVM.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[] { LLVMTypeRef.PointerType(LLVMExtensions.FakeDropType, 0) },
                false));
            CreateFakeDropDropFunction(fakeDropModule, externalFunctions);
        }

        private static void CreateFakeDropCreateFunction(Module fakeDropModule, CommonExternalFunctions externalFunctions)
        {
            LLVMValueRef fakeDropCreateFunction = fakeDropModule.AddFunction(FakeDropCreateName, CommonModuleSignatures[FakeDropCreateName]);
            LLVMBasicBlockRef entryBlock = fakeDropCreateFunction.AppendBasicBlock("entry");
            var builder = new IRBuilder();
            builder.PositionBuilderAtEnd(entryBlock);

            LLVMValueRef fakeDropPtr = fakeDropCreateFunction.GetParam(1u),
                fakeDropIdPtr = builder.CreateStructGEP(fakeDropPtr, 0u, "fakeDropIdPtr"),
                fakeDropId = fakeDropCreateFunction.GetParam(0u);
            builder.CreateStore(fakeDropId, fakeDropIdPtr);
            builder.CreateRetVoid();
        }

        private static void CreateFakeDropDropFunction(Module fakeDropModule, CommonExternalFunctions externalFunctions)
        {
            LLVMValueRef fakeDropDropFunction = fakeDropModule.AddFunction(FakeDropDropName, CommonModuleSignatures[FakeDropDropName]);
            LLVMBasicBlockRef entryBlock = fakeDropDropFunction.AppendBasicBlock("entry");
            var builder = new IRBuilder();
            builder.PositionBuilderAtEnd(entryBlock);

            LLVMValueRef fakeDropPtr = fakeDropDropFunction.GetParam(0u),
                fakeDropIdPtr = builder.CreateStructGEP(fakeDropPtr, 0u, "fakeDropIdPtr"),
                fakeDropId = builder.CreateLoad(fakeDropIdPtr, "fakeDropId");
            builder.CreateCall(externalFunctions.FakeDropFunction, new LLVMValueRef[] { fakeDropId }, string.Empty);
            builder.CreateRetVoid();
        }

        #endregion

        #region Scheduler Module

        private static void CreateSchedulerModule(Module schedulerModule)
        {
            var externalFunctions = new CommonExternalFunctions(schedulerModule);

            AddFunction(PartialScheduleName, SchedulerModuleName, LLVMSharp.LLVM.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[]
                {
                    LLVMExtensions.VoidPointerType,
                    LLVMTypeRef.PointerType(LLVMTypeRef.Int32Type(), 0u),
                    LLVMTypeRef.PointerType(LLVMExtensions.ScheduledTaskFunctionType, 0u)
                },
                false));
            BuildPartialScheduleFunction(schedulerModule, externalFunctions);

            AddFunction(InvokeName, SchedulerModuleName, LLVMSharp.LLVM.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[] { LLVMExtensions.WakerType },
                false));
            BuildInvokeFunction(schedulerModule);
        }

        private static void BuildPartialScheduleFunction(Module schedulerModule, CommonExternalFunctions externalFunctions)
        {
            var partialScheduleFunction = schedulerModule.AddFunction(PartialScheduleName, CommonModuleSignatures[PartialScheduleName]);
            LLVMBasicBlockRef entryBlock = partialScheduleFunction.AppendBasicBlock("entry"),
                scheduleBlock = partialScheduleFunction.AppendBasicBlock("schedule"),
                endBlock = partialScheduleFunction.AppendBasicBlock("end");
            var builder = new IRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef fireCountPtr = partialScheduleFunction.GetParam(1u),
                one = 1.AsLLVMValue(),
                previousFireCount = builder.CreateAtomicRMW(
                    LLVMAtomicRMWBinOp.LLVMAtomicRMWBinOpSub,
                    fireCountPtr,
                    one,
                    LLVMAtomicOrdering.LLVMAtomicOrderingMonotonic,
                    false),
                previousFireCountWasOne = builder.CreateICmp(LLVMIntPredicate.LLVMIntEQ, previousFireCount, one, "previousFireCountWasOne");
            builder.CreateCondBr(previousFireCountWasOne, scheduleBlock, endBlock);

            builder.PositionBuilderAtEnd(scheduleBlock);
            LLVMValueRef functionPtr = partialScheduleFunction.GetParam(2u),
                statePtr = partialScheduleFunction.GetParam(0u);
            builder.CreateCall(externalFunctions.ScheduleFunction, new LLVMValueRef[] { functionPtr, statePtr }, string.Empty);
            builder.CreateBr(endBlock);

            builder.PositionBuilderAtEnd(endBlock);
            builder.CreateRetVoid();
        }

        private static void BuildInvokeFunction(Module schedulerModule)
        {
            var invokeFunction = schedulerModule.AddFunction(InvokeName, CommonModuleSignatures[InvokeName]);
            LLVMBasicBlockRef entryBlock = invokeFunction.AppendBasicBlock("entry");
            var builder = new IRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef invokable = invokeFunction.GetParam(0u),
                functionPtr = builder.CreateExtractValue(invokable, 0u, "functionPtr"),
                functionArg = builder.CreateExtractValue(invokable, 1u, "functionArg");
            builder.CreateCall(functionPtr, new LLVMValueRef[] { functionArg }, string.Empty);
            builder.CreateRetVoid();
        }

        #endregion

        #region String Module

        private static void CreateStringModule(Module stringModule)
        {
            var externalFunctions = new CommonExternalFunctions(stringModule);

            AddFunction(CopySliceToPointerName, StringModuleName, LLVMSharp.LLVM.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[] { LLVMExtensions.StringSliceReferenceType, LLVMTypeRef.PointerType(LLVMTypeRef.Int8Type(), 0) },
                false));
            BuildCopySliceToPointerFunction(stringModule, externalFunctions);

            AddFunction(CreateEmptyStringName, StringModuleName, LLVMSharp.LLVM.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[] { LLVMTypeRef.PointerType(LLVMExtensions.StringType, 0) },
                false));
            BuildCreateEmptyStringFunction(stringModule);

            AddFunction(CreateNullTerminatedStringFromSliceName, StringModuleName, LLVMSharp.LLVM.FunctionType(
                LLVMExtensions.BytePointerType,
                new LLVMTypeRef[] { LLVMExtensions.StringSliceReferenceType },
                false));
            BuildCreateNullTerminatedStringFromSlice(stringModule);

            AddFunction(DropStringName, StringModuleName, LLVMSharp.LLVM.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[] { LLVMTypeRef.PointerType(LLVMExtensions.StringType, 0) },
                false));
            BuildDropStringFunction(stringModule);

            AddFunction(StringCloneName, StringModuleName, LLVMSharp.LLVM.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[]
                {
                    LLVMTypeRef.PointerType(LLVMExtensions.StringType, 0),
                    LLVMTypeRef.PointerType(LLVMExtensions.StringType, 0)
                },
                false));

            AddFunction(OutputStringSliceName, StringModuleName, LLVMSharp.LLVM.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[] { LLVMExtensions.StringSliceReferenceType },
                false));
            BuildOutputStringSliceFunction(stringModule, externalFunctions);

            AddFunction(StringFromSliceName, StringModuleName, LLVMSharp.LLVM.FunctionType(
                LLVMSharp.LLVM.VoidType(), 
                new LLVMTypeRef[] { LLVMExtensions.StringSliceReferenceType, LLVMTypeRef.PointerType(LLVMExtensions.StringType, 0) }, 
                false));
            BuildStringFromSliceFunction(stringModule, externalFunctions);

            AddFunction(StringToSliceRetName, StringModuleName, LLVMSharp.LLVM.FunctionType(
                LLVMExtensions.StringSliceReferenceType,
                new LLVMTypeRef[] { LLVMTypeRef.PointerType(LLVMExtensions.StringType, 0), },
                false));
            BuildStringToSliceRetFunction(stringModule);

            AddFunction(StringToSliceName, StringModuleName, LLVMSharp.LLVM.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[] { LLVMTypeRef.PointerType(LLVMExtensions.StringType, 0), LLVMTypeRef.PointerType(LLVMExtensions.StringSliceReferenceType, 0) },
                false));
            BuildStringToSliceFunction(stringModule);

            AddFunction(StringAppendName, StringModuleName, LLVMSharp.LLVM.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[] { LLVMTypeRef.PointerType(LLVMExtensions.StringType, 0), LLVMExtensions.StringSliceReferenceType },
                false));
            BuildStringAppendFunction(stringModule, externalFunctions);

            AddFunction(StringConcatName, StringModuleName, LLVMSharp.LLVM.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[] { LLVMExtensions.StringSliceReferenceType, LLVMExtensions.StringSliceReferenceType, LLVMTypeRef.PointerType(LLVMExtensions.StringType, 0) },
                false));
            BuildStringConcatFunction(stringModule, externalFunctions);

            AddFunction(StringSliceToStringSplitIteratorName, StringModuleName, LLVMSharp.LLVM.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[] { LLVMExtensions.StringSliceReferenceType, LLVMTypeRef.PointerType(LLVMExtensions.StringSplitIteratorType, 0) },
                false));
            BuildStringSliceToStringSplitIteratorFunction(stringModule);

            AddFunction(StringSplitIteratorNextName, StringModuleName, LLVMSharp.LLVM.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[] 
                {
                    LLVMTypeRef.PointerType(LLVMExtensions.StringSplitIteratorType, 0),
                    LLVMTypeRef.PointerType(LLVMExtensions.StringSliceReferenceType.CreateLLVMOptionType(), 0)
                },
                false));
            BuildStringSplitIteratorNextFunction(stringModule);

            // depends on StringToSliceRet and StringFromSlice
            BuildStringCloneFunction(stringModule);

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
            _stringFromSliceFunction = stringModule.AddFunction(StringFromSliceName, CommonModuleSignatures[StringFromSliceName]);
            LLVMBasicBlockRef entryBlock = _stringFromSliceFunction.AppendBasicBlock("entry");
            var builder = new IRBuilder();
            builder.PositionBuilderAtEnd(entryBlock);

            LLVMValueRef stringSliceReference = _stringFromSliceFunction.GetParam(0u),
                sliceAllocationPtr = builder.CreateExtractValue(stringSliceReference, 0u, "sliceAllocationPtr"),
                sliceSize = builder.CreateExtractValue(stringSliceReference, 1u, "sliceSize"),
                // Get a pointer to a heap allocation big enough for the string
                allocationPtr = builder.CreateArrayMalloc(LLVMTypeRef.Int8Type(), sliceSize, "allocationPtr");
            builder.CreateCallToCopyMemory(externalFunctions, allocationPtr, sliceAllocationPtr, sliceSize);
            LLVMValueRef stringValue = builder.BuildStructValue(LLVMExtensions.StringType, new LLVMValueRef[] { allocationPtr, sliceSize }, "string"),
                stringPtr = _stringFromSliceFunction.GetParam(1u);
            builder.CreateStore(stringValue, stringPtr);
            builder.CreateRetVoid();
        }

        private static void BuildStringToSliceRetFunction(Module stringModule)
        {
            _stringToSliceRetFunction = stringModule.AddFunction(StringToSliceRetName, CommonModuleSignatures[StringToSliceRetName]);
            LLVMBasicBlockRef entryBlock = _stringToSliceRetFunction.AppendBasicBlock("entry");
            var builder = new IRBuilder();
            builder.PositionBuilderAtEnd(entryBlock);

            LLVMValueRef stringPtr = _stringToSliceRetFunction.GetParam(0u),
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
            LLVMValueRef sliceReference = builder.CreateCall(_stringToSliceRetFunction, new LLVMValueRef[] { stringPtr }, "sliceReference");
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
            LLVMValueRef stringSlice = builder.CreateCall(_stringToSliceRetFunction, new LLVMValueRef[] { stringPtr }, "stringSlice");
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
                destinationPtr = _copySliceToPointerFunction.GetParam(1u);
            builder.CreateCallToCopyMemory(externalFunctions, destinationPtr, sourcePtr, size);
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

        private static void BuildStringCloneFunction(Module stringModule)
        {
            LLVMValueRef cloneStringFunction = stringModule.AddFunction(StringCloneName, CommonModuleSignatures[StringCloneName]);
            LLVMBasicBlockRef entryBlock = cloneStringFunction.AppendBasicBlock("entry");
            var builder = new IRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef stringPtr = cloneStringFunction.GetParam(0u),
                stringClonePtr = cloneStringFunction.GetParam(1u),
                stringSliceRef = builder.CreateCall(_stringToSliceRetFunction, new LLVMValueRef[] { stringPtr }, "stringSliceRef");
            builder.CreateCall(_stringFromSliceFunction, new LLVMValueRef[] { stringSliceRef, stringClonePtr }, string.Empty);
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

        private static void BuildStringSliceToStringSplitIteratorFunction(Module stringModule)
        {
            var stringSliceToStringSplitIteratorFunction = stringModule.AddFunction(StringSliceToStringSplitIteratorName, CommonModuleSignatures[StringSliceToStringSplitIteratorName]);
            LLVMBasicBlockRef entryBlock = stringSliceToStringSplitIteratorFunction.AppendBasicBlock("entry");
            var builder = new IRBuilder();
            builder.PositionBuilderAtEnd(entryBlock);

            LLVMValueRef stringSliceRef = stringSliceToStringSplitIteratorFunction.GetParam(0u),
                stringSplitIteratorPtr = stringSliceToStringSplitIteratorFunction.GetParam(1u),
                initialStringPtr = builder.CreateExtractValue(stringSliceRef, 0u, "initialStringPtr"),
                stringSplitIterator = builder.BuildStructValue(
                    LLVMExtensions.StringSplitIteratorType,
                    new LLVMValueRef[] { stringSliceRef, initialStringPtr },
                    "stringSplitIterator");
            builder.CreateStore(stringSplitIterator, stringSplitIteratorPtr);
            builder.CreateRetVoid();
        }

        private static void BuildStringSplitIteratorNextFunction(Module stringModule)
        {
            var stringSplitIteratorNextFunction = stringModule.AddFunction(StringSplitIteratorNextName, CommonModuleSignatures[StringSplitIteratorNextName]);
            LLVMBasicBlockRef entryBlock = stringSplitIteratorNextFunction.AppendBasicBlock("entry"),
                findSplitBeginLoopBeginBlock = stringSplitIteratorNextFunction.AppendBasicBlock("findSplitBeginLoopBegin"),
                checkSplitBeginForSpaceBlock = stringSplitIteratorNextFunction.AppendBasicBlock("checkSplitBeginForSpace"),
                advanceSplitBeginBlock = stringSplitIteratorNextFunction.AppendBasicBlock("advanceSplitBegin"),
                findSplitEndLoopBeginBlock = stringSplitIteratorNextFunction.AppendBasicBlock("findSplitEndLoopBeginBlock"),
                checkSplitEndForSpaceBlock = stringSplitIteratorNextFunction.AppendBasicBlock("checkSplitEndForSpace"),
                advanceSplitEndBlock = stringSplitIteratorNextFunction.AppendBasicBlock("advanceSplitEnd"),
                returnSomeBlock = stringSplitIteratorNextFunction.AppendBasicBlock("returnSome"),
                returnNoneBlock = stringSplitIteratorNextFunction.AppendBasicBlock("returnNone"),
                endBlock = stringSplitIteratorNextFunction.AppendBasicBlock("end");
            var builder = new IRBuilder();

            LLVMTypeRef stringRefOptionType = LLVMExtensions.StringSliceReferenceType.CreateLLVMOptionType();
            LLVMValueRef spaceCharValue = ((byte)' ').AsLLVMValue();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef stringSplitIteratorPtr = stringSplitIteratorNextFunction.GetParam(0u),
                stringSplitIterator = builder.CreateLoad(stringSplitIteratorPtr, "stringSplitIterator"),
                stringSliceRef = builder.CreateExtractValue(stringSplitIterator, 0u, "stringSliceRef"),
                stringSliceBeginPtr = builder.CreateExtractValue(stringSliceRef, 0u, "stringSliceBeginPtr"),
                stringSliceLength = builder.CreateExtractValue(stringSliceRef, 1u, "stringSliceLength"),
                stringLimitPtr = builder.CreateGEP(stringSliceBeginPtr, new LLVMValueRef[] { stringSliceLength }, "stringLimitPtr"),
                initialPtr = builder.CreateExtractValue(stringSplitIterator, 1u, "initialPtr");
            builder.CreateBr(findSplitBeginLoopBeginBlock);

            builder.PositionBuilderAtEnd(findSplitBeginLoopBeginBlock);
            LLVMValueRef splitBeginPtr = builder.CreatePhi(LLVMExtensions.BytePointerType, "splitBeginPtr");
            LLVMValueRef splitBeginPtrDiff = builder.CreatePtrDiff(splitBeginPtr, stringLimitPtr, "splitBeginPtrDiff"),
                isSplitBeginPtrPastEnd = builder.CreateICmp(LLVMIntPredicate.LLVMIntSGE, splitBeginPtrDiff, 0L.AsLLVMValue(), "isSplitBeginPtrPastEnd");
            builder.CreateCondBr(isSplitBeginPtrPastEnd, returnNoneBlock, checkSplitBeginForSpaceBlock);

            builder.PositionBuilderAtEnd(checkSplitBeginForSpaceBlock);
            LLVMValueRef splitBeginChar = builder.CreateLoad(splitBeginPtr, "splitBeginChar"),
                isSplitBeginSpace = builder.CreateICmp(LLVMIntPredicate.LLVMIntEQ, splitBeginChar, spaceCharValue, "isSplitBeginSpace");
            builder.CreateCondBr(isSplitBeginSpace, advanceSplitBeginBlock, findSplitEndLoopBeginBlock);

            builder.PositionBuilderAtEnd(advanceSplitBeginBlock);
            LLVMValueRef splitBeginIncrementPtr = builder.CreateGEP(splitBeginPtr, new LLVMValueRef[] { 1.AsLLVMValue() }, "splitBeginIncrementPtr");
            splitBeginPtr.AddIncoming(initialPtr, entryBlock);
            splitBeginPtr.AddIncoming(splitBeginIncrementPtr, advanceSplitBeginBlock);
            builder.CreateBr(findSplitBeginLoopBeginBlock);

            builder.PositionBuilderAtEnd(findSplitEndLoopBeginBlock);
            LLVMValueRef splitLength = builder.CreatePhi(LLVMTypeRef.Int32Type(), "splitLength");
            LLVMValueRef splitEndPtr = builder.CreateGEP(splitBeginPtr, new LLVMValueRef[] { splitLength }, "splitEndPtr"),
                splitEndPtrDiff = builder.CreatePtrDiff(splitEndPtr, stringLimitPtr, "splitEndPtrDiff"),
                isSplitEndPtrPastEnd = builder.CreateICmp(LLVMIntPredicate.LLVMIntSGE, splitEndPtrDiff, 0L.AsLLVMValue(), "isSplitEndPtrPastEnd");
            builder.CreateCondBr(isSplitEndPtrPastEnd, returnSomeBlock, checkSplitEndForSpaceBlock);

            builder.PositionBuilderAtEnd(checkSplitEndForSpaceBlock);
            LLVMValueRef splitEndChar = builder.CreateLoad(splitEndPtr, "splitEndChar"),
                isSplitEndSpace = builder.CreateICmp(LLVMIntPredicate.LLVMIntEQ, splitEndChar, spaceCharValue, "isSplitEndSpace");
            builder.CreateCondBr(isSplitEndSpace, returnSomeBlock, advanceSplitEndBlock);

            builder.PositionBuilderAtEnd(advanceSplitEndBlock);
            LLVMValueRef splitLengthIncrement = builder.CreateAdd(splitLength, 1.AsLLVMValue(), "splitLengthIncrement");
            splitLength.AddIncoming(1.AsLLVMValue(), checkSplitBeginForSpaceBlock);
            splitLength.AddIncoming(splitLengthIncrement, advanceSplitEndBlock);
            builder.CreateBr(findSplitEndLoopBeginBlock);

            builder.PositionBuilderAtEnd(returnSomeBlock);
            LLVMValueRef someSplitSliceRef = builder.BuildOptionValue(stringRefOptionType, builder.BuildStringSliceReferenceValue(splitBeginPtr, splitLength));
            builder.CreateBr(endBlock);

            builder.PositionBuilderAtEnd(returnNoneBlock);
            LLVMValueRef noneSplitSliceRef = builder.BuildOptionValue(stringRefOptionType, null);
            builder.CreateBr(endBlock);

            builder.PositionBuilderAtEnd(endBlock);
            LLVMValueRef option = builder.CreatePhi(stringRefOptionType, "option");
            option.AddIncoming(someSplitSliceRef, returnSomeBlock);
            option.AddIncoming(noneSplitSliceRef, returnNoneBlock);
            LLVMValueRef finalPtr = builder.CreatePhi(LLVMExtensions.BytePointerType, "finalPtr");
            finalPtr.AddIncoming(splitEndPtr, returnSomeBlock);
            finalPtr.AddIncoming(splitBeginPtr, returnNoneBlock);
            LLVMValueRef optionPtr = stringSplitIteratorNextFunction.GetParam(1u);
            builder.CreateStore(option, optionPtr);
            LLVMValueRef stringSplitIteratorCurrentPtr = builder.CreateStructGEP(stringSplitIteratorPtr, 1u, "stringSplitIteratorCurrentPtr");
            builder.CreateStore(finalPtr, stringSplitIteratorCurrentPtr);
            builder.CreateRetVoid();
        }

        #endregion

        #region Range Module

        private static void CreateRangeModule(Module rangeModule)
        {
            AddFunction(RangeIteratorNextName, RangeModuleName, LLVMSharp.LLVM.FunctionType(
                LLVMTypeRef.VoidType(),
                new LLVMTypeRef[]
                {
                    LLVMTypeRef.PointerType(LLVMExtensions.RangeIteratorType, 0),
                    LLVMTypeRef.PointerType(LLVMTypeRef.Int32Type().CreateLLVMOptionType(), 0)
                },
                false));
            BuildRangeIteratorNextFunction(rangeModule);

            AddFunction(CreateRangeIteratorName, RangeModuleName, LLVMSharp.LLVM.FunctionType(
                LLVMTypeRef.Int32Type().CreateLLVMOptionType(),
                new LLVMTypeRef[]
                {
                    LLVMTypeRef.Int32Type(),
                    LLVMTypeRef.Int32Type(),
                    LLVMTypeRef.PointerType(LLVMExtensions.RangeIteratorType, 0)
                },
                false));
            BuildCreateRangeIteratorFunction(rangeModule);
        }

        private static void BuildRangeIteratorNextFunction(Module rangeModule)
        {
            LLVMValueRef rangeIteratorNextFunction = rangeModule.AddFunction(RangeIteratorNextName, CommonModuleSignatures[RangeIteratorNextName]);
            LLVMBasicBlockRef entryBlock = rangeIteratorNextFunction.AppendBasicBlock("entry");
            var builder = new IRBuilder();
            builder.PositionBuilderAtEnd(entryBlock);

            LLVMValueRef rangeIteratorPtr = rangeIteratorNextFunction.GetParam(0u),
                optionItemPtr = rangeIteratorNextFunction.GetParam(1u),
                rangeCurrentPtr = builder.CreateStructGEP(rangeIteratorPtr, 0u, "rangeCurrentPtr"),
                rangeHighPtr = builder.CreateStructGEP(rangeIteratorPtr, 1u, "rangeHighPtr"),
                rangeCurrent = builder.CreateLoad(rangeCurrentPtr, "rangeCurrent"),
                rangeHigh = builder.CreateLoad(rangeHighPtr, "rangeHigh"),
                rangeCurrentInc = builder.CreateAdd(rangeCurrent, 1.AsLLVMValue(), "rangeCurrentInc");
            builder.CreateStore(rangeCurrentInc, rangeCurrentPtr);
            LLVMValueRef inRange = builder.CreateICmp(LLVMIntPredicate.LLVMIntSLT, rangeCurrentInc, rangeHigh, "inRange");
            LLVMTypeRef optionType = LLVMTypeRef.Int32Type().CreateLLVMOptionType();
            LLVMValueRef option = builder.BuildStructValue(optionType, new LLVMValueRef[] { inRange, rangeCurrentInc }, "option");
            builder.CreateStore(option, optionItemPtr);
            builder.CreateRetVoid();
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

            AddFunction(OpenFileHandleName, FileModuleName, LLVMSharp.LLVM.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[]
                {
                    LLVMExtensions.StringSliceReferenceType,
                    LLVMTypeRef.PointerType(LLVMExtensions.FileHandleType.CreateLLVMOptionType(), 0),
                },
                false));
            BuildOpenFileHandleFunction(fileModule, externalFunctions);

            AddFunction(ReadLineFromFileHandleName, FileModuleName, LLVMSharp.LLVM.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[]
                {
                    LLVMTypeRef.PointerType(LLVMExtensions.FileHandleType, 0),
                    LLVMTypeRef.PointerType(LLVMExtensions.StringType.CreateLLVMOptionType(), 0),
                },
                false));
            BuildReadLineFromFileHandleFunction(fileModule, externalFunctions);

            AddFunction(WriteStringToFileHandleName, FileModuleName, LLVMSharp.LLVM.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[]
                {
                    LLVMTypeRef.PointerType(LLVMExtensions.FileHandleType, 0),
                    LLVMExtensions.StringSliceReferenceType,
                },
                false));
            BuildWriteStringToFileHandleFunction(fileModule, externalFunctions);

            AddFunction(DropFileHandleName, FileModuleName, LLVMSharp.LLVM.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[]
                {
                    LLVMTypeRef.PointerType(LLVMExtensions.FileHandleType, 0),
                },
                false));
            BuildDropFileHandleFunction(fileModule, externalFunctions);
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
            LLVMValueRef readLineFromFileHandleFunction = fileModule.AddFunction(ReadLineFromFileHandleName, CommonModuleSignatures[ReadLineFromFileHandleName]);
            LLVMBasicBlockRef entryBlock = readLineFromFileHandleFunction.AppendBasicBlock("entry");
            var builder = new IRBuilder();
            builder.PositionBuilderAtEnd(entryBlock);

            LLVMBasicBlockRef loopStartBlock = readLineFromFileHandleFunction.AppendBasicBlock("loopStart"),
                handleByteBlock = readLineFromFileHandleFunction.AppendBasicBlock("handleByte"),
                byteIsCRBlock = readLineFromFileHandleFunction.AppendBasicBlock("byteIsCR"),
                byteIsNotCRBlock = readLineFromFileHandleFunction.AppendBasicBlock("byteIsNotCR"),
                notNewLineBlock = readLineFromFileHandleFunction.AppendBasicBlock("notNewLine"),
                appendCRBlock = readLineFromFileHandleFunction.AppendBasicBlock("appendCR"),
                appendByteBlock = readLineFromFileHandleFunction.AppendBasicBlock("appendByte"),
                loopEndBlock = readLineFromFileHandleFunction.AppendBasicBlock("loopEnd"),
                nonEmptyStringBlock = readLineFromFileHandleFunction.AppendBasicBlock("nonEmptyString"),
                emptyStringBlock = readLineFromFileHandleFunction.AppendBasicBlock("emptyString");
                
            LLVMValueRef fileHandlePtr = readLineFromFileHandleFunction.GetParam(0u),
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
            LLVMValueRef optionStringPtr = readLineFromFileHandleFunction.GetParam(1u),
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
            LLVMValueRef dropFileHandleFunction = fileModule.AddFunction(DropFileHandleName, CommonModuleSignatures[DropFileHandleName]);
            LLVMBasicBlockRef entryBlock = dropFileHandleFunction.AppendBasicBlock("entry");
            var builder = new IRBuilder();
            builder.PositionBuilderAtEnd(entryBlock);

            LLVMValueRef fileHandleStructPtr = dropFileHandleFunction.GetParam(0u),
                fileHandlePtr = builder.CreateStructGEP(fileHandleStructPtr, 0u, "fileHandlePtr"),
                fileHandle = builder.CreateLoad(fileHandlePtr, "fileHandle");
            builder.CreateCall(
                externalFunctions.CloseHandleFunction,
                new LLVMValueRef[] { fileHandle },
                "closeHandleResult");
            builder.CreateRetVoid();
        }

        #endregion

        public static void CreateCallToCopyMemory(this IRBuilder builder, CommonExternalFunctions externalFunctions, LLVMValueRef destinationPtr, LLVMValueRef sourcePtr, LLVMValueRef bytesToCopy)
        {
            LLVMValueRef bytesToCopyExtend = builder.CreateSExt(bytesToCopy, LLVMTypeRef.Int64Type(), "bytesToCopyExtend"),
                sourcePtrCast = builder.CreateBitCast(sourcePtr, LLVMExtensions.BytePointerType, "sourcePtrCast"),
                destinationPtrCast = builder.CreateBitCast(destinationPtr, LLVMExtensions.BytePointerType, "destinationPtrCast");
            builder.CreateCall(externalFunctions.CopyMemoryFunction, new LLVMValueRef[] { destinationPtrCast, sourcePtrCast, bytesToCopyExtend }, string.Empty);
        }
    }

    public class CommonExternalFunctions
    {
        public CommonExternalFunctions(Module addTo)
        {
            // NB: this will get resolved to the Win32 RtlCopyMemory function.
            LLVMTypeRef copyMemoryFunctionType = LLVMSharp.LLVM.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[] { LLVMExtensions.BytePointerType, LLVMExtensions.BytePointerType, LLVMTypeRef.Int64Type() },
                false);
            CopyMemoryFunction = addTo.AddFunction("CopyMemory", copyMemoryFunctionType);
            CopyMemoryFunction.SetLinkage(LLVMLinkage.LLVMExternalLinkage);

            LLVMTypeRef scheduleFunctionType = LLVMTypeRef.FunctionType(
                LLVMTypeRef.VoidType(),
                new LLVMTypeRef[] { LLVMTypeRef.PointerType(LLVMExtensions.ScheduledTaskFunctionType, 0u), LLVMExtensions.VoidPointerType },
                false);
            ScheduleFunction = addTo.AddFunction("schedule", scheduleFunctionType);
            ScheduleFunction.SetLinkage(LLVMLinkage.LLVMExternalLinkage);

            FakeDropFunction = CreateSingleParameterVoidFunction(addTo, LLVMTypeRef.Int32Type(), "fake_drop");

            LLVMTypeRef outputStringFunctionType = LLVMSharp.LLVM.FunctionType(
                LLVMSharp.LLVM.VoidType(),
                new LLVMTypeRef[] { LLVMExtensions.BytePointerType, LLVMTypeRef.Int32Type() },
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
                    LLVMExtensions.BytePointerType,    // filename
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

        public LLVMValueRef ScheduleFunction { get; }

        public LLVMValueRef OutputStringFunction { get; }

        public LLVMValueRef FakeDropFunction { get; }

        public LLVMValueRef CloseHandleFunction { get; }

        public LLVMValueRef CreateFileAFunction { get; }

        public LLVMValueRef ReadFileFunction { get; }

        public LLVMValueRef WriteFileFunction { get; }
    }
}
