using LLVMSharp;

namespace Rebar.RebarTarget.LLVM
{
    internal static class WasiExternsModule
    {
        public static Module WasiExterns { get; }

        static WasiExternsModule()
        {
            WasiExterns = new Module("wasi_externs");
            BuildWasiExternsModule(WasiExterns);
        }

        private static void SetFunctionAsImport(this LLVMValueRef function, string importModule, string importName)
        {
            function.AddTargetDependentFunctionAttr("wasm-import-module", importModule);
            function.AddTargetDependentFunctionAttr("wasm-import-name", importName);
            function.SetDLLStorageClass(LLVMDLLStorageClass.LLVMDLLImportStorageClass);
        }

        private static void BuildWasiExternsModule(Module module)
        {
            // size_t __wasi_fd_write(__wasi_fd_t fd, const __wasi_ciovec_t *iovs, size_t iovs_len)
            // typedef uint32 __wasi_fd_t;
            // typedef struct { const void *buf, size_t buf_len } __wasi_ciovec_t;
            LLVMTypeRef sizeType = LLVMTypeRef.Int32Type();

            LLVMTypeRef iovecType = module.GetModuleContext().StructCreateNamed("__wasi_ciovec_t");
            iovecType.StructSetBody(new LLVMTypeRef[] { LLVMExtensions.VoidPointerType, sizeType }, false);

            LLVMTypeRef fileDescriptorWriteType = LLVMTypeRef.FunctionType(
                sizeType,
                new LLVMTypeRef[] { LLVMTypeRef.Int32Type(), LLVMTypeRef.PointerType(iovecType, 0u), sizeType },
                false);
            LLVMValueRef fileDescriptorWriteFunction = module.AddFunction("wasi_fd_write", fileDescriptorWriteType);
            fileDescriptorWriteFunction.SetFunctionAsImport("wasi_unstable", "fd_write");

            LLVMTypeRef outputStringType = LLVMTypeRef.FunctionType(
                LLVMTypeRef.VoidType(),
                new LLVMTypeRef[] { LLVMExtensions.BytePointerType, LLVMTypeRef.Int32Type() },
                false);
            LLVMValueRef outputStringFunction = module.AddFunction("output_string", outputStringType);
            var builder = new IRBuilder();
            builder.PositionBuilderAtEnd(outputStringFunction.AppendBasicBlock("entry"));
            LLVMValueRef iovecPtr = builder.CreateAlloca(iovecType, "localIovec"),
                iovec = builder.BuildStructValue(iovecType, new LLVMValueRef[] { outputStringFunction.GetParam(0u), outputStringFunction.GetParam(1) });
            builder.CreateStore(iovec, iovecPtr);
            builder.CreateCall(
                fileDescriptorWriteFunction,
                new LLVMValueRef[]
                {
                    1.AsLLVMValue(),    // STDOUT
                    iovecPtr,           // iovs
                    1.AsLLVMValue()     // iovs_len
                },
                "bytesWritten");
            builder.CreateRetVoid();

            module.VerifyAndThrowIfInvalid();
        }
    }
}
