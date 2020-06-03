using System;
using LLVMSharp;
using NationalInstruments.DataTypes;
using Rebar.Common;

namespace Rebar.RebarTarget.LLVM
{
    internal static class TraitHelpers
    {
        #region Common

        private static Func<FunctionModuleContext, LLVMValueRef> MakeCommonFunctionImporter(string functionName)
        {
            return moduleContext => moduleContext.FunctionImporter.GetImportedCommonFunction(functionName);
        }

        #endregion

        #region Drop

        public static bool TypeHasDropFunction(NIType type)
        {
            Func<FunctionModuleContext, LLVMValueRef> dropFunctionCreator;
            return TryGetDropFunctionCreator(type, out dropFunctionCreator);
        }

        public static bool TryGetDropFunction(NIType type, FunctionModuleContext moduleContext, out LLVMValueRef dropFunction)
        {
            dropFunction = default(LLVMValueRef);
            Func<FunctionModuleContext, LLVMValueRef> dropFunctionCreator;
            if (TryGetDropFunctionCreator(type, out dropFunctionCreator))
            {
                dropFunction = dropFunctionCreator(moduleContext);
                return true;
            }
            return false;
        }

        public static void CreateDropCallIfDropFunctionExists(
            this FunctionModuleContext moduleContext,
            IRBuilder builder,
            NIType droppedValueType,
            Func<IRBuilder, LLVMValueRef> getDroppedValuePtr)
        {
            LLVMValueRef dropFunction;
            if (TraitHelpers.TryGetDropFunction(droppedValueType, moduleContext, out dropFunction))
            {
                LLVMValueRef droppedValuePtr = getDroppedValuePtr(builder);
                builder.CreateCall(dropFunction, new LLVMValueRef[] { droppedValuePtr }, string.Empty);
            }
        }

        private static NIType SpecializeDropSignature(NIType droppedValueType)
        {
            var functionBuilder = Signatures.DropType.DefineFunctionFromExisting();
            functionBuilder.ReplaceGenericParameters(droppedValueType, NIType.Unset);
            return functionBuilder.CreateType();
        }

        private static Func<FunctionModuleContext, LLVMValueRef> MakeDropFunctionSpecializer(
            NIType droppedValueType,
            Action<FunctionModuleContext, NIType, LLVMValueRef> specializedFunctionCreator)
        {
            return moduleContext => moduleContext.GetSpecializedFunctionWithSignature(
                SpecializeDropSignature(droppedValueType),
                specializedFunctionCreator);
        }

        private static bool TryGetDropFunctionCreator(NIType droppedValueType, out Func<FunctionModuleContext, LLVMValueRef> dropFunctionCreator)
        {
            dropFunctionCreator = null;
            var functionBuilder = Signatures.DropType.DefineFunctionFromExisting();
            functionBuilder.ReplaceGenericParameters(droppedValueType, NIType.Unset);
            NIType signature = functionBuilder.CreateType();

            NIType innerType;
            if (droppedValueType == NITypes.String)
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

        #endregion

        #region Clone

        public static bool TryGetCloneFunction(this FunctionModuleContext moduleContext, NIType valueType, out LLVMValueRef cloneFunction)
        {
            cloneFunction = default(LLVMValueRef);
            var functionBuilder = Signatures.CreateCopyType.DefineFunctionFromExisting();
            functionBuilder.Name = "Clone";
            functionBuilder.ReplaceGenericParameters(valueType, NIType.Unset);
            NIType signature = functionBuilder.CreateType();
            NIType innerType;
            if (valueType == NITypes.String)
            {
                cloneFunction = moduleContext.FunctionImporter.GetImportedCommonFunction(CommonModules.StringCloneName);
                return true;
            }
            if (valueType.TryDestructureSharedType(out innerType))
            {
                cloneFunction = moduleContext.GetSpecializedFunctionWithSignature(signature, FunctionCompiler.BuildSharedCloneFunction);
                return true;
            }
            if (valueType.TryDestructureVectorType(out innerType))
            {
                cloneFunction = moduleContext.GetSpecializedFunctionWithSignature(signature, FunctionCompiler.BuildVectorCloneFunction);
                return true;
            }

            if (valueType.TypeHasCloneTrait())
            {
                throw new NotSupportedException("Clone function not found for type: " + valueType);
            }
            return false;
        }

        #endregion

        #region Iterator

        public static LLVMValueRef GetIteratorNextFunction(this FunctionModuleContext moduleContext, NIType iteratorType, NIType iteratorNextSignature)
        {
            if (iteratorType == DataTypes.RangeIteratorType)
            {
                return moduleContext.FunctionImporter.GetImportedCommonFunction(CommonModules.RangeIteratorNextName);
            }
            if (iteratorType.IsStringSplitIteratorType())
            {
                return moduleContext.FunctionImporter.GetImportedCommonFunction(CommonModules.StringSplitIteratorNextName);
            }
            NIType innerType;
            if (iteratorType.TryDestructureSliceIteratorType(out innerType)
                || iteratorType.TryDestructureSliceMutableIteratorType(out innerType))
            {
                return moduleContext.GetSpecializedFunctionWithSignature(
                    iteratorNextSignature,
                    FunctionCompiler.CreateSliceIteratorNextFunction);
            }

            throw new NotSupportedException("Missing Iterator::Next method for type " + iteratorType);
        }

        #endregion

        #region Promise

        public static LLVMValueRef GetPromisePollFunction(this FunctionModuleContext moduleContext, NIType type)
        {
            NIType innerType;
            if (type.TryDestructureYieldPromiseType(out innerType))
            {
                NIType signature = Signatures.PromisePollType.ReplaceGenericParameters(type, innerType, NIType.Unset);
                return moduleContext.GetSpecializedFunctionWithSignature(signature, FunctionCompiler.BuildYieldPromisePollFunction);
            }
            if (type.TryDestructureMethodCallPromiseType(out innerType))
            {
                NIType signature = Signatures.PromisePollType.ReplaceGenericParameters(type, innerType, NIType.Unset);
                return moduleContext.GetSpecializedFunctionWithSignature(signature, FunctionCompiler.BuildMethodCallPromisePollFunction);
            }
            if (type.TryDestructureNotifierReaderPromiseType(out innerType))
            {
                NIType signature = Signatures.PromisePollType.ReplaceGenericParameters(type, innerType.CreateOption(), NIType.Unset);
                return moduleContext.GetSpecializedFunctionWithSignature(signature, FunctionCompiler.BuildNotifierReaderPromisePollFunction);
            }
            throw new NotSupportedException("Cannot find poll function for type " + type);
        }

        #endregion
    }
}
