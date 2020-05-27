using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using LLVMSharp;
using NationalInstruments.DataTypes;
using Rebar.Common;

namespace Rebar.RebarTarget.LLVM
{
    internal static class LLVMExtensions
    {
        private static System.Reflection.FieldInfo _moduleInstanceFieldInfo;
        private static readonly LLVMMCJITCompilerOptions _mcJITCompilerOptions;

        private static readonly Dictionary<LLVMContextRef, Dictionary<string, LLVMTypeRef>> _namedStructTypes = new Dictionary<LLVMContextRef, Dictionary<string, LLVMTypeRef>>();

        static LLVMExtensions()
        {
            _moduleInstanceFieldInfo = typeof(Module).GetField(
               "instance",
               System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            LLVMSharp.LLVM.LinkInMCJIT();

            LLVMSharp.LLVM.InitializeX86TargetMC();
            LLVMSharp.LLVM.InitializeX86Target();
            LLVMSharp.LLVM.InitializeX86TargetInfo();
            LLVMSharp.LLVM.InitializeX86AsmParser();
            LLVMSharp.LLVM.InitializeX86AsmPrinter();

            _mcJITCompilerOptions = new LLVMMCJITCompilerOptions
            {
                NoFramePointerElim = 1,
                // TODO: comment about why this is necessary
                CodeModel = LLVMCodeModel.LLVMCodeModelLarge,
            };
            LLVMSharp.LLVM.InitializeMCJITCompilerOptions(_mcJITCompilerOptions);
        }

        public static bool IsUninitialized(this LLVMValueRef valueReference)
        {
            return valueReference.Pointer == IntPtr.Zero;
        }

        public static void ThrowIfNull(this LLVMValueRef valueReference)
        {
            if (valueReference.IsUninitialized())
            {
                throw new NullReferenceException();
            }
        }

        public static LLVMTypeRef VoidPointerType(this ContextWrapper context) => LLVMTypeRef.PointerType(context.Int8Type, 0u);

        public static LLVMValueRef NullVoidPointer(this ContextWrapper context) => LLVMSharp.LLVM.ConstPointerNull(context.VoidPointerType());

        public static LLVMTypeRef BytePointerType(this ContextWrapper context) => LLVMTypeRef.PointerType(context.Int8Type, 0u);

        public static LLVMTypeRef StringSliceReferenceType(this ContextWrapper context) => context.StructType(
            new LLVMTypeRef[]
            {
                context.BytePointerType(),
                context.Int32Type
            },
            false);

        public static LLVMTypeRef ScheduledTaskFunctionType(this ContextWrapper context) => LLVMTypeRef.FunctionType(context.VoidType, new[] { context.VoidPointerType() }, false);

        public static LLVMTypeRef GetScheduledTaskType(this ContextWrapper context)
        {
            return context.GetCachedStructType("scheduled_task", () => new[]
                {
                    // task function pointer
                    LLVMTypeRef.PointerType(context.ScheduledTaskFunctionType(), 0u),
                    // task state
                    context.VoidPointerType()
                });
        }

        public static LLVMTypeRef WakerType(this ContextWrapper context) => context.StructType(
            new LLVMTypeRef[]
            {
                // task function pointer
                LLVMTypeRef.PointerType(context.ScheduledTaskFunctionType(), 0u),
                // task state
                context.VoidPointerType()
            });

        public static LLVMValueRef BuildStringSliceReferenceValue(this ContextWrapper context, IRBuilder builder, LLVMValueRef stringPtr, LLVMValueRef length)
        {
            return builder.BuildSliceReferenceValue(context.StringSliceReferenceType(), stringPtr, length);
        }

        public static LLVMValueRef BuildSliceReferenceValue(this IRBuilder builder, LLVMTypeRef sliceReferenceType, LLVMValueRef bufferPtr, LLVMValueRef length)
        {
            return builder.BuildStructValue(
                sliceReferenceType,
                new LLVMValueRef[] { bufferPtr, length },
                "slice");
        }

        public static LLVMValueRef BuildOptionValue(this ContextWrapper context, IRBuilder builder, LLVMTypeRef optionType, LLVMValueRef? someValue)
        {
            LLVMTypeRef innerType = optionType.GetSubtypes()[1];
            return someValue == null
                ? LLVMSharp.LLVM.ConstNull(optionType)
                : builder.BuildStructValue(
                    optionType,
                    new LLVMValueRef[] { context.AsLLVMValue(true), someValue.Value },
                    "option");
        }

        public static LLVMValueRef BuildStructValue(this IRBuilder builder, LLVMTypeRef structType, LLVMValueRef[] fieldValues, string valueName = null)
        {
            LLVMValueRef currentValue = LLVMSharp.LLVM.GetUndef(structType);
            valueName = valueName ?? "agg";
            for (uint i = 0; i < fieldValues.Length; ++i)
            {
                currentValue = builder.CreateInsertValue(
                    currentValue,
                    fieldValues[i],
                    i,
                    (i == fieldValues.Length - 1) ? valueName : "agg");
            }
            return currentValue;
        }

        public static LLVMTypeRef StringType(this ContextWrapper context) => context.StructType(
            new LLVMTypeRef[]
            {
                context.BytePointerType(),
                context.Int32Type
            });

        public static LLVMTypeRef StringSplitIteratorType(this ContextWrapper context) => context.StructType(
            new LLVMTypeRef[]
            {
                context.StringSliceReferenceType(),
                context.BytePointerType()
            });

        public static LLVMTypeRef RangeIteratorType(this ContextWrapper context) => context.StructType(
            new LLVMTypeRef[]
            {
                context.Int32Type,
                context.Int32Type
            });

        public static LLVMTypeRef FileHandleType(this ContextWrapper context) => context.StructType(
            new LLVMTypeRef[]
            {
                context.VoidPointerType()
            });

        public static LLVMTypeRef FakeDropType(this ContextWrapper context) => context.StructType(
            new LLVMTypeRef[]
            {
                context.Int32Type
            });

        // TechDebt: for some of the types below, it would be nicer to return named types, which we can do now that we're
        // passing around an LLVM context.
        public static LLVMTypeRef AsLLVMType(this ContextWrapper context, NIType niType)
        {
            switch (niType.GetKind())
            {
                case NITypeKind.UInt8:
                case NITypeKind.Int8:
                    return context.Int8Type;
                case NITypeKind.UInt16:
                case NITypeKind.Int16:
                    return context.Int16Type;
                case NITypeKind.UInt32:
                case NITypeKind.Int32:
                    return context.Int32Type;
                case NITypeKind.UInt64:
                case NITypeKind.Int64:
                    return context.Int64Type;
                case NITypeKind.Boolean:
                    return context.Int1Type;
                case NITypeKind.String:
                    return context.StringType();
                default:
                {
                    if (niType.IsRebarReferenceType())
                    {
                        NIType referentType = niType.GetReferentType();
                        if (referentType == DataTypes.StringSliceType)
                        {
                            return context.StringSliceReferenceType();
                        }
                        NIType sliceElementType;
                        if (referentType.TryDestructureSliceType(out sliceElementType))
                        {
                            return context.CreateLLVMSliceReferenceType(context.AsLLVMType(sliceElementType));
                        }
                        return LLVMTypeRef.PointerType(context.AsLLVMType(referentType), 0u);
                    }
                    if (niType.IsCluster())
                    {
                        LLVMTypeRef[] fieldTypes = niType.GetFields().Select(field => context.AsLLVMType(field.GetDataType())).ToArray();
                        return context.StructType(fieldTypes);
                    }
                    if (niType == DataTypes.FileHandleType)
                    {
                        return context.FileHandleType();
                    }
                    if (niType == DataTypes.FakeDropType)
                    {
                        return context.FakeDropType();
                    }
                    if (niType == DataTypes.RangeIteratorType)
                    {
                        return context.RangeIteratorType();
                    }
                    if (niType == DataTypes.WakerType)
                    {
                        return context.WakerType();
                    }
                    NIType innerType;
                    if (niType.TryDestructureOptionType(out innerType))
                    {
                        return context.CreateLLVMOptionType(context.AsLLVMType(innerType));
                    }
                    if (niType.IsStringSplitIteratorType())
                    {
                        return context.StringSplitIteratorType();
                    }
                    if (niType.TryDestructureVectorType(out innerType))
                    {
                        return context.CreateLLVMVectorType(context.AsLLVMType(innerType));
                    }
                    if (niType.TryDestructureSharedType(out innerType))
                    {
                        return context.CreateLLVMSharedType(context.AsLLVMType(innerType));
                    }
                    if (niType.TryDestructureYieldPromiseType(out innerType))
                    {
                        return context.CreateLLVMYieldPromiseType(context.AsLLVMType(innerType));
                    }
                    if (niType.TryDestructureMethodCallPromiseType(out innerType))
                    {
                        return context.CreateLLVMMethodCallPromiseType(context.AsLLVMType(innerType));
                    }
                    if (niType.TryDestructureNotifierReaderType(out innerType))
                    {
                        return context.CreateLLVMNotifierReaderType(context.AsLLVMType(innerType));
                    }
                    if (niType.TryDestructureNotifierReaderPromiseType(out innerType))
                    {
                        return context.CreateLLVMNotifierReaderPromiseType(context.AsLLVMType(innerType));
                    }
                    if (niType.TryDestructureNotifierWriterType(out innerType))
                    {
                        return context.CreateLLVMNotifierWriterType(context.AsLLVMType(innerType));
                    }
                    if (niType.TryDestructurePanicResultType(out innerType))
                    {
                        return context.CreateLLVMPanicResultType(context.AsLLVMType(innerType));
                    }
                    // TODO: when using typedef classes and unions in FunctionCompiler, the LLVM type should
                    // come from a TypeDiagramBuiltPackage.
                    if (niType.IsValueClass() && niType.GetFields().Any())
                    {
                        LLVMTypeRef[] fieldTypes = niType.GetFields().Select(f => context.AsLLVMType(f.GetDataType())).ToArray();
                        return context.StructType(fieldTypes);
                    }
                    if (niType.IsUnion())
                    {
                        int maxSize = 4;
                        foreach (NIType field in niType.GetFields())
                        {
                            // TODO: where possible, use a non-array type that matches the max size
                            LLVMTypeRef llvmFieldType = context.AsLLVMType(field.GetDataType());
                            int fieldSize = (int)LLVMSharp.LLVM.StoreSizeOfType(LocalTargetInfo.TargetData, llvmFieldType);
                            maxSize = Math.Max(maxSize, fieldSize);
                        }
                        LLVMTypeRef[] structFieldTypes = new LLVMTypeRef[]
                        {
                            context.Int8Type,
                            // TODO: this is incorrect because it does not consider alignment
                            LLVMTypeRef.ArrayType(context.Int8Type, (uint)maxSize)
                        };
                        return context.StructType(structFieldTypes);
                    }
                    throw new NotSupportedException("Unsupported type: " + niType);
                }
            }
        }

        internal static LLVMTypeRef CreateLLVMOptionType(this ContextWrapper context, LLVMTypeRef innerType)
        {
            return context.StructType(new LLVMTypeRef[] { context.Int1Type, innerType });
        }

        internal static LLVMTypeRef CreateLLVMPanicResultType(this ContextWrapper context, LLVMTypeRef innerType)
        {
            return context.StructType(new LLVMTypeRef[] { context.Int1Type, innerType });
        }

        internal static LLVMTypeRef CreateLLVMVectorType(this ContextWrapper context, LLVMTypeRef elementType)
        {
            return context.StructType(
                // { allocationPtr, size, capacity }
                new LLVMTypeRef[]
                {
                    LLVMTypeRef.PointerType(elementType, 0u),
                    context.Int32Type,
                    context.Int32Type
                },
                false);
        }

        internal static LLVMTypeRef CreateLLVMSliceReferenceType(this ContextWrapper context, LLVMTypeRef innerType)
        {
            return context.StructType(new LLVMTypeRef[] { LLVMTypeRef.PointerType(innerType, 0u), context.Int32Type });
        }

        internal static LLVMTypeRef CreateLLVMSharedType(this ContextWrapper context, LLVMTypeRef innerType)
        {
            return LLVMTypeRef.PointerType(context.CreateLLVMRefCountType(innerType), 0u);
        }

        internal static LLVMTypeRef CreateLLVMRefCountType(this ContextWrapper context, LLVMTypeRef innerType)
        {
            return context.StructType(new LLVMTypeRef[] { context.Int32Type, innerType });
        }

        internal static LLVMTypeRef CreateLLVMYieldPromiseType(this ContextWrapper context, LLVMTypeRef innerType) => context.StructType(new LLVMTypeRef[] { innerType });

        private static LLVMTypeRef MethodPollFunctionType(this ContextWrapper context) => LLVMTypeRef.FunctionType(
            context.VoidType,
            new LLVMTypeRef[]
            {
                context.VoidPointerType(),
                LLVMTypeRef.PointerType(context.ScheduledTaskFunctionType(), 0u),
                context.VoidPointerType()
            },
            false);

        internal static LLVMTypeRef CreateLLVMMethodCallPromiseType(this ContextWrapper context, LLVMTypeRef innerType)
        {
            return context.StructType(new LLVMTypeRef[]
            {
                LLVMTypeRef.PointerType(context.MethodPollFunctionType(), 0u),
                context.VoidPointerType(),
                innerType,
            });
        }

        internal static LLVMTypeRef CreateLLVMNotifierSharedDataType(this ContextWrapper context, LLVMTypeRef innerType)
        {
            return context.StructType(
                new LLVMTypeRef[]
                {
                    context.WakerType(),
                    innerType,
                    context.Int32Type,
                });
        }

        internal static LLVMTypeRef CreateLLVMNotifierReaderType(this ContextWrapper context, LLVMTypeRef innerType)
        {
            return context.StructType(
                new LLVMTypeRef[]
                {
                    LLVMTypeRef.PointerType(context.CreateLLVMRefCountType(context.CreateLLVMNotifierSharedDataType(innerType)), 0u)
                },
                false);
        }

        internal static LLVMTypeRef CreateLLVMNotifierReaderPromiseType(this ContextWrapper context, LLVMTypeRef innerType)
        {
            return context.StructType(
                new LLVMTypeRef[]
                {
                    LLVMTypeRef.PointerType(context.CreateLLVMRefCountType(context.CreateLLVMNotifierSharedDataType(innerType)), 0u)
                },
                false);
        }

        internal static LLVMTypeRef CreateLLVMNotifierWriterType(this ContextWrapper context, LLVMTypeRef innerType)
        {
            return context.StructType(
                new LLVMTypeRef[]
                {
                    LLVMTypeRef.PointerType(context.CreateLLVMRefCountType(context.CreateLLVMNotifierSharedDataType(innerType)), 0u)
                },
                false);
        }

        internal static LLVMTypeRef TranslateFunctionType(this ContextWrapper context, NIType functionType)
        {
            LLVMTypeRef[] parameterTypes = functionType.GetParameters().Select(context.TranslateParameterType).ToArray();
            return LLVMSharp.LLVM.FunctionType(context.VoidType, parameterTypes, false);
        }

        internal static LLVMTypeRef TranslateParameterType(this ContextWrapper context, NIType parameterType)
        {
            // TODO: this should probably share code with how we compute the top function LLVM type above
            bool isInput = parameterType.GetInputParameterPassingRule() != NIParameterPassingRule.NotAllowed,
                isOutput = parameterType.GetOutputParameterPassingRule() != NIParameterPassingRule.NotAllowed;
            LLVMTypeRef parameterLLVMType = context.AsLLVMType(parameterType.GetDataType());
            if (isInput)   // includes inout parameters
            {
                if (isOutput && !parameterType.GetDataType().IsRebarReferenceType())
                {
                    throw new InvalidOperationException("Inout parameter with non-reference type");
                }
                return parameterLLVMType;
            }
            if (isOutput)
            {
                return LLVMTypeRef.PointerType(parameterLLVMType, 0u);
            }
            throw new NotImplementedException("Parameter direction is wrong");
        }

        #region Memory Buffer

        private static byte[] CopyBytesFromMemoryBufferRef(LLVMMemoryBufferRef bufferRef)
        {
            IntPtr start = LLVMSharp.LLVM.GetBufferStart(bufferRef);
            var size = LLVMSharp.LLVM.GetBufferSize(bufferRef);
            byte[] copy = new byte[size];
            Marshal.Copy(start, copy, 0, size);
            return copy;
        }

        private static LLVMMemoryBufferRef CreateMemoryBufferRefFromBytes(byte[] bytes)
        {
            LLVMMemoryBufferRef bufferRef;
            unsafe
            {
                fixed (byte* ptr = &bytes[0])
                {
                    bufferRef = LLVMSharp.LLVM.CreateMemoryBufferWithMemoryRangeCopy((IntPtr)ptr, bytes.Length, "buffer");
                }
            }
            return bufferRef;
        }

        #endregion

        #region Module

        internal static byte[] SerializeModuleAsBitcode(this Module module)
        {
            LLVMMemoryBufferRef bufferRef = module.WriteBitcodeToMemoryBuffer();
            byte[] serialized = CopyBytesFromMemoryBufferRef(bufferRef);
            LLVMSharp.LLVM.DisposeMemoryBuffer(bufferRef);
            return serialized;
        }

        internal static Module DeserializeModuleAsBitcode(this byte[] moduleBytes, LLVMContextRef contextRef)
        {
            LLVMMemoryBufferRef bufferRef = CreateMemoryBufferRefFromBytes(moduleBytes);
            LLVMModuleRef moduleRef;
            try
            {
                if (LLVMSharp.LLVM.ParseBitcodeInContext2(contextRef, bufferRef, out moduleRef) != false)
                {
                    throw new ArgumentException("Failed to load bitcode module", nameof(moduleBytes));
                }
                return moduleRef.ModuleFromModuleRef();
            }
            finally
            {
                LLVMSharp.LLVM.DisposeMemoryBuffer(bufferRef);
            }
        }

        internal static LLVMModuleRef GetModuleRef(this Module module)
        {
            return (LLVMModuleRef)_moduleInstanceFieldInfo.GetValue(module);
        }

        private static Module ModuleFromModuleRef(this LLVMModuleRef moduleRef)
        {
            Module module = (Module)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Module));            
            _moduleInstanceFieldInfo.SetValue(module, moduleRef);
            return module;
        }

        internal static void VerifyAndThrowIfInvalid(this Module module)
        {
            string error;
            if (LLVMSharp.LLVM.VerifyModule(
                module.GetModuleRef(),
                LLVMVerifierFailureAction.LLVMReturnStatusAction,
                out error) != false)
            {
                throw new InvalidOperationException($"Invalid module: {error}");
            }
        }

        internal static void LinkInModule(this Module linkInto, Module toLinkIn)
        {
            LLVMSharp.LLVM.LinkModules2(linkInto.GetModuleRef(), toLinkIn.GetModuleRef());
        }

        internal static LLVMExecutionEngineRef CreateMCJITCompilerForModule(this Module module)
        {
            LLVMExecutionEngineRef engine;
            string error;
            LLVMBool Success = new LLVMBool(0);
            if (LLVMSharp.LLVM.CreateMCJITCompilerForModule(
                out engine,
                module.GetModuleRef(),
                _mcJITCompilerOptions,
                out error) != Success)
            {
                throw new InvalidOperationException($"Error creating JIT: {error}");
            }
            return engine;
        }

        #endregion

        internal static LLVMBuilderRef GetBuilderRef(this IRBuilder builder)
        {
            System.Reflection.FieldInfo field = typeof(IRBuilder).GetField(
                "instance",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (LLVMBuilderRef)field.GetValue(builder);
        }

        internal static void AddIncoming(this LLVMValueRef phiValue, LLVMValueRef incomingValue, LLVMBasicBlockRef incomingBlock)
        {
            phiValue.AddIncoming(new LLVMValueRef[] { incomingValue }, new LLVMBasicBlockRef[] { incomingBlock }, 1u);
        }
    }
}
