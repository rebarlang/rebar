using System;
using System.Collections.Generic;
using System.Linq;

namespace Rebar.Common
{
    internal sealed class VariableSet
    {
        private sealed class Variable
        {
            /// <summary>
            /// </summary>
            /// <remarks>TODO: get rid of this. The only thing that ultimately consumes it is the FunctionWireViewModel; for that,
            /// it would be better to have a more stable, source model-related notion of the origin of a variable.
            /// 
            /// For debugging purposes, come up with something else.</remarks>
            public int Id { get; }

            public int FirstReferenceIndex { get; }

            /// <summary>
            /// True if the <see cref="Variable"/> represents a mutable binding.
            /// </summary>
            /// <remarks>This property is independent of whether the <see cref="Variable"/>'s type
            /// is a mutable reference; it is possible to have a mutable ImmutableReference <see cref="Variable"/>
            /// (which can be rebound to a different ImmutableReference) and an immutable MutableReference
            /// <see cref="Variable"/> (where the referred-to storage can be modified, but the <see cref="Variable"/>
            /// cannot be rebound).</remarks>
            public bool Mutable { get; }

            public TypeVariableReference TypeVariableReference { get; }

            public Lifetime Lifetime { get; set; }

            public Variable(int id, int firstReferenceIndex, TypeVariableReference variableType, bool mutable)
            {
                Id = id;
                FirstReferenceIndex = firstReferenceIndex;
                TypeVariableReference = variableType;
                Mutable = mutable;
            }

            public override string ToString()
            {
                string mut = Mutable ? "mut" : string.Empty;
                return $"v_{Id} : {mut} Type";
            }
        }

        private int _currentVariableId = 0;
        private int _currentVariableReferenceId = 1;

        private readonly List<Variable> _variables = new List<Variable>();
        private readonly List<Variable> _variableReferences = new List<Variable>();

        public VariableSet()
            : this(null)
        {
        }

        public VariableSet(TypeVariableSet typeVariableSet)
        {
            TypeVariableSet = typeVariableSet;
        }

        public TypeVariableSet TypeVariableSet { get; }

        private Variable CreateNewVariable(bool mutableVariable, int firstReferenceIndex, TypeVariableReference variableType)
        {
            int variableId = _currentVariableId;
            _currentVariableId++;
            var variable = new Variable(variableId, firstReferenceIndex, variableType, mutableVariable);
            _variables.Add(variable);
            return variable;
        }

        private void SetVariableAtReferenceIndex(Variable variable, int referenceIndex)
        {
            while (_variableReferences.Count <= referenceIndex)
            {
                _variableReferences.Add(null);
            }
            _variableReferences[referenceIndex] = variable;
        }

        private Variable GetVariableForVariableReference(VariableReference variableReference)
        {
            return _variableReferences[variableReference.ReferenceIndex];
        }

        private VariableReference GetExistingReferenceForVariable(Variable variable)
        {
            return new VariableReference(this, variable.FirstReferenceIndex);
        }

        public VariableReference CreateNewVariable(TypeVariableReference variableType, bool mutable = false)
        {
            int id = _currentVariableReferenceId++;
            Variable variable = CreateNewVariable(mutable, id, variableType);
            SetVariableAtReferenceIndex(variable, id);
            return new VariableReference(this, id);
        }

        public VariableReference CreateNewVariableForUnwiredTerminal()
        {
            return CreateNewVariable(TypeVariableSet.CreateReferenceToNewTypeVariable());
        }

        public IEnumerable<VariableReference> GetUniqueVariableReferences()
        {
            return _variables.Select(GetExistingReferenceForVariable);
        }

        public void MergeVariables(VariableReference toMerge, VariableReference mergeWith)
        {
            Variable mergeWithVariable = GetVariableForVariableReference(mergeWith),
                toMergeVariable = GetVariableForVariableReference(toMerge);

            for (int i = 0; i < _variableReferences.Count; ++i)
            {
                if (_variableReferences[i] == toMergeVariable)
                {
                    _variableReferences[i] = mergeWithVariable;
                }
            }
            _variables.Remove(toMergeVariable);
        }

        internal bool GetMutable(VariableReference variableReference) => GetVariableForVariableReference(variableReference).Mutable;

        internal int GetId(VariableReference variableReference) => GetVariableForVariableReference(variableReference).Id;

        internal string GetDebuggerDisplay(VariableReference variableReference)
        {
            Variable variable = GetVariableForVariableReference(variableReference);
            return variable.ToString();
        }

        internal TypeVariableReference GetTypeVariableReference(VariableReference variableReference)
        {
            Variable variable = GetVariableForVariableReference(variableReference);
            TypeVariableReference typeVariableReference = variable.TypeVariableReference;
            if (typeVariableReference.TypeVariableSet == null)
            {
                throw new ArgumentException("Getting TypeVariableReference for a variable that hasn't set one.");
            }
            return typeVariableReference;
        }

        internal bool ReferenceSameVariable(VariableReference x, VariableReference y) => GetVariableForVariableReference(x) == GetVariableForVariableReference(y);

        internal int GetReferenceHashCode(VariableReference x) => GetVariableForVariableReference(x).GetHashCode();
    }
}
