using System;
using System.Collections.Generic;
using System.Linq;
using NationalInstruments.DataTypes;

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

            /// <summary>
            /// The data <see cref="NIType"/> stored by the <see cref="Variable"/>.
            /// </summary>
            /// <remarks>This property should not store ImmutableValue or MutableValue types.
            /// ImmutableReference and MutableReference types are allowed.</remarks>
            public NIType Type { get; set; }

            public Lifetime Lifetime { get; set; }

            public Variable(int id, int firstReferenceIndex, bool mutable)
            {
                Type = PFTypes.Void;
                Id = id;
                FirstReferenceIndex = firstReferenceIndex;
                Mutable = mutable;
            }

            public override string ToString()
            {
                string mut = Mutable ? "mut" : string.Empty;
                return $"v_{Id} : {mut} {Type}";
            }
        }

        private int _currentVariableId = 1;

        private readonly List<Variable> _variables = new List<Variable>();
        private readonly List<Variable> _variableReferences = new List<Variable>();
        private readonly Dictionary<Lifetime, List<Variable>> _variablesInterruptedByLifetimes = new Dictionary<Lifetime, List<Variable>>();
        private readonly BoundedLifetimeGraph _boundedLifetimeGraph = new BoundedLifetimeGraph();

        private Variable CreateNewVariable(bool mutableVariable, int firstReferenceIndex)
        {
            var variable = new Variable(_variables.Count, firstReferenceIndex, mutableVariable);
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

        public VariableReference CreateNewVariable(bool mutable = false)
        {
            int id = _currentVariableId++;
            Variable variable = CreateNewVariable(mutable, id);
            SetVariableAtReferenceIndex(variable, id);
            return new VariableReference(this, id);
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

        public IEnumerable<VariableReference> GetVariablesInterruptedByLifetime(Lifetime lifetime)
        {
            List<Variable> variables;
            if (_variablesInterruptedByLifetimes.TryGetValue(lifetime, out variables))
            {
                // TODO: create new reference indices for these variables?
                return variables.Select(GetExistingReferenceForVariable);
            }
            return Enumerable.Empty<VariableReference>();
        }

        public Lifetime DefineLifetimeThatOutlastsDiagram()
        {
            return _boundedLifetimeGraph.CreateLifetimeThatOutlastsDiagram();
        }

        public Lifetime DefineLifetimeThatIsBoundedByDiagram(IEnumerable<VariableReference> decomposedVariables)
        {
            Lifetime lifetime = _boundedLifetimeGraph.CreateLifetimeThatIsBoundedByDiagram();
            _variablesInterruptedByLifetimes.Add(lifetime, decomposedVariables.Select(GetVariableForVariableReference).ToList());
            return lifetime;
        }

        public Lifetime ComputeCommonLifetime(Lifetime left, Lifetime right)
        {
            if (_boundedLifetimeGraph.DoesOutlast(left, right))
            {
                return right;
            }
            if (_boundedLifetimeGraph.DoesOutlast(right, left))
            {
                return left;
            }
            return Lifetime.Empty;
        }

        internal bool GetMutable(VariableReference variableReference) => GetVariableForVariableReference(variableReference).Mutable;

        internal NIType GetType(VariableReference variableReference) => GetVariableForVariableReference(variableReference).Type;

        internal Lifetime GetLifetime(VariableReference variableReference) => GetVariableForVariableReference(variableReference).Lifetime;

        internal int GetId(VariableReference variableReference) => GetVariableForVariableReference(variableReference).Id;

        internal void SetTypeAndLifetime(VariableReference variableReference, NIType type, Lifetime lifetime)
        {
            Variable variable = GetVariableForVariableReference(variableReference);
            variable.Type = type;
            variable.Lifetime = lifetime;
        }

        internal bool ReferenceSameVariable(VariableReference x, VariableReference y) => GetVariableForVariableReference(x) == GetVariableForVariableReference(y);

        internal int GetReferenceHashCode(VariableReference x) => GetVariableForVariableReference(x).GetHashCode();
    }
}
