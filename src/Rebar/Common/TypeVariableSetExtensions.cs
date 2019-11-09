using System;
using System.Collections.Generic;
using System.Linq;
using NationalInstruments;
using NationalInstruments.DataTypes;

namespace Rebar.Common
{
    internal static class TypeVariableSetExtensions
    {
        public static Dictionary<NIType, TypeVariableReference> CreateTypeVariablesForGenericParameters(
            this TypeVariableSet typeVariableSet,
            NIType genericType,
            Func<NIType, TypeVariableReference> createLifetimeTypeReference)
        {
            Dictionary<NIType, TypeVariableReference> genericTypeParameters = new Dictionary<NIType, TypeVariableReference>();
            foreach (NIType genericParameterNIType in genericType.GetGenericParameters())
            {
                if (genericParameterNIType.IsGenericParameter())
                {
                    if (genericParameterNIType.IsLifetimeType())
                    {
                        genericTypeParameters[genericParameterNIType] = createLifetimeTypeReference(genericParameterNIType);
                    }
                    else if (genericParameterNIType.IsMutabilityType())
                    {
                        genericTypeParameters[genericParameterNIType] = typeVariableSet.CreateReferenceToMutabilityType();
                    }
                    else
                    {
                        var typeConstraints = genericParameterNIType.GetConstraints().Select(CreateConstraintFromGenericNITypeConstraint).ToList();
                        genericTypeParameters[genericParameterNIType] = typeVariableSet.CreateReferenceToNewTypeVariable(typeConstraints);
                    }
                }
            }
            return genericTypeParameters;
        }

        public static TypeVariableReference CreateTypeVariableReferenceFromNIType(
            this TypeVariableSet typeVariableSet,
            NIType type)
        {
            return typeVariableSet.CreateTypeVariableReferenceFromNIType(type, new Dictionary<NIType, TypeVariableReference>());
        }

        public static TypeVariableReference CreateTypeVariableReferenceFromNIType(
            this TypeVariableSet typeVariableSet,
            NIType type,
            Dictionary<NIType, TypeVariableReference> genericTypeParameters)
        {
            if (type.IsGenericParameter())
            {
                return genericTypeParameters[type];
            }

            if (type.IsInterface())
            {
                return typeVariableSet.CreateTypeVariableReferenceFromInterfaceNIType(type, genericTypeParameters);
            }

            if (!type.IsClass())
            {
                return typeVariableSet.CreateTypeVariableReferenceFromPrimitiveType(type, genericTypeParameters);
            }

            if (type.IsRebarReferenceType())
            {
                TypeVariableReference referentType = typeVariableSet.CreateTypeVariableReferenceFromNIType(type.GetReferentType(), genericTypeParameters);
                TypeVariableReference lifetimeType = typeVariableSet.CreateTypeVariableReferenceFromNIType(type.GetReferenceLifetimeType(), genericTypeParameters);
                if (type.IsPolymorphicReferenceType())
                {
                    TypeVariableReference mutabilityType = typeVariableSet.CreateTypeVariableReferenceFromNIType(type.GetReferenceMutabilityType(), genericTypeParameters);
                    return typeVariableSet.CreateReferenceToPolymorphicReferenceType(mutabilityType, referentType, lifetimeType);
                }
                return typeVariableSet.CreateReferenceToReferenceType(type.IsMutableReferenceType(), referentType, lifetimeType);
            }

            return typeVariableSet.CreateTypeVariableReferenceFromClassNIType(type, genericTypeParameters);
        }

        private static TypeVariableReference CreateTypeVariableReferenceFromPrimitiveType(
            this TypeVariableSet typeVariableSet,
            NIType type,
            Dictionary<NIType, TypeVariableReference> genericTypeParameters)
        {
            var implementedTraits = new List<TypeVariableReference>();
            if (type.IsInteger() || type.IsBoolean())
            {
                // TODO: cache parameterless trait references?
                implementedTraits.Add(typeVariableSet.CreateReferenceToParameterlessTraitType("Display"));
                implementedTraits.Add(typeVariableSet.CreateReferenceToParameterlessTraitType("Clone"));
                implementedTraits.Add(typeVariableSet.CreateReferenceToParameterlessTraitType("Copy"));
            }
            else if (type.IsString())
            {
                implementedTraits.Add(typeVariableSet.CreateReferenceToParameterlessTraitType("Display"));
                implementedTraits.Add(typeVariableSet.CreateReferenceToParameterlessTraitType("Clone"));
            }
            else
            {
                throw new NotSupportedException("Unknown non-class type: " + type);
            }
            return typeVariableSet.CreateReferenceToConcreteType(type, new TypeVariableReference[0], implementedTraits.ToArray());
        }

        private static TypeVariableReference CreateTypeVariableReferenceFromInterfaceNIType(
            this TypeVariableSet typeVariableSet,
            NIType type,
            Dictionary<NIType, TypeVariableReference> genericTypeParameters)
        {
            string typeName = type.GetName();
            if (type.IsGenericType())
            {
                TypeVariableReference[] parameterTypeVariables = type
                    .GetGenericParameters()
                    .Select(t => typeVariableSet.CreateTypeVariableReferenceFromNIType(t, genericTypeParameters))
                    .ToArray();
                return typeVariableSet.CreateReferenceToTraitType(typeName, parameterTypeVariables);
            }
            return typeVariableSet.CreateReferenceToParameterlessTraitType(typeName);
        }

        private static TypeVariableReference CreateTypeVariableReferenceFromClassNIType(
            this TypeVariableSet typeVariableSet,
            NIType type,
            Dictionary<NIType, TypeVariableReference> genericTypeParameters)
        {
            TypeVariableReference[] parameterTypeVariables = type.IsGenericType()
                ? type.GetGenericParameters().Select(t => typeVariableSet.CreateTypeVariableReferenceFromNIType(t, genericTypeParameters)).ToArray()
                : new TypeVariableReference[0];
            TypeVariableReference[] traits = typeVariableSet.GetImplementedTraitTypeVariables(type, genericTypeParameters).ToArray();

            // TODO: eventually it would be nice to decorate the generic type definition with [Derive] attributes
            // that say which traits to derive from the inner type, so that this can be made more generic.
            TraitDeriver traitDeriver = null;
            if (type.GetName() == "Option")
            {
                traitDeriver = new TraitDeriver(parameterTypeVariables[0], "Copy");
            }
            if (type.GetName() == "Vector")
            {
                // Vector<T> is Clone only for T : Clone
                traitDeriver = new TraitDeriver(parameterTypeVariables[0], "Clone");
            }

            return typeVariableSet.CreateReferenceToConcreteType(type, parameterTypeVariables, traits, traitDeriver);
        }

        private static IEnumerable<TypeVariableReference> GetImplementedTraitTypeVariables(this TypeVariableSet typeVariableSet, NIType type, Dictionary<NIType, TypeVariableReference> genericTypeParameters)
        {
            if (!type.IsClassOrInterface())
            {
                return Enumerable.Empty<TypeVariableReference>();
            }
            return type
                .GetInterfaces()
                .Select(i => typeVariableSet.CreateTypeVariableReferenceFromNIType(i, genericTypeParameters));
        }

        private static Constraint CreateConstraintFromGenericNITypeConstraint(NIType niTypeConstraint)
        {
            if (niTypeConstraint.IsInterface())
            {
                string interfaceName = niTypeConstraint.GetName();
                switch (interfaceName)
                {
                    case "Clone":
                        return new CloneTraitConstraint();
                    case "Copy":
                        return new CopyTraitConstraint();
                    case "Display":
                        return new DisplayTraitConstraint();
                }
            }
            throw new NotImplementedException("Don't know how to translate generic type constraint " + niTypeConstraint);
        }

        public static TypeVariableReference CreateReferenceToLockingCellType(this TypeVariableSet typeVariableSet, TypeVariableReference innerType)
        {
            return typeVariableSet.CreateReferenceToGenericTypeSpecializedWithTypeParameters(DataTypes.LockingCellGenericType, innerType);
        }

        public static TypeVariableReference CreateReferenceToOptionType(this TypeVariableSet typeVariableSet, TypeVariableReference innerType)
        {
            return typeVariableSet.CreateReferenceToGenericTypeSpecializedWithTypeParameters(DataTypes.OptionGenericType, innerType);
        }

        public static TypeVariableReference CreateReferenceToGenericTypeSpecializedWithTypeParameters(
            this TypeVariableSet typeVariableSet,
            NIType genericTypeDefinition,
            params TypeVariableReference[] typeParameters)
        {
            if (typeParameters.Length != genericTypeDefinition.GetGenericParameters().Count)
            {
                throw new ArgumentException("Wrong number of parameter type variables; expected " + genericTypeDefinition.GetGenericParameters().Count);
            }
            var genericTypeParameters = new Dictionary<NIType, TypeVariableReference>();
            foreach (var pair in genericTypeDefinition.GetGenericParameters().Zip(typeParameters))
            {
                NIType genericParameter = pair.Key;
                TypeVariableReference parameterTypeVariable = pair.Value;
                genericTypeParameters[genericParameter] = parameterTypeVariable;
            }
            return typeVariableSet.CreateTypeVariableReferenceFromNIType(genericTypeDefinition, genericTypeParameters);
        }
    }
}
