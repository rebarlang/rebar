using System;
using System.Collections.Generic;
using NationalInstruments.DataTypes;
using Rebar.Common;

namespace Rebar.RebarTarget.BytecodeInterpreter
{
    internal class BytecodeInterpreterAllocator : Allocator<ValueSource, LocalAllocationValueSource, ConstantLocalReferenceValueSource>
    {
        private int _currentIndex = 0;

        public BytecodeInterpreterAllocator(Dictionary<VariableReference, ValueSource> variableAllocations) : base(variableAllocations)
        {
        }

        internal static int GetTypeSize(NIType type)
        {
            if (type.IsRebarReferenceType())
            {
                if (type.GetReferentType() == DataTypes.StringSliceType)
                {
                    return TargetConstants.PointerSize + 4;
                }
                return TargetConstants.PointerSize;
            }
            NIType innerType;
            if (type.TryDestructureOptionType(out innerType))
            {
                return 4 + GetTypeSize(innerType);
            }
            if (type.IsInt32() || type.IsBoolean())
            {
                return 4;
            }
            if (type.IsString())
            {
                return TargetConstants.PointerSize + 4;
            }
            if (type.IsIteratorType())
            {
                // for now, the only possible iterator is RangeIterator<int>
                // { current : i32, range_max : i32 }
                return 8;
            }
            throw new NotImplementedException("Unknown size for type " + type);
        }

        protected override LocalAllocationValueSource CreateLocalAllocation(VariableReference variable)
        {
            int size = GetTypeSize(variable.Type);
            var localAllocation = new LocalAllocationValueSource(_currentIndex, size);
            ++_currentIndex;
            return localAllocation;
        }

        protected override ConstantLocalReferenceValueSource CreateConstantLocalReference(VariableReference referencedVariable)
        {
            var localAllocation = (LocalAllocationValueSource)GetValueSourceForVariable(referencedVariable);
            return new ConstantLocalReferenceValueSource(localAllocation.Index);
        }
    }

    internal abstract class ValueSource
    {
        public ValueSource()
        {
        }
    }

    internal class LocalAllocationValueSource : ValueSource
    {
        public LocalAllocationValueSource(int index, int size)
        {
            Index = index;
            Size = size;
        }

        public int Index { get; }

        public int Size { get; }
    }

    internal class ConstantLocalReferenceValueSource : ValueSource
    {
        public ConstantLocalReferenceValueSource(int referencedIndex)
            : base()
        {
            ReferencedIndex = referencedIndex;
        }

        public int ReferencedIndex { get; }
    }
}
