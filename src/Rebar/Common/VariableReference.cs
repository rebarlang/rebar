using System;
using System.Collections.Generic;
using System.Diagnostics;
using NationalInstruments.DataTypes;

namespace Rebar.Common
{
    [DebuggerDisplay("{DebuggerDisplay}")]
    public struct VariableReference
    {
        private readonly VariableSet _variableSet;

        private class SameReferencedVariableEqualityComparer : IEqualityComparer<VariableReference>
        {
            public SameReferencedVariableEqualityComparer()
            {
            }

            public bool Equals(VariableReference x, VariableReference y)
            {
                return x._variableSet == y._variableSet
                    && x._variableSet.ReferenceSameVariable(x, y);
            }

            public int GetHashCode(VariableReference obj)
            {
                return obj._variableSet.GetReferenceHashCode(obj);
            }
        }

        public static Dictionary<VariableReference, TValue> CreateDictionaryWithUniqueVariableKeys<TValue>()
        {
            return new Dictionary<VariableReference, TValue>(new SameReferencedVariableEqualityComparer());
        }

        public static HashSet<VariableReference> CreateUniqueVariableHashSet()
        {
            return new HashSet<VariableReference>(new SameReferencedVariableEqualityComparer());
        }

        internal VariableReference(VariableSet variableSet, int referenceIndex)
        {
            _variableSet = variableSet;
            ReferenceIndex = referenceIndex;
        }

        public bool IsValid => _variableSet != null;

        public int ReferenceIndex { get; }

        public int Id => _variableSet?.GetId(this) ?? 0;

        public string Name
        {
            get
            {
                return _variableSet?.GetName(this) ?? null;
            }
            set
            {
                _variableSet?.SetName(this, value);
            }
        }

        public int DiagramId => _variableSet?.GetDiagramId(this) ?? 0;

        public bool Mutable => _variableSet?.GetMutable(this) ?? false;

        public NIType Type => TypeVariableReference.RenderNIType();

        internal Lifetime Lifetime => TypeVariableReference.Lifetime;

        public void MergeInto(VariableReference intoVariable)
        {
            if (_variableSet == null)
            {
                throw new InvalidOperationException("This variable is invalid.");
            }
            if (intoVariable._variableSet == null)
            {
                throw new ArgumentException("Attempting to merge into an invalid variable.");
            }
            if (intoVariable._variableSet != _variableSet)
            {
                throw new ArgumentException("Attempting to merge into a variable in a different set.");
            }
            if (intoVariable.DiagramId != DiagramId)
            {
                throw new ArgumentException("Attempting to merge into a variable from a different diagram.");
            }
            _variableSet.MergeVariables(this, intoVariable);
        }

        public bool ReferencesSame(VariableReference other)
        {
            return _variableSet.ReferenceSameVariable(this, other);
        }

        internal TypeVariableReference TypeVariableReference => _variableSet.GetTypeVariableReference(this);

        private string DebuggerDisplay => _variableSet?.GetDebuggerDisplay(this);
    }
}
