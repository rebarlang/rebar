using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NationalInstruments.DataTypes;
using Rebar.Common;

namespace Rebar.RebarTarget.LLVM
{
    internal static class FunctionNames
    {
        public static string GetSynchronousFunctionName(string functionName)
        {
            return $"{functionName}::sync";
        }

        public static string GetInitializeStateFunctionName(string runtimeFunctionName)
        {
            return $"{runtimeFunctionName}::InitializeState";
        }

        public static string GetPollFunctionName(string runtimeFunctionName)
        {
            return $"{runtimeFunctionName}::Poll";
        }

        public static string MonomorphizeFunctionName(string functionName, IEnumerable<NIType> typeArguments)
        {
            var nameBuilder = new StringBuilder(functionName);
            foreach (NIType typeArgument in typeArguments)
            {
                nameBuilder.Append("_");
                nameBuilder.Append(StringifyType(typeArgument));
            }
            return nameBuilder.ToString();
        }

        public static string MonomorphizeFunctionName(this NIType signatureType)
        {
            return MonomorphizeFunctionName(signatureType.GetName(), signatureType.GetGenericParameters());
        }

        private static string StringifyType(NIType type)
        {
            switch (type.GetKind())
            {
                case NITypeKind.UInt8:
                    return "u8";
                case NITypeKind.Int8:
                    return "i8";
                case NITypeKind.UInt16:
                    return "u16";
                case NITypeKind.Int16:
                    return "i16";
                case NITypeKind.UInt32:
                    return "u32";
                case NITypeKind.Int32:
                    return "i32";
                case NITypeKind.UInt64:
                    return "u64";
                case NITypeKind.Int64:
                    return "i64";
                case NITypeKind.Boolean:
                    return "bool";
                case NITypeKind.String:
                    return "string";
                default:
                    {
                        if (type.IsRebarReferenceType())
                        {
                            NIType referentType = type.GetReferentType();
                            if (referentType == DataTypes.StringSliceType)
                            {
                                return "str";
                            }
                            NIType sliceElementType;
                            if (referentType.TryDestructureSliceType(out sliceElementType))
                            {
                                return $"slice[{StringifyType(sliceElementType)}]";
                            }
                            return $"ref[{StringifyType(referentType)}]";
                        }
                        if (type.IsCluster())
                        {
                            string fieldStrings = string.Join(",", type.GetFields().Select(t => StringifyType(t.GetDataType())));
                            return $"{{{fieldStrings}}}";
                        }
                        if (type == DataTypes.FileHandleType)
                        {
                            return "filehandle";
                        }
                        if (type == DataTypes.FakeDropType)
                        {
                            return "fakedrop";
                        }
                        NIType innerType;
                        if (type.TryDestructureOptionType(out innerType))
                        {
                            return $"option[{StringifyType(innerType)}]";
                        }
                        if (type.TryDestructureVectorType(out innerType))
                        {
                            return $"vec[{StringifyType(innerType)}]";
                        }
                        if (type.TryDestructureSharedType(out innerType))
                        {
                            return $"shared[{StringifyType(innerType)}]";
                        }
                        if (type.TryDestructureYieldPromiseType(out innerType))
                        {
                            return $"yieldPromise[{StringifyType(innerType)}]";
                        }
                        if (type.TryDestructureMethodCallPromiseType(out innerType))
                        {
                            return $"methodCallPromise[{StringifyType(innerType)}]";
                        }
                        if (type.TryDestructureNotifierReaderType(out innerType))
                        {
                            return $"notifierReader[{StringifyType(innerType)}]";
                        }
                        if (type.TryDestructureNotifierWriterType(out innerType))
                        {
                            return $"notifierWriter[{StringifyType(innerType)}]";
                        }
                        if (type.TryDestructureNotifierReaderPromiseType(out innerType))
                        {
                            return $"notifierReaderPromise[{StringifyType(innerType)}]";
                        }
                        if (type.TryDestructureSliceIteratorType(out innerType)
                            || type.TryDestructureSliceMutableIteratorType(out innerType))
                        {
                            return $"sliceiterator[{StringifyType(innerType)}]";
                        }
                        if (type == DataTypes.RangeIteratorType)
                        {
                            return "rangeiterator";
                        }
                        if (type == DataTypes.WakerType)
                        {
                            return "waker";
                        }
                        if (type.IsValueClass())
                        {
                            return type.GetTypeDefinitionQualifiedName().ToString("::");
                        }
                        if (type.IsUnion())
                        {
                            return type.IsTypedef()
                                ? type.GetTypeDefinitionQualifiedName().ToString("::")
                                : type.GetName();
                        }
                        throw new NotSupportedException("Unsupported type: " + type);
                    }
            }
        }
    }
}
