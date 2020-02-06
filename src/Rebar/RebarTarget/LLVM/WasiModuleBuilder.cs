using LLVMSharp;

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

            LLVMTypeRef startFunctionType = LLVMTypeRef.FunctionType(LLVMTypeRef.VoidType(), new LLVMTypeRef[0], false);
            LLVMValueRef startFunction = wasiExecutableModule.AddFunction("_start", startFunctionType);
            var builder = new IRBuilder();
            builder.PositionBuilderAtEnd(startFunction.AppendBasicBlock("entry"));
            LLVMValueRef cloneFuncValue = wasiExecutableModule.GetNamedFunction(topLevelFunctionName);
            builder.CreateCall(
                cloneFuncValue,
                new LLVMValueRef[]
                {
                    // TODO: this would need to be a real waker function
                    LLVMSharp.LLVM.ConstNull(LLVMTypeRef.PointerType(LLVMExtensions.ScheduledTaskFunctionType, 0u)),
                    LLVMSharp.LLVM.ConstNull(LLVMExtensions.VoidPointerType)
                },
                string.Empty);
            builder.CreateRetVoid();

            // TODO: this will move to CommonModules.OutputModule
            LLVMValueRef outputStringFunction = wasiExecutableModule.GetNamedFunction("output_string");
            LLVMValueRef trueConstantPtr = wasiExecutableModule.DefineStringGlobalInModule("trueString", "true"),
                falseConstantPtr = wasiExecutableModule.DefineStringGlobalInModule("falseString", "false");
            LLVMValueRef outputBoolFunction = wasiExecutableModule.GetNamedFunction("output_bool");
            builder.PositionBuilderAtEnd(outputBoolFunction.AppendBasicBlock("entry"));
            LLVMValueRef trueConstantCast = builder.CreateBitCast(trueConstantPtr, LLVMExtensions.BytePointerType, "trueConstantCast"),
                falseConstantCast = builder.CreateBitCast(falseConstantPtr, LLVMExtensions.BytePointerType, "falseConstantCast"),
                selectedConstantPtr = builder.CreateSelect(outputBoolFunction.GetParam(0u), trueConstantCast, falseConstantCast, "selectedConstantPtr"),
                selectedConstantSize = builder.CreateSelect(outputBoolFunction.GetParam(0u), 4.AsLLVMValue(), 5.AsLLVMValue(), "selectedConstantSize");
            builder.CreateCall(
                outputStringFunction,
                new LLVMValueRef[] { selectedConstantPtr, selectedConstantSize },
                string.Empty);
            builder.CreateRetVoid();

            wasiExecutableModule.VerifyAndThrowIfInvalid();
            return wasiExecutableModule;
        }
    }
}
