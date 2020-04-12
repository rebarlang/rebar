using System;
using System.Collections.Generic;
using LLVMSharp;
using NationalInstruments.DataTypes;
using static LLVMSharp.LLVM;

namespace Rebar.RebarTarget.LLVM
{
    internal sealed class ContextWrapper : IDisposable
    {
        private readonly LLVMContextRef _context;
        // TODO: unclear if we need this
        private readonly Dictionary<string, LLVMTypeRef> _structTypes = new Dictionary<string, LLVMTypeRef>();

        public static readonly ContextWrapper GlobalContext = new ContextWrapper(GetGlobalContext());

        public ContextWrapper()
            : this(ContextCreate())
        {
        }

        private ContextWrapper(LLVMContextRef contextRef)
        {
            _context = contextRef;
        }

        public Module CreateModule(string moduleName)
        {
            return new Module(moduleName, _context);
        }

        public IRBuilder CreateIRBuilder() => new IRBuilder(_context);

        public Module LoadContextFreeModule(ContextFreeModule contextFreeModule)
        {
            return contextFreeModule.LoadModuleInContext(_context);
        }

        public LLVMTypeRef VoidType => _context.VoidTypeInContext();

        public LLVMTypeRef Int1Type => _context.Int1TypeInContext();

        public LLVMTypeRef Int8Type => _context.Int8TypeInContext();

        public LLVMTypeRef Int16Type => _context.Int16TypeInContext();

        public LLVMTypeRef Int32Type => _context.Int32TypeInContext();

        public LLVMTypeRef Int64Type => _context.Int64TypeInContext();

        public LLVMTypeRef StructType(LLVMTypeRef[] fieldTypes, bool packed = false)
        {
            // TODO: validate that input types are in _context?
            return _context.StructTypeInContext(fieldTypes, packed);
        }

        public LLVMTypeRef NamedStructType(string typeName, LLVMTypeRef[] fields, bool packed = false)
        {
            LLVMTypeRef structType = _context.StructCreateNamed(typeName);
            structType.StructSetBody(fields, packed);
            return structType;
        }

        public LLVMTypeRef GetCachedStructType(string typeName, Func<LLVMTypeRef[]> fields)
        {
            LLVMTypeRef structType;
            if (!_structTypes.TryGetValue(typeName, out structType))
            {
                structType = NamedStructType(typeName, fields(), false);
                _structTypes[typeName] = structType;
            }
            return structType;
        }

        public LLVMValueRef AsLLVMValue(bool booleanValue) => ConstInt(Int1Type, (booleanValue ? 1u : 0u), false);

        public LLVMValueRef AsLLVMValue(sbyte intValue) => ConstInt(Int8Type, (ulong)intValue, true);

        public LLVMValueRef AsLLVMValue(byte intValue) => ConstInt(Int8Type, intValue, false);

        public LLVMValueRef AsLLVMValue(short intValue) => ConstInt(Int16Type, (ulong)intValue, true);

        public LLVMValueRef AsLLVMValue(ushort intValue) => ConstInt(Int16Type, intValue, false);

        public LLVMValueRef AsLLVMValue(int intValue) => ConstInt(Int32Type, (ulong)intValue, true);

        public LLVMValueRef AsLLVMValue(uint intValue) => ConstInt(Int32Type, intValue, false);

        public LLVMValueRef AsLLVMValue(long intValue) => ConstInt(Int64Type, (ulong)intValue, true);

        public LLVMValueRef AsLLVMValue(ulong intValue) => ConstInt(Int64Type, intValue, false);

        public LLVMValueRef GetIntegerValue(object value, NIType type)
        {
            LLVMValueRef constantValueRef;
            switch (type.GetKind())
            {
                case NITypeKind.Int8:
                    constantValueRef = AsLLVMValue((sbyte)value);
                    break;
                case NITypeKind.UInt8:
                    constantValueRef = AsLLVMValue((byte)value);
                    break;
                case NITypeKind.Int16:
                    constantValueRef = AsLLVMValue((short)value);
                    break;
                case NITypeKind.UInt16:
                    constantValueRef = AsLLVMValue((ushort)value);
                    break;
                case NITypeKind.Int32:
                    constantValueRef = AsLLVMValue((int)value);
                    break;
                case NITypeKind.UInt32:
                    constantValueRef = AsLLVMValue((uint)value);
                    break;
                case NITypeKind.Int64:
                    constantValueRef = AsLLVMValue((long)value);
                    break;
                case NITypeKind.UInt64:
                    constantValueRef = AsLLVMValue((ulong)value);
                    break;
                default:
                    throw new NotSupportedException("Unsupported numeric constant type: " + type);
            }
            return constantValueRef;
        }

        public void Dispose()
        {
            if (this != GlobalContext)
            {
                _context.ContextDispose();
            }
            else
            {
                throw new InvalidOperationException("Can't dispose the global context");
            }
        }
    }
}
