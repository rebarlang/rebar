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

        public static LLVMValueRef CreateMalloc(this FunctionModuleContext moduleContext, IRBuilder builder, LLVMTypeRef type, string name)
        {
            LLVMValueRef mallocFunction = moduleContext.FunctionImporter.GetCachedFunction("malloc", moduleContext.CreateMallocFunction),
                mallocCall = builder.CreateCall(mallocFunction, new LLVMValueRef[] { type.SizeOf() }, "malloCcall");
            return builder.CreateBitCast(mallocCall, LLVMTypeRef.PointerType(type, 0u), name);
        }

        public static LLVMValueRef CreateArrayMalloc(this FunctionModuleContext moduleContext, IRBuilder builder, LLVMTypeRef type, LLVMValueRef size, string name)
        {
            LLVMValueRef bitCastSize = builder.CreateZExtOrBitCast(size, moduleContext.LLVMContext.Int64Type, "bitCastSize"),
                mallocSize = builder.CreateMul(type.SizeOf(), bitCastSize, "mallocSize"),
                mallocFunction = moduleContext.FunctionImporter.GetCachedFunction("malloc", moduleContext.CreateMallocFunction),
                mallocCall = builder.CreateCall(mallocFunction, new LLVMValueRef[] { mallocSize }, "malloCcall");
            return builder.CreateBitCast(mallocCall, LLVMTypeRef.PointerType(type, 0u), name);
        }

        private static LLVMValueRef CreateMallocFunction(this FunctionModuleContext moduleContext)
        {
            return moduleContext.Module.AddFunction(
                "malloc",
                // TODO: should ideally use the native integer size of the context
                LLVMTypeRef.FunctionType(moduleContext.LLVMContext.VoidPointerType(), new LLVMTypeRef[] { moduleContext.LLVMContext.Int64Type }, IsVarArg: false));
        }
    }
}
