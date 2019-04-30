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

        internal VariableReference(VariableSet variableSet, int referenceIndex)
        {
            _variableSet = variableSet;
            ReferenceIndex = referenceIndex;
        }

        public int ReferenceIndex { get; }

        public int Id => _variableSet?.GetId(this) ?? 0;

        public bool Mutable => _variableSet?.GetMutable(this) ?? false;

        public NIType Type => _variableSet?.GetType(this) ?? NIType.Unset;

        internal Lifetime Lifetime => _variableSet?.GetLifetime(this);

        internal void SetTypeAndLifetime(NIType type, Lifetime lifetime) => _variableSet?.SetTypeAndLifetime(this, type, lifetime);

        public void MergeInto(VariableReference intoVariable)
        {
            if (intoVariable._variableSet != _variableSet)
            {
                throw new ArgumentException("Attempting to merge into a variable in a different set.");
            }
            _variableSet.MergeVariables(this, intoVariable);
        }

        private string DebuggerDisplay => _variableSet?.GetDebuggerDisplay(this);
    }
}
