using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using NationalInstruments;
using NationalInstruments.DataTypes;

namespace Rebar.Common
{
    internal sealed class TypeVariableSet
    {
        #region Type variable kinds

        [DebuggerDisplay("{DebuggerDisplay}")]
        private abstract class TypeBase
        {
            public abstract string DebuggerDisplay { get; }

            public abstract NIType RenderNIType();

            public abstract Lifetime Lifetime { get; }

            public virtual bool IsOrContainsTypeVariable()
            {
                return false;
            }
        }

        private sealed class TypeVariable : TypeBase
        {
            private Constraint[] _constraints;

            public TypeVariable(int id, IEnumerable<Constraint> constraints)
            {
                Id = id;
                _constraints = constraints.ToArray();
            }

            public int Id { get; }

            public IEnumerable<Constraint> Constraints => _constraints;

            public void AdoptConstraintsFromVariable(TypeVariable other)
            {
                _constraints = _constraints.Concat(other._constraints).ToArray();
            }

            public override string DebuggerDisplay => $"T${Id}";

            public override NIType RenderNIType()
            {
                return PFTypes.Void;
            }

            public override Lifetime Lifetime => Lifetime.Empty;

            public override bool IsOrContainsTypeVariable() => true;
        }

        private abstract class ParameterizedType : TypeBase
        {
            public IReadOnlyList<TypeVariableReference> TypeParameters { get; }

            protected ParameterizedType(TypeVariableReference[] typeParameters)
            {
                TypeParameters = typeParameters;
            }
        }

        private sealed class ConcreteType : ParameterizedType
        {
            private readonly NIType _niType;

            public ConcreteType(NIType niType, TypeVariableReference[] typeParameters, TypeVariableReference[] implementedTraits, TraitDeriver traitDeriver = null)
                : base(typeParameters)
            {
                _niType = niType;
                ImplementedTraits = implementedTraits;
                TraitDeriver = traitDeriver;
            }

            public string TypeName => _niType.GetName();

            public IReadOnlyList<TypeVariableReference> ImplementedTraits { get; }

            public TraitDeriver TraitDeriver { get; }

            public override bool IsOrContainsTypeVariable()
            {
                return TypeParameters.Count > 0;
            }

            public override string DebuggerDisplay
            {
                get
                {
                    var stringBuilder = new StringBuilder(_niType.GetName());
                    if (TypeParameters.Any())
                    {
                        stringBuilder.Append("<");
                        stringBuilder.Append(String.Join(", ", TypeParameters.Select(t => t.DebuggerDisplay)));
                        stringBuilder.Append(">");
                    }
                    if (ImplementedTraits.Any())
                    {
                        stringBuilder.Append(" : ");
                        stringBuilder.Append(String.Join(", ", ImplementedTraits.Select(t => t.DebuggerDisplay)));
                    }
                    return stringBuilder.ToString();
                }
            }

            public override Lifetime Lifetime
            {
                get
                {
                    // TODO
                    if (TypeParameters.Any())
                    {
                        return TypeParameters.First().Lifetime;
                    }
                    return Lifetime.Unbounded;
                }
            }

            public override NIType RenderNIType()
            {
                if (_niType.IsGenericType())
                {
                    NIType genericTypeDefinition = _niType.IsGenericTypeDefinition() ? _niType : _niType.GetGenericTypeDefinition();
                    NIClassBuilder specializationTypeBuilder = genericTypeDefinition.DefineClassFromExisting();
                    NIType[] typeParameters = TypeParameters.Select(t => t.RenderNIType()).ToArray();
                    specializationTypeBuilder.ReplaceGenericParameters(typeParameters);
                    return specializationTypeBuilder.CreateType();
                }
                return _niType;
            }
        }

        private sealed class TraitType : ParameterizedType
        {
            public TraitType(string traitName, TypeVariableReference[] typeParameters)
                : base(typeParameters)
            {
                Name = traitName;
            }

            public string Name { get; }

            public override string DebuggerDisplay => Name;

            public override Lifetime Lifetime => Lifetime.Unbounded;

            public override NIType RenderNIType()
            {
                throw new NotImplementedException();
            }
        }

        private sealed class ReferenceType : TypeBase
        {
            private abstract class Mutability
            {
                public abstract bool Mutable { get; }
                public abstract void UnifyMutability(Mutability unifyWith);
            }

            private sealed class ConstantMutability : Mutability
            {
                public ConstantMutability(bool mutable)
                {
                    Mutable = mutable;
                }

                public override bool Mutable { get; }

                public override void UnifyMutability(Mutability unifyWith)
                {
                    var unifyWithConstant = unifyWith as ConstantMutability;
                    if (unifyWithConstant != null)
                    {
                        if (Mutable != unifyWithConstant.Mutable)
                        {
                            // type error
                        }
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }

            private sealed class VariableMutability : Mutability
            {
                private readonly TypeVariableReference _mutabilityVariable;

                public VariableMutability(TypeVariableReference mutabilityVariable)
                {
                    _mutabilityVariable = mutabilityVariable;
                }

                public override void UnifyMutability(Mutability unifyWith)
                {
                    ConstantMutability constant = unifyWith as ConstantMutability;
                    if (constant != null)
                    {
                        _mutabilityVariable.AndWith(constant.Mutable);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }

                public override bool Mutable
                {
                    get
                    {
                        return _mutabilityVariable.Mutable;
                    }
                }
            }

            private readonly Mutability _mutability;

            public ReferenceType(bool mutable, TypeVariableReference underlyingType, TypeVariableReference lifetimeType)
            {
                _mutability = new ConstantMutability(mutable);
                UnderlyingType = underlyingType;
                LifetimeType = lifetimeType;
            }

            public ReferenceType(TypeVariableReference mutability, TypeVariableReference underlyingType, TypeVariableReference lifetimeType)
            {
                _mutability = new VariableMutability(mutability);
                UnderlyingType = underlyingType;
                LifetimeType = lifetimeType;
            }

            public bool Mutable => _mutability.Mutable;

            public TypeVariableReference UnderlyingType { get; }

            public TypeVariableReference LifetimeType { get; }

            public void UnifyMutability(ReferenceType unifyWith)
            {
                _mutability.UnifyMutability(unifyWith._mutability);
            }

            public override string DebuggerDisplay
            {
                get
                {
                    string mut = _mutability.Mutable ? "mut " : string.Empty;
                    return $"& ({LifetimeType.DebuggerDisplay}) {mut}{UnderlyingType.DebuggerDisplay}";
                }
            }

            public override NIType RenderNIType()
            {
                NIType underlyingNIType = UnderlyingType.RenderNIType();
                return _mutability.Mutable ? underlyingNIType.CreateMutableReference() : underlyingNIType.CreateImmutableReference();
            }

            public override Lifetime Lifetime => LifetimeType.Lifetime;

            public override bool IsOrContainsTypeVariable()
            {
                return UnderlyingType.IsOrContainsTypeVariable;
            }
        }

        private sealed class LifetimeTypeContainer : TypeBase
        {
            private readonly Lazy<Lifetime> _lazyNewLifetime;

            public LifetimeTypeContainer(Lazy<Lifetime> lazyNewLifetime)
            {
                _lazyNewLifetime = lazyNewLifetime;
            }

            public Lifetime LifetimeValue { get; private set; }

            public override Lifetime Lifetime => LifetimeValue;

            public void AdoptLifetimeIfPossible(Lifetime lifetime)
            {
                if (LifetimeValue == null)
                {
                    LifetimeValue = lifetime;
                }
                else if (LifetimeValue != lifetime)
                {
                    AdoptNewLifetime();
                }
                // TODO: instead of using a canned supertype lifetime, it would be good to construct new supertype
                // lifetimes from whatever we get unified with on the fly
            }

            public void AdoptNewLifetime()
            {
                LifetimeValue = _lazyNewLifetime.Value;
            }

            public override string DebuggerDisplay
            {
                get
                {
                    // TODO
                    return "Lifetime";
                }
            }

            public override NIType RenderNIType()
            {
                throw new NotImplementedException();
            }
        }

        private sealed class MutabilityTypeVariable : TypeBase
        {
            private bool? _value;

            public void AndWith(bool value)
            {
                if (!_value.HasValue)
                {
                    _value = value;
                }
                else
                {
                    _value = (_value.Value && value);
                }
            }

            public bool Mutable
            {
                get
                {
                    if (_value.HasValue)
                    {
                        return _value.Value;
                    }
                    else
                    {
                        throw new InvalidOperationException("Mutability has not been determined");
                    }
                }
            }

            public override string DebuggerDisplay
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override Lifetime Lifetime
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override NIType RenderNIType()
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        private readonly List<TypeBase> _types = new List<TypeBase>();
        private readonly List<TypeBase> _typeReferences = new List<TypeBase>();
        private int _currentReferenceIndex = 0;
        private int _currentTypeVariable = 0;
        private readonly Dictionary<string, TypeVariableReference> _parameterlessTraits = new Dictionary<string, TypeVariableReference>();

        public TypeVariableReference CreateReferenceToNewTypeVariable()
        {
            return CreateReferenceToNewTypeVariable(Enumerable.Empty<Constraint>());
        }

        public TypeVariableReference CreateReferenceToNewTypeVariable(IEnumerable<Constraint> constraints)
        {
            int id = _currentTypeVariable++;
            return CreateReferenceToNewType(new TypeVariable(id, constraints));
        }

        public TypeVariableReference CreateReferenceToConcreteType(NIType niType, TypeVariableReference[] typeArguments, TypeVariableReference[] implementedTraits, TraitDeriver traitDeriver = null)
        {
            return CreateReferenceToNewType(new ConcreteType(niType, typeArguments, implementedTraits, traitDeriver));
        }

        public TypeVariableReference CreateReferenceToTraitType(string typeName, TypeVariableReference[] typeArguments)
        {
            return CreateReferenceToNewType(new TraitType(typeName, typeArguments));
        }

        public TypeVariableReference CreateReferenceToParameterlessTraitType(string typeName)
        {
            TypeVariableReference traitType;
            if (!_parameterlessTraits.TryGetValue(typeName, out traitType))
            {
                traitType = CreateReferenceToTraitType(typeName, new TypeVariableReference[0]);
                _parameterlessTraits[typeName] = traitType;
            }
            return traitType;
        }

        public TypeVariableReference CreateReferenceToReferenceType(bool mutable, TypeVariableReference underlyingType, TypeVariableReference lifetimeType)
        {
            return CreateReferenceToNewType(new ReferenceType(mutable, underlyingType, lifetimeType));
        }

        public TypeVariableReference CreateReferenceToPolymorphicReferenceType(TypeVariableReference mutabilityType, TypeVariableReference underlyingType, TypeVariableReference lifetimeType)
        {
            return CreateReferenceToNewType(new ReferenceType(mutabilityType, underlyingType, lifetimeType));
        }

        public TypeVariableReference CreateReferenceToLifetimeType(Lazy<Lifetime> lazyNewLifetime)
        {
            return CreateReferenceToNewType(new LifetimeTypeContainer(lazyNewLifetime));
        }

        public TypeVariableReference CreateReferenceToLifetimeType(Lifetime lifetime)
        {
            var lifetimeTypeContainer = new LifetimeTypeContainer(null);
            lifetimeTypeContainer.AdoptLifetimeIfPossible(lifetime);
            return CreateReferenceToNewType(lifetimeTypeContainer);
        }

        public TypeVariableReference CreateReferenceToMutabilityType()
        {
            return CreateReferenceToNewType(new MutabilityTypeVariable());
        }

        private TypeVariableReference CreateReferenceToNewType(TypeBase type)
        {
            int referenceIndex = _currentReferenceIndex++;
            _types.Add(type);
            SetTypeAtReferenceIndex(type, referenceIndex);
            return new TypeVariableReference(this, referenceIndex);
        }

        private void SetTypeAtReferenceIndex(TypeBase type, int referenceIndex)
        {
            while (_typeReferences.Count <= referenceIndex)
            {
                _typeReferences.Add(null);
            }
            _typeReferences[referenceIndex] = type;
        }

        private TypeBase GetTypeForTypeVariableReference(TypeVariableReference typeVariableReference)
        {
            return _typeReferences[typeVariableReference.ReferenceIndex];
        }

        private void MergeTypeVariableIntoTypeVariable(TypeVariableReference toMerge, TypeVariableReference mergeInto)
        {
            TypeBase typeToMerge = GetTypeForTypeVariableReference(toMerge),
                typeToMergeInto = GetTypeForTypeVariableReference(mergeInto);
            if (typeToMerge != null && typeToMergeInto != null)
            {
                for (int i = 0; i < _typeReferences.Count; ++i)
                {
                    if (_typeReferences[i] == typeToMerge)
                    {
                        _typeReferences[i] = typeToMergeInto;
                    }
                }
            }
            _types.Remove(typeToMerge);
        }

        public void Unify(TypeVariableReference toUnify, TypeVariableReference toUnifyWith, ITypeUnificationResult unificationResult)
        {
            TypeBase toUnifyTypeBase = GetTypeForTypeVariableReference(toUnify),
                toUnifyWithTypeBase = GetTypeForTypeVariableReference(toUnifyWith);

            ConcreteType toUnifyConcrete = toUnifyTypeBase as ConcreteType,
                toUnifyWithConcrete = toUnifyWithTypeBase as ConcreteType;
            if (toUnifyConcrete != null && toUnifyWithConcrete != null)
            {
                if (toUnifyConcrete.TypeName != toUnifyWithConcrete.TypeName)
                {
                    unificationResult.SetTypeMismatch();
                    return;
                }
                if (toUnifyConcrete.TypeParameters.Count != toUnifyWithConcrete.TypeParameters.Count)
                {
                    unificationResult.SetTypeMismatch();
                    return;
                }

                foreach (var typeParameterPair in toUnifyConcrete.TypeParameters.Zip(toUnifyWithConcrete.TypeParameters))
                {
                    Unify(typeParameterPair.Key, typeParameterPair.Value, unificationResult);
                }
                MergeTypeVariableIntoTypeVariable(toUnify, toUnifyWith);
                return;
            }

            ReferenceType toUnifyReference = toUnifyTypeBase as ReferenceType,
                toUnifyWithReference = toUnifyWithTypeBase as ReferenceType;
            if (toUnifyReference != null && toUnifyWithReference != null)
            {
                toUnifyReference.UnifyMutability(toUnifyWithReference);
                Unify(toUnifyReference.UnderlyingType, toUnifyWithReference.UnderlyingType, unificationResult);
                Unify(toUnifyReference.LifetimeType, toUnifyWithReference.LifetimeType, unificationResult);
                return;
            }

            LifetimeTypeContainer toUnifyLifetime = toUnifyTypeBase as LifetimeTypeContainer,
                toUnifyWithLifetime = toUnifyWithTypeBase as LifetimeTypeContainer;
            if (toUnifyLifetime != null && toUnifyWithLifetime != null)
            {
                // toUnify is the possible supertype container here
                toUnifyLifetime.AdoptLifetimeIfPossible(toUnifyWithLifetime.LifetimeValue);
                return;
            }

            TypeVariable toUnifyTypeVariable = toUnifyTypeBase as TypeVariable,
                toUnifyWithTypeVariable = toUnifyWithTypeBase as TypeVariable;
            if (toUnifyTypeVariable != null && toUnifyWithTypeVariable != null)
            {
                toUnifyWithTypeVariable.AdoptConstraintsFromVariable(toUnifyTypeVariable);
                MergeTypeVariableIntoTypeVariable(toUnify, toUnifyWith);
                return;
            }
            if (toUnifyTypeVariable != null)
            {
                UnifyTypeVariableWithNonTypeVariable(toUnify, toUnifyWith, unificationResult);
                return;
            }
            if (toUnifyWithTypeVariable != null)
            {
                UnifyTypeVariableWithNonTypeVariable(toUnifyWith, toUnify, unificationResult);
                return;
            }

            unificationResult.SetTypeMismatch();
            return;
        }

        private void UnifyTypeVariableWithNonTypeVariable(TypeVariableReference typeVariable, TypeVariableReference nonTypeVariable, ITypeUnificationResult unificationResult)
        {
            var t = (TypeVariable)GetTypeForTypeVariableReference(typeVariable);
            foreach (Constraint constraint in t.Constraints)
            {
                constraint.ValidateConstraintForType(nonTypeVariable, unificationResult);
            }
            MergeTypeVariableIntoTypeVariable(typeVariable, nonTypeVariable);
        }

        public string GetDebuggerDisplay(TypeVariableReference typeVariableReference)
        {
            TypeBase typeBase = GetTypeForTypeVariableReference(typeVariableReference);
            return typeBase?.DebuggerDisplay ?? "invalid";
        }

        public NIType RenderNIType(TypeVariableReference typeVariableReference)
        {
            TypeBase typeBase = GetTypeForTypeVariableReference(typeVariableReference);
            return typeBase?.RenderNIType() ?? PFTypes.Void;
        }

        public Lifetime GetLifetime(TypeVariableReference typeVariableReference)
        {
            TypeBase typeBase = GetTypeForTypeVariableReference(typeVariableReference);
            return typeBase?.Lifetime ?? Lifetime.Empty;
        }

        public bool TryDecomposeReferenceType(TypeVariableReference type, out TypeVariableReference underlyingType, out TypeVariableReference lifetimeType, out bool mutable)
        {
            TypeBase typeBase = GetTypeForTypeVariableReference(type);
            var referenceType = typeBase as ReferenceType;
            if (referenceType == null)
            {
                underlyingType = lifetimeType = default(TypeVariableReference);
                mutable = false;
                return false;
            }
            underlyingType = referenceType.UnderlyingType;
            lifetimeType = referenceType.LifetimeType;
            mutable = referenceType.Mutable;
            return true;
        }

        public bool TryGetImplementedTrait(TypeVariableReference type, string traitName, out TypeVariableReference traitType)
        {
            traitType = default(TypeVariableReference);
            var concreteType = GetTypeForTypeVariableReference(type) as ConcreteType;
            if (concreteType != null)
            {
                foreach (TypeVariableReference implementedTraitType in concreteType.ImplementedTraits)
                {
                    var trait = GetTypeForTypeVariableReference(implementedTraitType) as TraitType;
                    if (trait != null && trait.Name == traitName)
                    {
                        traitType = implementedTraitType;
                        return true;
                    }
                }
                if (concreteType.TraitDeriver?.TryGetDerivedTrait(this, traitName, out traitType) ?? false)
                {
                    return true;
                }
            }
            var referenceType = GetTypeForTypeVariableReference(type) as ReferenceType;
            if (referenceType != null)
            {
                if (traitName == "Copy" && !referenceType.Mutable)
                {
                    traitType = CreateReferenceToParameterlessTraitType("Copy");
                    return true;
                }
            }
            return false;
        }

        public string GetTypeName(TypeVariableReference type)
        {
            TypeBase typeBase = GetTypeForTypeVariableReference(type);
            var concreteType = typeBase as ConcreteType;
            if (concreteType != null)
            {
                return concreteType.TypeName;
            }
            var traitType = typeBase as TraitType;
            if (traitType != null)
            {
                return traitType.Name;
            }
            return null;
        }

        public IEnumerable<TypeVariableReference> GetTypeParameters(TypeVariableReference type)
        {
            var parameterizedType = GetTypeForTypeVariableReference(type) as ParameterizedType;
            if (parameterizedType != null)
            {
                return parameterizedType.TypeParameters;
            }
            return Enumerable.Empty<TypeVariableReference>();
        }

        public void AndWith(TypeVariableReference type, bool value)
        {
            var mutabilityType = GetTypeForTypeVariableReference(type) as MutabilityTypeVariable;
            if (mutabilityType == null)
            {
                throw new ArgumentException("Type should be a mutability type");
            }
            mutabilityType.AndWith(value);
        }

        public bool GetMutable(TypeVariableReference type)
        {
            var mutabilityType = GetTypeForTypeVariableReference(type) as MutabilityTypeVariable;
            if (mutabilityType == null)
            {
                throw new ArgumentException("Type should be a mutability type");
            }
            return mutabilityType.Mutable;
        }

        public bool GetIsOrContainsTypeVariable(TypeVariableReference type)
        {
            return GetTypeForTypeVariableReference(type).IsOrContainsTypeVariable();
        }
    }

    [DebuggerDisplay("{DebuggerDisplay}")]
    internal struct TypeVariableReference
    {
        public TypeVariableReference(TypeVariableSet typeVariableSet, int referenceIndex)
        {
            TypeVariableSet = typeVariableSet;
            ReferenceIndex = referenceIndex;
        }

        public TypeVariableSet TypeVariableSet { get; }

        public int ReferenceIndex { get; }

        public string DebuggerDisplay => TypeVariableSet.GetDebuggerDisplay(this);

        public NIType RenderNIType() => TypeVariableSet.RenderNIType(this);

        public Lifetime Lifetime => TypeVariableSet.GetLifetime(this);

        // TODO: these two should hopefully be temporary
        public void AndWith(bool value) => TypeVariableSet.AndWith(this, value);

        public bool Mutable => TypeVariableSet.GetMutable(this);

        public bool IsOrContainsTypeVariable => TypeVariableSet.GetIsOrContainsTypeVariable(this);
    }

    internal abstract class Constraint
    {
        public abstract void ValidateConstraintForType(TypeVariableReference type, ITypeUnificationResult unificationResult);
    }

    internal class CopyTraitConstraint : Constraint
    {
        public override void ValidateConstraintForType(TypeVariableReference type, ITypeUnificationResult unificationResult)
        {
            TypeVariableReference copyTrait;
            if (!type.TypeVariableSet.TryGetImplementedTrait(type, "Copy", out copyTrait))
            {
                unificationResult.AddFailedTypeConstraint(this);
            }
        }
    }

    internal class CloneTraitConstraint : Constraint
    {
        public override void ValidateConstraintForType(TypeVariableReference type, ITypeUnificationResult unificationResult)
        {
            TypeVariableReference unused;
            TypeVariableSet typeVariableSet = type.TypeVariableSet;
            if (!typeVariableSet.TryGetImplementedTrait(type, "Clone", out unused)
                && !typeVariableSet.TryGetImplementedTrait(type, "Copy", out unused))
            {
                unificationResult.AddFailedTypeConstraint(this);
            }
        }
    }

    internal class DisplayTraitConstraint : Constraint
    {
        public override void ValidateConstraintForType(TypeVariableReference type, ITypeUnificationResult unificationResult)
        {
            TypeVariableReference displayTrait;
            if (!type.TypeVariableSet.TryGetImplementedTrait(type, "Display", out displayTrait))
            {
                unificationResult.AddFailedTypeConstraint(this);
            }
        }
    }

    internal class IteratorTraitConstraint : Constraint
    {
        private readonly TypeVariableReference _itemTypeVariable;

        public IteratorTraitConstraint(TypeVariableReference itemTypeVariable)
        {
            _itemTypeVariable = itemTypeVariable;
        }

        public override void ValidateConstraintForType(TypeVariableReference type, ITypeUnificationResult unificationResult)
        {
            TypeVariableSet typeVariableSet = type.TypeVariableSet;
            TypeVariableReference iteratorTraitReference;
            if (typeVariableSet.TryGetImplementedTrait(type, "Iterator", out iteratorTraitReference))
            {
                TypeVariableReference itemTypeReference = typeVariableSet.GetTypeParameters(iteratorTraitReference).First();
                type.TypeVariableSet.Unify(_itemTypeVariable, itemTypeReference, unificationResult);
            }
            else
            {
                unificationResult.AddFailedTypeConstraint(this);
            }
        }
    }

    internal class OutlastsLifetimeGraphConstraint : Constraint
    {
        private readonly LifetimeGraphIdentifier _lifetimeGraph;

        public OutlastsLifetimeGraphConstraint(LifetimeGraphIdentifier lifetimeGraph)
        {
            _lifetimeGraph = lifetimeGraph;
        }

        public override void ValidateConstraintForType(TypeVariableReference type, ITypeUnificationResult unificationResult)
        {
            if (!type.Lifetime.DoesOutlastLifetimeGraph(_lifetimeGraph))
            {
                unificationResult.AddFailedTypeConstraint(this);
            }
        }
    }

    internal sealed class TraitDeriver
    {
        private readonly TypeVariableReference _deriveFrom;
        private readonly string[] _derivedTraitNames;

        public TraitDeriver(TypeVariableReference deriveFrom, params string[] derivedTraitNames)
        {
            _deriveFrom = deriveFrom;
            _derivedTraitNames = derivedTraitNames;
        }

        public bool TryGetDerivedTrait(TypeVariableSet typeVariableSet, string traitName, out TypeVariableReference derivedTrait)
        {
            if (!_derivedTraitNames.Contains(traitName))
            {
                derivedTrait = default(TypeVariableReference);
                return false;
            }
            return typeVariableSet.TryGetImplementedTrait(_deriveFrom, traitName, out derivedTrait);
        }
    }
}
