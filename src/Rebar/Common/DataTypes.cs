using System;
using System.Linq;
using NationalInstruments.DataTypes;

namespace Rebar.Common
{
    public static class DataTypes
    {
        public const string RebarTypeKeyword = "RebarType";

        private static NIType MutableReferenceGenericType { get; }

        private static NIType ImmutableReferenceGenericType { get; }

        private static NIType PolymorphicReferenceGenericType { get; }

        private static NIType OptionGenericType { get; }

        private static NIType LockingCellGenericType { get; }

        private static NIType NonLockingCellGenericType { get; }

        private static NIType IteratorGenericType { get; }

        private static NIType VectorGenericType { get; }

        public static NIType StringSliceType { get; }

        static DataTypes()
        {
            var mutableReferenceGenericTypeBuilder = PFTypes.Factory.DefineReferenceClass("MutableReference");
            mutableReferenceGenericTypeBuilder.MakeGenericParameters("TDeref", "TLife");
            mutableReferenceGenericTypeBuilder.AddTypeKeywordProviderAttribute(RebarTypeKeyword);
            MutableReferenceGenericType = mutableReferenceGenericTypeBuilder.CreateType();

            var immutableReferenceGenericTypeBuilder = PFTypes.Factory.DefineReferenceClass("ImmutableReference");
            immutableReferenceGenericTypeBuilder.MakeGenericParameters("TDeref", "TLife");
            immutableReferenceGenericTypeBuilder.AddTypeKeywordProviderAttribute(RebarTypeKeyword);
            ImmutableReferenceGenericType = immutableReferenceGenericTypeBuilder.CreateType();

            var polymorphicReferenceGenericTypeBuilder = PFTypes.Factory.DefineReferenceClass("PolymorphicReference");
            polymorphicReferenceGenericTypeBuilder.MakeGenericParameters("TDeref", "TLife", "TMut");
            polymorphicReferenceGenericTypeBuilder.AddTypeKeywordProviderAttribute(RebarTypeKeyword);
            PolymorphicReferenceGenericType = polymorphicReferenceGenericTypeBuilder.CreateType();

            var optionGenericTypeBuilder = PFTypes.Factory.DefineValueClass("Option");
            optionGenericTypeBuilder.MakeGenericParameters("T");
            optionGenericTypeBuilder.AddTypeKeywordProviderAttribute(RebarTypeKeyword);
            OptionGenericType = optionGenericTypeBuilder.CreateType();

            var lockingCellGenericTypeBuilder = PFTypes.Factory.DefineReferenceClass("LockingCell");
            lockingCellGenericTypeBuilder.MakeGenericParameters("T");
            lockingCellGenericTypeBuilder.AddTypeKeywordProviderAttribute(RebarTypeKeyword);
            LockingCellGenericType = lockingCellGenericTypeBuilder.CreateType();

            var nonLockingCellGenericTypeBuilder = PFTypes.Factory.DefineReferenceClass("NonLockingCell");
            nonLockingCellGenericTypeBuilder.MakeGenericParameters("T");
            nonLockingCellGenericTypeBuilder.AddTypeKeywordProviderAttribute(RebarTypeKeyword);
            NonLockingCellGenericType = nonLockingCellGenericTypeBuilder.CreateType();

            var iteratorGenericTypeBuilder = PFTypes.Factory.DefineReferenceClass("Iterator");
            iteratorGenericTypeBuilder.MakeGenericParameters("T");
            iteratorGenericTypeBuilder.AddTypeKeywordProviderAttribute(RebarTypeKeyword);
            IteratorGenericType = iteratorGenericTypeBuilder.CreateType();

            var stringSliceTypeBuilder = PFTypes.Factory.DefineValueClass("StringSlice");
            stringSliceTypeBuilder.AddTypeKeywordProviderAttribute(RebarTypeKeyword);
            StringSliceType = stringSliceTypeBuilder.CreateType();

            var vectorGenericTypeBuilder = PFTypes.Factory.DefineReferenceClass("Vector");
            vectorGenericTypeBuilder.MakeGenericParameters("T");
            vectorGenericTypeBuilder.AddTypeKeywordProviderAttribute("RustyWiresReferece");
            VectorGenericType = vectorGenericTypeBuilder.CreateType();
        }

        private static NIType SpecializeGenericType(NIType genericTypeDefinition, params NIType[] typeParameters)
        {
            var specializationTypeBuilder = genericTypeDefinition.DefineClassFromExisting();
            specializationTypeBuilder.ReplaceGenericParameters(typeParameters);
            return specializationTypeBuilder.CreateType();
        }

        private static bool IsGenericTypeSpecialization(this NIType type, NIType genericTypeDefinition)
        {
            return type.IsGenericType() && type.GetGenericTypeDefinition() == genericTypeDefinition;
        }

        public static NIType CreateMutableReference(this NIType dereferenceType, NIType lifetimeType = default(NIType))
        {
            return SpecializeGenericType(MutableReferenceGenericType, dereferenceType, lifetimeType);
        }

        public static bool IsMutableReferenceType(this NIType type)
        {
            return type.IsGenericTypeSpecialization(MutableReferenceGenericType);
        }

        public static NIType CreateImmutableReference(this NIType dereferenceType, NIType lifetimeType = default(NIType))
        {
            return SpecializeGenericType(ImmutableReferenceGenericType, dereferenceType, lifetimeType);
        }

        public static bool IsImmutableReferenceType(this NIType type)
        {
            return type.IsGenericTypeSpecialization(ImmutableReferenceGenericType);
        }

        public static bool IsRebarReferenceType(this NIType type)
        {
            return type.IsImmutableReferenceType() || type.IsMutableReferenceType() || type.IsPolymorphicReferenceType();
        }

        public static NIType GetTypeOrReferentType(this NIType type)
        {
            return type.IsRebarReferenceType()
                ? type.GetGenericParameters().ElementAt(0)
                : type;
        }

        public static NIType GetReferentType(this NIType type)
        {
            if (!type.IsRebarReferenceType())
            {
                throw new ArgumentException("Expected a reference type.", "type");
            }
            return type.GetGenericParameters().ElementAt(0);
        }

        public static NIType GetReferenceLifetimeType(this NIType type)
        {
            if (!type.IsRebarReferenceType())
            {
                throw new ArgumentException("Expected a reference type.", "type");
            }
            return type.GetGenericParameters().ElementAt(1);
        }

        public static NIType CreatePolymorphicReference(this NIType dereferenceType, NIType lifetimeType, NIType mutabilityType)
        {
            return SpecializeGenericType(PolymorphicReferenceGenericType, dereferenceType, lifetimeType, mutabilityType);
        }

        public static bool IsPolymorphicReferenceType(this NIType type)
        {
            return type.IsGenericTypeSpecialization(PolymorphicReferenceGenericType);
        }

        public static NIType GetReferenceMutabilityType(this NIType type)
        {
            if (!type.IsPolymorphicReferenceType())
            {
                throw new ArgumentException("Expected a polymorphic reference type.", "type");
            }
            return type.GetGenericParameters().ElementAt(2);
        }

        internal static InputReferenceMutability GetInputReferenceMutabilityFromType(this NIType type)
        {
            if (!type.IsRebarReferenceType())
            {
                throw new ArgumentException("Expected a reference type.", "type");
            }
            if (type.IsImmutableReferenceType())
            {
                return InputReferenceMutability.AllowImmutable;
            }
            if (type.IsMutableReferenceType())
            {
                return InputReferenceMutability.RequireMutable;
            }
            return InputReferenceMutability.Polymorphic;
        }

        public static bool IsReferenceToSameTypeAs(this NIType type, NIType other)
        {
            if (!type.IsRebarReferenceType() || !other.IsRebarReferenceType())
            {
                return false;
            }
            return type.GetReferentType() == other.GetReferentType();
        }

        public static NIType CreateOption(this NIType valueType)
        {
            return SpecializeGenericType(OptionGenericType, valueType);
        }

        public static bool IsOptionType(this NIType type)
        {
            return type.IsGenericTypeSpecialization(OptionGenericType);
        }

        /// <summary>
        /// If the given <see cref="type"/> is an Option type, outputs the inner value type and returns true; otherwise, returns false.
        /// </summary>
        /// <param name="type">The <see cref="NIType"/> to try to destructure as an Option type.</param>
        /// <param name="valueType">The inner value type of the given type if it is an Option.</param>
        /// <returns>True if the given type was an Option type; false otherwise.</returns>
        public static bool TryDestructureOptionType(this NIType type, out NIType valueType)
        {
            if (!IsOptionType(type))
            {
                valueType = NIType.Unset;
                return false;
            }
            valueType = type.GetGenericParameters().ElementAt(0);
            return true;
        }

        public static NIType CreateLockingCell(this NIType dereferenceType)
        {
            return SpecializeGenericType(LockingCellGenericType, dereferenceType);
        }

        public static bool IsLockingCellType(this NIType type)
        {
            return type.IsGenericTypeSpecialization(LockingCellGenericType);
        }

        public static NIType CreateNonLockingCell(this NIType dereferenceType)
        {
            return SpecializeGenericType(NonLockingCellGenericType, dereferenceType);
        }

        public static bool IsNonLockingCellType(this NIType type)
        {
            return type.IsGenericTypeSpecialization(NonLockingCellGenericType);
        }

        public static NIType GetUnderlyingTypeFromRebarType(this NIType rebarType)
        {
            if (rebarType.IsRebarReferenceType())
            {
                return rebarType.GetGenericParameters().ElementAt(0);
            }
            return rebarType;
        }

        public static NIType GetUnderlyingTypeFromLockingCellType(this NIType rebarType)
        {
            if (rebarType.IsLockingCellType())
            {
                return rebarType.GetGenericParameters().ElementAt(0);
            }
            throw new ArgumentException("Expected a LockingCell type.");
        }

        /// <summary>
        /// If the given <see cref="type"/> is a LockingCell type, outputs the inner value type and returns true; otherwise, returns false.
        /// </summary>
        /// <param name="type">The <see cref="NIType"/> to try to destructure as an LockingCell type.</param>
        /// <param name="valueType">The inner value type of the given type if it is an LockingCell.</param>
        /// <returns>True if the given type was an LockingCell type; false otherwise.</returns>
        public static bool TryDestructureLockingCellType(this NIType type, out NIType valueType)
        {
            if (!IsLockingCellType(type))
            {
                valueType = NIType.Unset;
                return false;
            }
            valueType = type.GetGenericParameters().ElementAt(0);
            return true;
        }

        public static NIType CreateIterator(this NIType itemType)
        {
            return SpecializeGenericType(IteratorGenericType, itemType);
        }

        public static bool IsIteratorType(this NIType type)
        {
            return type.IsGenericTypeSpecialization(IteratorGenericType);
        }

        /// <summary>
        /// If the given <see cref="type"/> is an Iterator type, outputs the inner value type and returns true; otherwise, returns false.
        /// </summary>
        /// <param name="type">The <see cref="NIType"/> to try to destructure as an Iterator type.</param>
        /// <param name="valueType">The inner value type of the given type if it is an Iterator.</param>
        /// <returns>True if the given type was an Iterator type; false otherwise.</returns>
        public static bool TryDestructureIteratorType(this NIType type, out NIType valueType)
        {
            if (!IsIteratorType(type))
            {
                valueType = NIType.Unset;
                return false;
            }
            valueType = type.GetGenericParameters().ElementAt(0);
            return true;
        }

        public static NIType CreateVector(this NIType itemType)
        {
            return SpecializeGenericType(VectorGenericType, itemType);
        }

        public static bool IsVectorType(this NIType type)
        {
            return type.IsGenericTypeSpecialization(VectorGenericType);
        }

        /// <summary>
        /// If the given <see cref="type"/> is a Vector type, outputs the inner value type and returns true; otherwise, returns false.
        /// </summary>
        /// <param name="type">The <see cref="NIType"/> to try to destructure as an Vector type.</param>
        /// <param name="itemType">The inner value type of the given type if it is an Vector.</param>
        /// <returns>True if the given type was an Vector type; false otherwise.</returns>
        public static bool TryDestructureVectorType(this NIType type, out NIType itemType)
        {
            if (!IsVectorType(type))
            {
                itemType = NIType.Unset;
                return false;
            }
            itemType = type.GetGenericParameters().ElementAt(0);
            return true;
        }

        internal static bool WireTypeMayFork(this NIType wireType)
        {
            if (wireType.IsImmutableReferenceType())
            {
                return true;
            }

            if (wireType.IsNumeric() || wireType.IsBoolean())
            {
                return true;
            }

            NIType optionValueType;
            if (wireType.TryDestructureOptionType(out optionValueType))
            {
                return WireTypeMayFork(optionValueType);
            }

            return false;
        }

        internal static bool IsSupportedIntegerType(this NIType type)
        {
            return type.IsInt32() || (type.IsInteger() && RebarFeatureToggles.IsAllIntegerTypesEnabled);
        }

        internal static bool TypeHasDisplayTrait(this NIType type)
        {
            if (type.IsString() || type == StringSliceType)
            {
                return RebarFeatureToggles.IsStringDataTypeEnabled;
            }
            return type.IsSupportedIntegerType() || type.IsBoolean();
        }

        internal static bool TypeHasDropTrait(this NIType type)
        {
            return type == PFTypes.String;
        }
    }
}
