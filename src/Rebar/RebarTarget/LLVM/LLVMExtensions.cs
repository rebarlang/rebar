using System;
using System.Runtime.InteropServices;
using LLVMSharp;
using NationalInstruments.DataTypes;
using Rebar.Common;

namespace Rebar.RebarTarget.LLVM
{
    internal static class LLVMExtensions
    {
        private static System.Reflection.FieldInfo _moduleInstanceFieldInfo;

        static LLVMExtensions()
        {
            _moduleInstanceFieldInfo = typeof(Module).GetField(
               "instance",
               System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        }

        public static void ThrowIfNull(this LLVMValueRef valueReference)
        {
            if (valueReference.Pointer == IntPtr.Zero)
            {
                throw new NullReferenceException();
            }
        }

        public static LLVMValueRef AsLLVMValue(this bool booleanValue)
        {
            return LLVMSharp.LLVM.ConstInt(LLVMTypeRef.Int1Type(), (booleanValue ? 1u : 0u), false);
        }

        public static LLVMValueRef AsLLVMValue(this sbyte intValue)
        {
            return LLVMSharp.LLVM.ConstInt(LLVMTypeRef.Int8Type(), (ulong)intValue, true);
        }

        public static LLVMValueRef AsLLVMValue(this byte intValue)
        {
            return LLVMSharp.LLVM.ConstInt(LLVMTypeRef.Int8Type(), intValue, false);
        }

        public static LLVMValueRef AsLLVMValue(this short intValue)
        {
            return LLVMSharp.LLVM.ConstInt(LLVMTypeRef.Int16Type(), (ulong)intValue, true);
        }

        public static LLVMValueRef AsLLVMValue(this ushort intValue)
        {
            return LLVMSharp.LLVM.ConstInt(LLVMTypeRef.Int16Type(), intValue, false);
        }

        public static LLVMValueRef AsLLVMValue(this int intValue)
        {
            return LLVMSharp.LLVM.ConstInt(LLVMTypeRef.Int32Type(), (ulong)intValue, true);
        }

        public static LLVMValueRef AsLLVMValue(this uint intValue)
        {
            return LLVMSharp.LLVM.ConstInt(LLVMTypeRef.Int32Type(), intValue, false);
        }

        public static LLVMValueRef AsLLVMValue(this long intValue)
        {
            return LLVMSharp.LLVM.ConstInt(LLVMTypeRef.Int64Type(), (ulong)intValue, true);
        }

        public static LLVMValueRef AsLLVMValue(this ulong intValue)
        {
            return LLVMSharp.LLVM.ConstInt(LLVMTypeRef.Int64Type(), intValue, false);
        }

        public static LLVMTypeRef VoidPointerType { get; } = LLVMTypeRef.PointerType(LLVMTypeRef.Int8Type(), 0u);

        public static LLVMValueRef NullVoidPointer { get; } = LLVMSharp.LLVM.ConstPointerNull(VoidPointerType);

        public static LLVMTypeRef BytePointerType { get; } = LLVMTypeRef.PointerType(LLVMTypeRef.Int8Type(), 0u);

        public static LLVMTypeRef StringSliceReferenceType { get; } = LLVMTypeRef.StructType(
            new LLVMTypeRef[]
            {
                BytePointerType,
                LLVMTypeRef.Int32Type()
            },
            false);

        public static LLVMValueRef BuildStringSliceReferenceValue(this IRBuilder builder, LLVMValueRef stringPtr, LLVMValueRef length)
        {
            return builder.BuildSliceReferenceValue(StringSliceReferenceType, stringPtr, length);
        }

        public static LLVMValueRef BuildSliceReferenceValue(this IRBuilder builder, LLVMTypeRef sliceReferenceType, LLVMValueRef bufferPtr, LLVMValueRef length)
        {
            LLVMValueRef slice0 = builder.CreateInsertValue(LLVMSharp.LLVM.GetUndef(sliceReferenceType), bufferPtr, 0u, "slice0"),
                slice = builder.CreateInsertValue(slice0, length, 1u, "slice");
            return slice;
        }

        public static LLVMValueRef BuildOptionValue(this IRBuilder builder, LLVMTypeRef optionType, LLVMValueRef? someValue)
        {
            LLVMTypeRef innerType = optionType.GetSubtypes()[1];
            LLVMValueRef undef = LLVMSharp.LLVM.GetUndef(optionType),
                option0 = builder.CreateInsertValue(undef, (someValue != null).AsLLVMValue(), 0u, "option0"), 
                option = builder.CreateInsertValue(option0, someValue ?? LLVMSharp.LLVM.ConstNull(innerType), 1u, "option");
            return option;
        }

        public static LLVMTypeRef StringType { get; } = LLVMTypeRef.StructType(
            new LLVMTypeRef[]
            {
                BytePointerType,
                LLVMTypeRef.Int32Type()
            },
            false);

        public static LLVMTypeRef RangeIteratorType { get; } = LLVMTypeRef.StructType(
            new LLVMTypeRef[]
            {
                LLVMTypeRef.Int32Type(),
                LLVMTypeRef.Int32Type()
            },
            false);

        public static LLVMTypeRef FileHandleType { get; } = LLVMTypeRef.StructType(
            new LLVMTypeRef[]
            {
                VoidPointerType
            },
            false);

        public static LLVMTypeRef FakeDropType { get; } = LLVMTypeRef.StructType(
            new LLVMTypeRef[]
            {
                LLVMTypeRef.Int32Type()
            },
            false);

        public static LLVMTypeRef AsLLVMType(this NIType niType)
        {
            switch (niType.GetKind())
            {
                case NITypeKind.UInt8:
                case NITypeKind.Int8:
                    return LLVMTypeRef.Int8Type();
                case NITypeKind.UInt16:
                case NITypeKind.Int16:
                    return LLVMTypeRef.Int16Type();
                case NITypeKind.UInt32:
                case NITypeKind.Int32:
                    return LLVMTypeRef.Int32Type();
                case NITypeKind.UInt64:
                case NITypeKind.Int64:
                    return LLVMTypeRef.Int64Type();
                case NITypeKind.Boolean:
                    return LLVMTypeRef.Int1Type();
                case NITypeKind.String:
                    return StringType;
                default:
                {
                    if (niType.IsRebarReferenceType())
                    {
                        NIType referentType = niType.GetReferentType();
                        if (referentType == DataTypes.StringSliceType)
                        {
                            return StringSliceReferenceType;
                        }
                        NIType sliceElementType;
                        if (referentType.TryDestructureSliceType(out sliceElementType))
                        {
                            return CreateLLVMSliceReferenceType(sliceElementType.AsLLVMType());
                        }
                        return LLVMTypeRef.PointerType(referentType.AsLLVMType(), 0u);
                    }
                    if (niType == DataTypes.FileHandleType)
                    {
                        return FileHandleType;
                    }
                    if (niType == DataTypes.FakeDropType)
                    {
                        return FakeDropType;
                    }
                    NIType innerType;
                    if (niType.TryDestructureOptionType(out innerType))
                    {
                        return CreateLLVMOptionType(innerType.AsLLVMType());
                    }
                    if (niType.TryDestructureVectorType(out innerType))
                    {
                        return CreateLLVMVectorType(innerType.AsLLVMType());
                    }
                    if (niType.TryDestructureSharedType(out innerType))
                    {
                        return CreateLLVMSharedType(innerType.AsLLVMType());
                    }
                    if (niType == DataTypes.RangeIteratorType)
                    {
                        return RangeIteratorType;
                    }
                    throw new NotSupportedException("Unsupported type: " + niType);
                }
            }
        }

        internal static LLVMTypeRef CreateLLVMOptionType(this LLVMTypeRef innerType)
        {
            return LLVMTypeRef.StructType(new LLVMTypeRef[] { LLVMTypeRef.Int1Type(), innerType }, false);
        }

        internal static LLVMTypeRef CreateLLVMVectorType(this LLVMTypeRef elementType)
        {
            return LLVMTypeRef.StructType(
                // { allocationPtr, size, capacity }
                new LLVMTypeRef[]
                {
                    LLVMTypeRef.PointerType(elementType, 0u),
                    LLVMTypeRef.Int32Type(),
                    LLVMTypeRef.Int32Type()
                },
                false);
        }

        internal static LLVMTypeRef CreateLLVMSliceReferenceType(this LLVMTypeRef innerType)
        {
            return LLVMTypeRef.StructType(new LLVMTypeRef[] { LLVMTypeRef.PointerType(innerType, 0u), LLVMTypeRef.Int32Type() }, false);
        }

        internal static LLVMTypeRef CreateLLVMSharedType(this LLVMTypeRef innerType)
        {
            return LLVMTypeRef.PointerType(innerType.CreateLLVMRefCountType(), 0u);
        }

        internal static LLVMTypeRef CreateLLVMRefCountType(this LLVMTypeRef innerType)
        {
            return LLVMTypeRef.StructType(new LLVMTypeRef[] { LLVMTypeRef.Int32Type(), innerType }, false);
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

        internal static Module DeserializeModuleAsBitcode(this byte[] moduleBytes)
        {
            LLVMMemoryBufferRef bufferRef = CreateMemoryBufferRefFromBytes(moduleBytes);
            LLVMModuleRef moduleRef;
            try
            {
                if (LLVMSharp.LLVM.ParseBitcode2(bufferRef, out moduleRef) != false)
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

        #endregion

        internal static LLVMBuilderRef GetBuilderRef(this IRBuilder builder)
        {
            System.Reflection.FieldInfo field = typeof(IRBuilder).GetField(
                "instance",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (LLVMBuilderRef)field.GetValue(builder);
        }
    }
}
