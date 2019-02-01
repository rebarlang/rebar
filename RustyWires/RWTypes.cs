using System;
using System.Linq;
using NationalInstruments.DataTypes;

namespace RustyWires
{
    public static class RWTypes
    {
        private static NIType MutableReferenceGenericType { get; }

        private static NIType ImmutableReferenceGenericType { get; }

        private static NIType OptionGenericType { get; }

        private static NIType LockingCellGenericType { get; }

        private static NIType NonLockingCellGenericType { get; }

        private static NIType IteratorGenericType { get; }

        static RWTypes()
        {
            var mutableReferenceGenericTypeBuilder = PFTypes.Factory.DefineReferenceClass("MutableReference");
            mutableReferenceGenericTypeBuilder.MakeGenericParameters("TDeref"); // TODO: also need a lifetime type parameter
            mutableReferenceGenericTypeBuilder.AddTypeKeywordProviderAttribute("RustyWiresReference");
            MutableReferenceGenericType = mutableReferenceGenericTypeBuilder.CreateType();

            var immutableReferenceGenericTypeBuilder = PFTypes.Factory.DefineReferenceClass("ImmutableReference");
            immutableReferenceGenericTypeBuilder.MakeGenericParameters("TDeref");    // TODO: also need a lifetime type parameter
            immutableReferenceGenericTypeBuilder.AddTypeKeywordProviderAttribute("RustyWiresReference");
            ImmutableReferenceGenericType = immutableReferenceGenericTypeBuilder.CreateType();

            var optionGenericTypeBuilder = PFTypes.Factory.DefineValueClass("Option");
            optionGenericTypeBuilder.MakeGenericParameters("T");
            optionGenericTypeBuilder.AddTypeKeywordProviderAttribute("RustyWiresReference");
            OptionGenericType = optionGenericTypeBuilder.CreateType();

            var lockingCellGenericTypeBuilder = PFTypes.Factory.DefineReferenceClass("LockingCell");
            lockingCellGenericTypeBuilder.MakeGenericParameters("T");
            lockingCellGenericTypeBuilder.AddTypeKeywordProviderAttribute("RustyWiresReference");
            LockingCellGenericType = lockingCellGenericTypeBuilder.CreateType();

            var nonLockingCellGenericTypeBuilder = PFTypes.Factory.DefineReferenceClass("NonLockingCell");
            nonLockingCellGenericTypeBuilder.MakeGenericParameters("T");
            nonLockingCellGenericTypeBuilder.AddTypeKeywordProviderAttribute("RustyWiresReference");
            NonLockingCellGenericType = nonLockingCellGenericTypeBuilder.CreateType();

            var iteratorGenericTypeBuilder = PFTypes.Factory.DefineReferenceClass("Iterator");
            iteratorGenericTypeBuilder.MakeGenericParameters("T");
            iteratorGenericTypeBuilder.AddTypeKeywordProviderAttribute("RustyWiresReference");
            IteratorGenericType = iteratorGenericTypeBuilder.CreateType();
        }

        private static NIType SpecializeGenericType(NIType genericTypeDefinition, NIType typeParameter)
        {
            var specializationTypeBuilder = genericTypeDefinition.DefineClassFromExisting();
            specializationTypeBuilder.ReplaceGenericParameters(typeParameter);
            return specializationTypeBuilder.CreateType();
        }

        private static bool IsGenericTypeSpecialization(this NIType type, NIType genericTypeDefinition)
        {
            return type.IsGenericType() && type.GetGenericTypeDefinition() == genericTypeDefinition;
        }

        public static NIType CreateMutableReference(this NIType dereferenceType)
        {
            return SpecializeGenericType(MutableReferenceGenericType, dereferenceType);
        }

        public static bool IsMutableReferenceType(this NIType type)
        {
            return type.IsGenericTypeSpecialization(MutableReferenceGenericType);
        }

        public static NIType CreateImmutableReference(this NIType dereferenceType)
        {
            return SpecializeGenericType(ImmutableReferenceGenericType, dereferenceType);
        }

        public static bool IsImmutableReferenceType(this NIType type)
        {
            return type.IsGenericTypeSpecialization(ImmutableReferenceGenericType);
        }

        public static bool IsRWReferenceType(this NIType type)
        {
            return type.IsImmutableReferenceType() || type.IsMutableReferenceType();
        }

        public static NIType GetTypeOrReferentType(this NIType type)
        {
            return type.IsRWReferenceType()
                ? type.GetGenericParameters().ElementAt(0)
                : type;
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

        public static NIType GetUnderlyingTypeFromRustyWiresType(this NIType rustyWiresType)
        {
            if (rustyWiresType.IsImmutableReferenceType() ||
                rustyWiresType.IsMutableReferenceType())
            {
                return rustyWiresType.GetGenericParameters().ElementAt(0);
            }
            return rustyWiresType;
        }

        public static NIType GetUnderlyingTypeFromLockingCellType(this NIType rustyWiresType)
        {
            if (rustyWiresType.IsLockingCellType())
            {
                return rustyWiresType.GetGenericParameters().ElementAt(0);
            }
            throw new ArgumentException("Expected a LockingCell type.");
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

        internal static TypePermissiveness GetTypePermissiveness(this NIType type)
        {
            if (type.IsImmutableReferenceType())
            {
                return TypePermissiveness.ImmutableReference;
            }
            else if (type.IsMutableReferenceType())
            {
                return TypePermissiveness.MutableReference;
            }
            else
            {
                return TypePermissiveness.Owner;
            }
        }

        public static NIType PromoteTypePermissivenessToMatch(this NIType typeToPromote, NIType typeToMatch)
        {
            TypePermissiveness toPromotePermissiveness = typeToPromote.GetTypePermissiveness(),
                toMatchPermissiveness = typeToMatch.GetTypePermissiveness();
            if (toPromotePermissiveness >= toMatchPermissiveness)
            {
                return typeToPromote;
            }
            else if (toMatchPermissiveness == TypePermissiveness.Owner)
            {
                return typeToPromote.GetGenericParameters().First();
            }
            else // toMatchPermissiveness == TypePermissiveness.MutableReference
            {
                return typeToPromote.GetGenericParameters().First().CreateMutableReference();
            }
        }

        internal static Compiler.Nodes.BorrowMode GetBorrowMode(TypePermissiveness borrowFrom, TypePermissiveness borrowTo)
        {
            if (borrowFrom == TypePermissiveness.Owner)
            {
                if (borrowTo == TypePermissiveness.MutableReference)
                {
                    return Compiler.Nodes.BorrowMode.OwnerToMutable;
                }

                if (borrowTo == TypePermissiveness.ImmutableReference)
                {
                    return Compiler.Nodes.BorrowMode.OwnerToImmutable;
                }
            }
            else if (borrowFrom == TypePermissiveness.MutableReference &&
                     borrowTo == TypePermissiveness.ImmutableReference)
            {
                return Compiler.Nodes.BorrowMode.MutableToImmutable;
            }
            throw new InvalidOperationException($"Borrowing {borrowTo} from {borrowFrom} is not necessary.");
        }
    }

    internal enum TypePermissiveness
    {
        ImmutableReference,
        MutableReference,
        Owner,
        MutableOwner
    }
}
