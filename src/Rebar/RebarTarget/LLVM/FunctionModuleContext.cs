using System;
using LLVMSharp;
using NationalInstruments.DataTypes;

namespace Rebar.RebarTarget.LLVM
{
    /// <summary>
    /// Shared object for creating auxiliary or specialized functions within the context of a <see cref="Module"/> for a single Function.
    /// </summary>
    internal sealed class FunctionModuleContext
    {
        public FunctionModuleContext(ContextWrapper llvmContext, Module module, FunctionImporter functionImporter)
        {
            LLVMContext = llvmContext;
            Module = module;
            FunctionImporter = functionImporter;
        }

        public ContextWrapper LLVMContext { get; }

        public Module Module { get; }

        public FunctionImporter FunctionImporter { get; }
    }

    internal static class FunctionModuleContextExtensions
    {
        public static LLVMValueRef GetSpecializedFunctionWithSignature(this FunctionModuleContext moduleContext, NIType specializedSignature, Action<FunctionModuleContext, NIType, LLVMValueRef> createFunction)
        {
            string specializedFunctionName = specializedSignature.MonomorphizeFunctionName();
            return moduleContext.FunctionImporter.GetCachedFunction(specializedFunctionName, () =>
            {
                LLVMValueRef function = moduleContext.Module.AddFunction(specializedFunctionName, moduleContext.LLVMContext.TranslateFunctionType(specializedSignature));
                createFunction(moduleContext, specializedSignature, function);
                return function;
            });
        }

        public static void CreateCallToCopyMemory(this FunctionModuleContext moduleContext, IRBuilder builder, LLVMValueRef destinationPtr, LLVMValueRef sourcePtr, LLVMValueRef bytesToCopy)
        {
            LLVMValueRef bytesToCopyExtend = builder.CreateSExt(bytesToCopy, moduleContext.LLVMContext.Int64Type, "bytesToCopyExtend"),
                sourcePtrCast = builder.CreateBitCast(sourcePtr, moduleContext.LLVMContext.BytePointerType(), "sourcePtrCast"),
                destinationPtrCast = builder.CreateBitCast(destinationPtr, moduleContext.LLVMContext.BytePointerType(), "destinationPtrCast");
            builder.CreateCall(
                moduleContext.FunctionImporter.GetImportedCommonFunction(CommonModules.CopyMemoryName),
                new LLVMValueRef[] { destinationPtrCast, sourcePtrCast, bytesToCopyExtend },
                string.Empty);
        }
    }
}
