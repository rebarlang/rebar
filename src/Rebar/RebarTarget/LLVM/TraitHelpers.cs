using System;
using LLVMSharp;
using NationalInstruments.DataTypes;
using Rebar.Common;

namespace Rebar.RebarTarget.LLVM
{
    internal static class TraitHelpers
    {
        public static bool TypeHasDropFunction(NIType type)
        {
            Func<FunctionCompiler, LLVMValueRef> dropFunctionCreator;
            return TryGetDropFunctionCreator(type, out dropFunctionCreator);
        }

        public static bool TryGetDropFunction(NIType type, FunctionCompiler compiler, out LLVMValueRef dropFunction)
        {
            dropFunction = default(LLVMValueRef);
            Func<FunctionCompiler, LLVMValueRef> dropFunctionCreator;
            if (TryGetDropFunctionCreator(type, out dropFunctionCreator))
            {
                dropFunction = dropFunctionCreator(compiler);
                return true;
            }
            return false;
        }

        private static Func<FunctionCompiler, LLVMValueRef> MakeCommonFunctionImporter(string functionName)
        {
            return compiler => compiler.GetImportedCommonFunction(functionName);
        }

        private static NIType SpecializeDropSignature(NIType droppedValueType)
        {
            var functionBuilder = Signatures.DropType.DefineFunctionFromExisting();
            functionBuilder.ReplaceGenericParameters(droppedValueType, NIType.Unset);
            return functionBuilder.CreateType();
        }

        private static Func<FunctionCompiler, LLVMValueRef> MakeDropFunctionSpecializer(
            NIType droppedValueType,
            Action<FunctionCompiler, NIType, LLVMValueRef> specializedFunctionCreator)
        {
            return compiler => compiler.GetSpecializedFunctionWithSignature(
                SpecializeDropSignature(droppedValueType),
                specializedFunctionCreator);
        }

        private static bool TryGetDropFunctionCreator(NIType droppedValueType, out Func<FunctionCompiler, LLVMValueRef> dropFunctionCreator)
        {
            dropFunctionCreator = null;
            var functionBuilder = Signatures.DropType.DefineFunctionFromExisting();
            functionBuilder.ReplaceGenericParameters(droppedValueType, NIType.Unset);
            NIType signature = functionBuilder.CreateType();

            NIType innerType;
            if (droppedValueType == PFTypes.String)
            {
                dropFunctionCreator = MakeCommonFunctionImporter(CommonModules.DropStringName);
                return true;
            }
            if (droppedValueType == DataTypes.FileHandleType)
            {
                dropFunctionCreator = MakeCommonFunctionImporter(CommonModules.DropFileHandleName);
                return true;
            }
            if (droppedValueType == DataTypes.FakeDropType)
            {
                dropFunctionCreator = MakeCommonFunctionImporter(CommonModules.FakeDropDropName);
                return true;
            }
            if (droppedValueType.IsVectorType())
            {
                dropFunctionCreator = MakeDropFunctionSpecializer(droppedValueType, FunctionCompiler.BuildVectorDropFunction);
                return true;
            }
            if (droppedValueType.TryDestructureOptionType(out innerType) && TypeHasDropFunction(innerType))
            {
                dropFunctionCreator = MakeDropFunctionSpecializer(droppedValueType, FunctionCompiler.BuildOptionDropFunction);
                return true;
            }
            if (droppedValueType.IsSharedType())
            {
                dropFunctionCreator = MakeDropFunctionSpecializer(droppedValueType, FunctionCompiler.BuildSharedDropFunction);
                return true;
            }
            if (droppedValueType.TryDestructureNotifierReaderType(out innerType))
            {
                dropFunctionCreator = MakeDropFunctionSpecializer(droppedValueType, FunctionCompiler.BuildNotifierReaderDropFunction);
                return true;
            }
            if (droppedValueType.TryDestructureNotifierWriterType(out innerType))
            {
                dropFunctionCreator = MakeDropFunctionSpecializer(droppedValueType, FunctionCompiler.BuildNotifierWriterDropFunction);
                return true;
            }

            if (droppedValueType.TypeHasDropTrait())
            {
                throw new NotSupportedException("Drop function not found for type: " + droppedValueType);
            }
            return false;
        }
    }
}
