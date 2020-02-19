using System.Collections.Generic;
using System.IO;
using LLVMSharp;

namespace Rebar.RebarTarget.LLVM
{
    internal static class CommonModules
    {
        public static Module ExternsModule { get; }
        public static Module FakeDropModule { get; }
        public static Module SchedulerModule { get; }
        public static Module StringModule { get; }
        public static Module OutputModule { get; }
        public static Module RangeModule { get; }
        public static Module FileModule { get; }

        public static Dictionary<string, LLVMTypeRef> CommonModuleSignatures { get; }

        // NB: this will get resolved to the Win32 RtlCopyMemory function.
        public const string CopyMemoryName = "CopyMemory";
        public const string ScheduleName = "schedule";

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

            var externsNames = new string[]
            {
                CopyMemoryName,
                ScheduleName
            };
            ExternsModule = LoadModuleAndRegisterFunctionNames(Path.Combine(llvmResourcesPath, "externs.bc"), externsNames);

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
    }
}
