﻿using System;
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
                return NITypes.Void;
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

        private abstract class ConcreteType : ParameterizedType
        {
            private readonly NIType _niType;

            public ConcreteType(
                NIType niType,
                TypeVariableReference[] typeParameters,
                IReadOnlyDictionary<string, TypeVariableReference> fieldTypes,
                TypeVariableReference[] implementedTraits,
                TraitDeriver traitDeriver)
                : base(typeParameters)
            {
                _niType = niType;
                FieldTypes = fieldTypes;
                ImplementedTraits = implementedTraits;
                TraitDeriver = traitDeriver;
            }

            protected NIType NIType => _niType;

            public string TypeName => _niType.GetName();

            public IReadOnlyDictionary<string, TypeVariableReference> FieldTypes { get; }

            public IReadOnlyList<TypeVariableReference> ImplementedTraits { get; }

            public TraitDeriver TraitDeriver { get; }

            public override bool IsOrContainsTypeVariable()
            {
                return TypeParameters.Any(t => t.IsOrContainsTypeVariable);
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
                    // TODO: need to create a supertype lifetime of all bounded lifetimes
                    // in TypeParameters
                    Lifetime commonBoundedLifetime = TypeParameters
                        .Select(t => t.Lifetime)
                        .Where(l => l.IsBounded)
                        .FirstOrDefault();
                    return commonBoundedLifetime ?? Lifetime.Unbounded;
                }
            }
        }

        private sealed class ConcreteClassType : ConcreteType
        {
            public ConcreteClassType(
                NIType niType,
                TypeVariableReference[] typeParameters,
                IReadOnlyDictionary<string, TypeVariableReference> fieldTypes,
                TypeVariableReference[] implementedTraits,
                TraitDeriver traitDeriver = null)
                : base(niType, typeParameters, fieldTypes, implementedTraits, traitDeriver)
            {
            }

            public override NIType RenderNIType()
            {
                if (NIType.IsGenericType())
                {
                    NIType genericTypeDefinition = NIType.IsGenericTypeDefinition() ? NIType : NIType.GetGenericTypeDefinition();
                    NIClassBuilder specializationTypeBuilder = genericTypeDefinition.DefineClassFromExisting();
                    NIType[] typeParameters = TypeParameters.Select(t => t.RenderNIType()).ToArray();
                    specializationTypeBuilder.ReplaceGenericParameters(typeParameters);
                    return specializationTypeBuilder.CreateType();
                }
                return NIType;
            }
        }

        private sealed class ConcreteUnionType : ConcreteType
        {
            public ConcreteUnionType(
                NIType niType,
                TypeVariableReference[] typeParameters,
                IReadOnlyDictionary<string, TypeVariableReference> fieldTypes,
                TypeVariableReference[] implementedTraits,
                TraitDeriver traitDeriver = null)
                : base(niType, typeParameters, fieldTypes, implementedTraits, traitDeriver)
            {
            }

            public override NIType RenderNIType()
            {
                if (NIType.IsGenericType())
                {
                    NIType genericTypeDefinition = NIType.IsGenericTypeDefinition() ? NIType : NIType.GetGenericTypeDefinition();
                    NIUnionBuilder specializationTypeBuilder = genericTypeDefinition.DefineUnionFromExisting();
                    NIType[] typeParameters = TypeParameters.Select(t => t.RenderNIType()).ToArray();
                    // TODO: verify that this works for union NITypes
                    return specializationTypeBuilder.CreateType().ReplaceGenericParameters(typeParameters);
                }
                return NIType;
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

        private sealed class TupleType : ParameterizedType
        {
            public TupleType(TypeVariableReference[] elementTypes)
                : base(elementTypes)
            {
            }

            public override string DebuggerDisplay
            {
                get
                {
                    var stringBuilder = new StringBuilder();
                    stringBuilder.Append("<");
                    stringBuilder.Append(string.Join(", ", TypeParameters.Select(t => t.DebuggerDisplay)));
                    stringBuilder.Append(">");
                    return stringBuilder.ToString();
                }
            }

            public override Lifetime Lifetime
            {
                get
                {
                    // TODO
                    return Lifetime.Unbounded;
                }
            }

            public override NIType RenderNIType()
            {
                return TypeParameters.Select(p => p.RenderNIType()).DefineTupleType();
            }
        }

        private sealed class IndefiniteFieldedType : TypeBase
        {
            public IndefiniteFieldedType(IReadOnlyDictionary<string, TypeVariableReference> fieldTypes)
            {
                FieldTypes = fieldTypes;
            }

            public IReadOnlyDictionary<string, TypeVariableReference> FieldTypes { get; }

            public override string DebuggerDisplay
            {
                get
                {
                    string joinedFields = string.Join(", ", FieldTypes.Select(fieldTypePair => $"{fieldTypePair.Key}: {fieldTypePair.Value.DebuggerDisplay}"));
                    return $"{{{joinedFields}}}";
                }
            }

            public override Lifetime Lifetime => Lifetime.Unbounded;

            // An IndefiniteFieldedType not unified with any ConcreteTypes cannot determine its NIType.
            public override NIType RenderNIType() => NITypes.Void;

            public override bool IsOrContainsTypeVariable() => true;
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
                return NIType.Unset;
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
                return NIType.Unset;
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

        public TypeVariableReference CreateReferenceToConcreteType(
            NIType niType,
            TypeVariableReference[] typeArguments,
            IReadOnlyDictionary<string, TypeVariableReference> fieldTypes,
            TypeVariableReference[] implementedTraits,
            TraitDeriver traitDeriver = null)
        {
            // TODO: we may need a separate ConcretePrimitiveType class
            if (niType.IsUnion())
            {
                return CreateReferenceToNewType(new ConcreteUnionType(niType, typeArguments, fieldTypes, implementedTraits, traitDeriver));
            }
            return CreateReferenceToNewType(new ConcreteClassType(niType, typeArguments, fieldTypes, implementedTraits, traitDeriver));
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

        public TypeVariableReference CreateReferenceToTupleType(TypeVariableReference[] elementTypes)
        {
            return CreateReferenceToNewType(new TupleType(elementTypes));
        }

        public TypeVariableReference CreateReferenceToIndefiniteFieldedType(IReadOnlyDictionary<string, TypeVariableReference> fieldTypes)
        {
            return CreateReferenceToNewType(new IndefiniteFieldedType(fieldTypes));
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

            IndefiniteFieldedType toUnifyFieldedType = toUnifyTypeBase as IndefiniteFieldedType,
                toUnifyWithFieldedType = toUnifyWithTypeBase as IndefiniteFieldedType;
            if (toUnifyConcrete != null && toUnifyWithFieldedType != null)
            {
                if (UnifyConcreteTypeWithIndefiniteFieldedType(toUnifyConcrete, toUnifyWithFieldedType, unificationResult))
                {
                    MergeTypeVariableIntoTypeVariable(toUnifyWith, toUnify);
                }
                return;
            }
            if (toUnifyWithConcrete != null && toUnifyFieldedType != null)
            {
                if (UnifyConcreteTypeWithIndefiniteFieldedType(toUnifyWithConcrete, toUnifyFieldedType, unificationResult))
                {
                    MergeTypeVariableIntoTypeVariable(toUnify, toUnifyWith);
                }
                return;
            }

            TupleType toUnifyTuple = toUnifyTypeBase as TupleType,
                toUnifyWithTuple = toUnifyWithTypeBase as TupleType;
            if (toUnifyTuple != null && toUnifyWithTuple != null)
            {
                if (toUnifyTuple.TypeParameters.Count != toUnifyWithTuple.TypeParameters.Count)
                {
                    unificationResult.SetTypeMismatch();
                    return;
                }

                foreach (var typeParameterPair in toUnifyTuple.TypeParameters.Zip(toUnifyWithTuple.TypeParameters))
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

        private bool UnifyConcreteTypeWithIndefiniteFieldedType(ConcreteType concreteType, IndefiniteFieldedType fieldedType, ITypeUnificationResult unificationResult)
        {
            bool success = true;
            foreach (var fieldPair in fieldedType.FieldTypes)
            {
                string fieldName = fieldPair.Key;
                TypeVariableReference concreteField;
                if (concreteType.FieldTypes.TryGetValue(fieldName, out concreteField))
                {
                    Unify(fieldPair.Value, concreteField, unificationResult);
                }
                else
                {
                    success = false;
                }
            }
            return success;
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
            return typeBase?.RenderNIType() ?? NITypes.Void;
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

            // Somewhat of a hack: we want the Clone trait to be found for anything that has the Copy trait.
            if (traitName == "Clone")
            {
                return TryGetImplementedTrait(type, "Copy", out traitType);
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

    internal abstract class TraitConstraint : Constraint
    {
        public abstract string TraitName { get; }
    }

    internal class SimpleTraitConstraint : TraitConstraint
    {
        public SimpleTraitConstraint(string traitName)
        {
            TraitName = traitName;
        }

        public override void ValidateConstraintForType(TypeVariableReference type, ITypeUnificationResult unificationResult)
        {
            TypeVariableReference unused;
            if (!type.TypeVariableSet.TryGetImplementedTrait(type, TraitName, out unused))
            {
                unificationResult.AddFailedTypeConstraint(this);
            }
        }

        public override string TraitName { get; }
    }

    internal class IteratorTraitConstraint : TraitConstraint
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

        public override string TraitName => "Iterator";
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
