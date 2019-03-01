using System;
using System.Collections.Generic;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler;
using Rebar.RebarTarget.Execution;

namespace Rebar.RebarTarget
{
    /// <summary>
    /// Transform that associates a local slot with each <see cref="Variable"/> in a <see cref="DfirRoot"/>.
    /// </summary>
    /// <remarks>For now, the implementation is the most naive one possible; it assigns every Variable
    /// its own unique local slot. Future implementations can improve on this by:
    /// * Determining when variables from two different sets can reuse local slots
    /// * Using the same frame space for variables of different types
    /// * Determining when semantic variables are actually constants and thus do not need to be
    /// allocated in the frame</remarks>
    internal sealed class Allocator : VisitorTransformBase
    {
        private readonly Dictionary<Variable, Allocation> _variableLocalIndices;
        private int _currentIndex = 0;

        public Allocator(Dictionary<Variable, Allocation> variableLocalIndices)
        {
            _variableLocalIndices = variableLocalIndices;
        }

        protected override void VisitDiagram(Diagram diagram)
        {
            base.VisitDiagram(diagram);
            VariableSet diagramSet = diagram.GetVariableSet();
            foreach (Variable variable in diagramSet.Variables)
            {
                int size = GetTypeSize(variable.Type);
                _variableLocalIndices[variable] = new Allocation(_currentIndex, size);
                ++_currentIndex;
            }
        }

        protected override void VisitBorderNode(BorderNode borderNode)
        {
        }

        protected override void VisitNode(Node node)
        {
        }

        protected override void VisitWire(Wire wire)
        {
        }

        private int GetTypeSize(NIType type)
        {
            if (type.IsRebarReferenceType())
            {
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
            if (type.IsIteratorType())
            {
                // for now, the only possible iterator is RangeIterator<int>
                // { current : i32, range_max : i32 }
                return 8;
            }
            throw new NotImplementedException("Unknown size for type " + type);
        }
    }

    internal struct Allocation
    {
        public Allocation(int index, int size)
        {
            Index = index;
            Size = size;
        }

        public int Index { get; }

        public int Size { get; }
    }
}
