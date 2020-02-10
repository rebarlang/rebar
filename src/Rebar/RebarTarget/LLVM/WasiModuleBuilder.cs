using System.Collections.Generic;
using System.Diagnostics;
using LLVMSharp;
using NationalInstruments.Core.IO;

namespace Rebar.RebarTarget.LLVM
{
    internal static class WasiModuleBuilder
    {
        public static Module CreateWasiExecutableModule(Module topLevelModule, string topLevelFunctionName)
        {
            Module wasiExecutableModule = topLevelModule.Clone();
            // NOTE: required for wasm-ld to work
            wasiExecutableModule.SetTarget("wasm32-unknown-unknown-wasm");
            wasiExecutableModule.SetDataLayout("e-m:e-p:32:32-i64:64-n32:64-S128");

            wasiExecutableModule.LinkInModule(WasiExternsModule.WasiExterns.Clone());

            AddEntryPointCallerFunctionToModule(
                wasiExecutableModule,
                "_start",
                wasiExecutableModule.GetNamedFunction(topLevelFunctionName),
                LLVMTypeRef.VoidType(),
                new LLVMTypeRef[0]);

            wasiExecutableModule.VerifyAndThrowIfInvalid();
            return wasiExecutableModule;
        }

        public static Module CreateWasiLibraryModule(Module topLevelModule, string topLevelFunctionName)
        {
            Module wasiLibraryModule = topLevelModule.Clone();
            // NOTE: required for wasm-ld to work
            wasiLibraryModule.SetTarget("wasm32-unknown-unknown-wasm");
            wasiLibraryModule.SetDataLayout("e-m:e-p:32:32-i64:64-n32:64-S128");

            // wasiExecutableModule.LinkInModule(WasiExternsModule.WasiExterns.Clone());
            AddEntryPointCallerFunctionToModule(
                wasiLibraryModule,
                topLevelFunctionName + "entry",
                wasiLibraryModule.GetNamedFunction(topLevelFunctionName),
                LLVMTypeRef.Int32Type(),
                new LLVMTypeRef[] { LLVMTypeRef.Int32Type(), LLVMTypeRef.Int32Type() });

            wasiLibraryModule.VerifyAndThrowIfInvalid();
            return wasiLibraryModule;
        }

        private static LLVMValueRef AddEmptyWakerFunctionToModule(Module module)
        {
            LLVMValueRef emptyWakerFunction = module.AddFunction("emptyWaker", LLVMExtensions.ScheduledTaskFunctionType);
            var builder = new IRBuilder();
            builder.PositionBuilderAtEnd(emptyWakerFunction.AppendBasicBlock("entry"));
            builder.CreateRetVoid();
            return emptyWakerFunction;
        }

        private static void AddEntryPointCallerFunctionToModule(Module module, string name, LLVMValueRef target, LLVMTypeRef returnType, LLVMTypeRef[] parameterTypes)
        {
            LLVMValueRef emptyWakerFunction = AddEmptyWakerFunctionToModule(module);

            bool isVoidResult = returnType.TypeKind == LLVMTypeKind.LLVMVoidTypeKind;
            LLVMTypeRef entryPointType = LLVMTypeRef.FunctionType(LLVMTypeRef.Int32Type(), new LLVMTypeRef[] { LLVMTypeRef.Int32Type(), LLVMTypeRef.Int32Type() }, false);
            LLVMValueRef entryPointFunction = module.AddFunction(name, entryPointType);
            var builder = new IRBuilder();
            builder.PositionBuilderAtEnd(entryPointFunction.AppendBasicBlock("entry"));
            LLVMValueRef resultAlloca = isVoidResult
                ? default(LLVMValueRef)
                : builder.CreateAlloca(LLVMTypeRef.Int32Type(), "resultPtr");
            var arguments = new List<LLVMValueRef>() { emptyWakerFunction, LLVMSharp.LLVM.ConstNull(LLVMExtensions.VoidPointerType) };
            for (uint i = 0; i < parameterTypes.Length; ++i)
            {
                arguments.Add(entryPointFunction.GetParam(i));
            }
            if (!isVoidResult)
            {
                arguments.Add(resultAlloca);
            }
            builder.CreateCall(target, arguments.ToArray(), string.Empty);
            if (isVoidResult)
            {
                builder.CreateRetVoid();
            }
            else
            {
                builder.CreateRet(builder.CreateLoad(resultAlloca, "result"));
            }
        }

        public static void LinkWasmModule(Module wasmModule, string wasmModuleFilePath, bool containsEntryPoint)
        {
            string bitcodeFilePath = LongPath.ChangeExtension(wasmModuleFilePath, ".bc");
            int ret = wasmModule.WriteBitcodeToFile(bitcodeFilePath);

            // TODO: this needs to be configurable
            const string wasmLinkerPath = @"G:\Program Files\LLVM\bin\wasm-ld.exe";
            // TODO: create a file for allowed undefined symbols
            string noEntryOption = containsEntryPoint ? string.Empty : " --no-entry";
            string export = " --export=entryentry"; // " --export-all"; // "--export=entry";
            string wasmLinkerCommandArguments = $"\"{bitcodeFilePath}\" -o \"{wasmModuleFilePath}\" --allow-undefined{noEntryOption}{export}";
            var process = Process.Start(new ProcessStartInfo(wasmLinkerPath, wasmLinkerCommandArguments));
            process.WaitForExit();
        }
    }
}
