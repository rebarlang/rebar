using System.Collections.Generic;
using System.IO;
using LLVMSharp;

namespace Rebar.RebarTarget.LLVM
{
    internal static class CommonModules
    {
        public static Module FakeDropModule { get; }
        public static Module SchedulerModule { get; }
        public static Module StringModule { get; }
        public static Module OutputModule { get; }
        public static Module RangeModule { get; }
        public static Module FileModule { get; }

        public static Dictionary<string, LLVMTypeRef> CommonModuleSignatures { get; }

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
            string assemblyDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string llvmResourcesPath = Path.Combine(assemblyDirectory, "RebarTarget", "LLVM", "Resources");

            var fakeDropNames = new string[]
            {
                FakeDropCreateName,
                FakeDropDropName
            };
            FakeDropModule = LoadModuleAndRegisterFunctionNames(Path.Combine(llvmResourcesPath, "fakedrop.bc"), fakeDropNames);

            var schedulerNames = new string[]
            {
                PartialScheduleName,
                InvokeName
            };
            SchedulerModule = LoadModuleAndRegisterFunctionNames(Path.Combine(llvmResourcesPath, "scheduler.bc"), schedulerNames);

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
            OutputModule = LoadModuleAndRegisterFunctionNames(Path.Combine(llvmResourcesPath, "output.bc"), outputNames);

            var rangeNames = new string[]
            {
                CreateRangeIteratorName,
                RangeIteratorNextName
            };
            RangeModule = LoadModuleAndRegisterFunctionNames(Path.Combine(llvmResourcesPath, "range.bc"), rangeNames);

            var stringNames = new string[]
            {
                CopySliceToPointerName,
                CreateEmptyStringName,
                CreateNullTerminatedStringFromSliceName,
                DropStringName,
                StringCloneName,
                OutputStringSliceName,
                StringFromSliceName,
                StringToSliceRetName,
                StringToSliceName,
                StringAppendName,
                StringConcatName,
                StringSliceToStringSplitIteratorName,
                StringSplitIteratorNextName,
            };
            StringModule = LoadModuleAndRegisterFunctionNames(Path.Combine(llvmResourcesPath, "string.bc"), stringNames);

            var fileNames = new string[]
            {
                DropFileHandleName,
                OpenFileHandleName,
                ReadLineFromFileHandleName,
                WriteStringToFileHandleName
            };
            FileModule = LoadModuleAndRegisterFunctionNames(Path.Combine(llvmResourcesPath, "file.bc"), fileNames);
        }

        private static Module LoadModuleAndRegisterFunctionNames(string modulePath, IEnumerable<string> functionNames)
        {
            Module module = File.ReadAllBytes(modulePath).DeserializeModuleAsBitcode();
            foreach (string name in functionNames)
            {
                CommonModuleSignatures[name] = module.GetNamedFunction(name).TypeOf().GetElementType();
            }
            return module;
        }

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
