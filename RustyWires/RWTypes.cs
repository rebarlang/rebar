using System;
using System.Linq;
using NationalInstruments.DataTypes;
using RustyWires.Compiler;

namespace RustyWires
{
    public static class RWTypes
    {
        private static NIType MutableReferenceGenericType { get; }

        private static NIType ImmutableReferenceGenericType { get; }

        private static NIType MutableValueGenericType { get; }

        private static NIType ImmutableValueGenericType { get; }

        private static NIType LockingCellGenericType { get; }

        private static NIType NonLockingCellGenericType { get; }

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

            var mutableValueGenericTypeBuilder = PFTypes.Factory.DefineReferenceClass("MutableValue");
            mutableValueGenericTypeBuilder.MakeGenericParameters("T");
            mutableValueGenericTypeBuilder.AddTypeKeywordProviderAttribute("RustyWiresReference");
            MutableValueGenericType = mutableValueGenericTypeBuilder.CreateType();

            var immutableValueGenericTypeBuilder = PFTypes.Factory.DefineReferenceClass("ImmutableValue");
            immutableValueGenericTypeBuilder.MakeGenericParameters("T");
            immutableValueGenericTypeBuilder.AddTypeKeywordProviderAttribute("RustyWiresReference");
            ImmutableValueGenericType = immutableValueGenericTypeBuilder.CreateType();

            var lockingCellGenericTypeBuilder = PFTypes.Factory.DefineReferenceClass("LockingCell");
            lockingCellGenericTypeBuilder.MakeGenericParameters("T");
            lockingCellGenericTypeBuilder.AddTypeKeywordProviderAttribute("RustyWiresReference");
            LockingCellGenericType = lockingCellGenericTypeBuilder.CreateType();

            var nonLockingCellGenericTypeBuilder = PFTypes.Factory.DefineReferenceClass("NonLockingCell");
            nonLockingCellGenericTypeBuilder.MakeGenericParameters("T");
            nonLockingCellGenericTypeBuilder.AddTypeKeywordProviderAttribute("RustyWiresReference");
            NonLockingCellGenericType = nonLockingCellGenericTypeBuilder.CreateType();
        }

        public static NIType CreateMutableReference(this NIType dereferenceType)
        {
            var mutableReferenceTypeBuilder = MutableReferenceGenericType.DefineClassFromExisting();
            mutableReferenceTypeBuilder.ReplaceGenericParameters(dereferenceType);
            return mutableReferenceTypeBuilder.CreateType();
        }

        public static bool IsMutableReferenceType(this NIType type)
        {
            return type.IsGenericType() && type.GetGenericTypeDefinition() == MutableReferenceGenericType;
        }

        public static NIType CreateImmutableReference(this NIType dereferenceType)
        {
            var immutableReferenceTypeBuilder = ImmutableReferenceGenericType.DefineClassFromExisting();
            immutableReferenceTypeBuilder.ReplaceGenericParameters(dereferenceType);
            return immutableReferenceTypeBuilder.CreateType();
        }

        public static bool IsImmutableReferenceType(this NIType type)
        {
            return type.IsGenericType() && type.GetGenericTypeDefinition() == ImmutableReferenceGenericType;
        }

        public static bool IsRWReferenceType(this NIType type)
        {
            return type.IsImmutableReferenceType() || type.IsMutableReferenceType();
        }

        public static NIType CreateMutableValue(this NIType valueType)
        {
            var mutableValueTypeBuilder = MutableValueGenericType.DefineClassFromExisting();
            mutableValueTypeBuilder.ReplaceGenericParameters(valueType);
            return mutableValueTypeBuilder.CreateType();
        }

        public static bool IsMutableValueType(this NIType type)
        {
            return type.IsGenericType() && type.GetGenericTypeDefinition() == MutableValueGenericType;
        }

        public static NIType CreateImmutableValue(this NIType valueType)
        {
            var immutableValueTypeBuilder = ImmutableValueGenericType.DefineClassFromExisting();
            immutableValueTypeBuilder.ReplaceGenericParameters(valueType);
            return immutableValueTypeBuilder.CreateType();
        }

        public static bool IsImmutableValueType(this NIType type)
        {
            return type.IsGenericType() && type.GetGenericTypeDefinition() == ImmutableValueGenericType;
        }

        public static NIType CreateLockingCell(this NIType dereferenceType)
        {
            var lockingCellTypeBuilder = LockingCellGenericType.DefineClassFromExisting();
            lockingCellTypeBuilder.ReplaceGenericParameters(dereferenceType);
            return lockingCellTypeBuilder.CreateType();
        }

        public static bool IsLockingCellType(this NIType type)
        {
            return type.IsGenericType() && type.GetGenericTypeDefinition() == LockingCellGenericType;
        }

        public static NIType CreateNonLockingCell(this NIType dereferenceType)
        {
            var nonLockingCellTypeBuilder = NonLockingCellGenericType.DefineClassFromExisting();
            nonLockingCellTypeBuilder.ReplaceGenericParameters(dereferenceType);
            return nonLockingCellTypeBuilder.CreateType();
        }

        public static bool IsNonLockingCellType(this NIType type)
        {
            return type.IsGenericType() && type.GetGenericTypeDefinition() == NonLockingCellGenericType;
        }

        public static NIType GetUnderlyingTypeFromRustyWiresType(this NIType rustyWiresType)
        {
            if (rustyWiresType.IsImmutableReferenceType() ||
                rustyWiresType.IsMutableReferenceType() ||
                rustyWiresType.IsImmutableValueType() ||
                rustyWiresType.IsMutableValueType())
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
            else if (type.IsMutableValueType())
            {
                return TypePermissiveness.MutableOwner;
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

        internal static BorrowMode GetBorrowMode(TypePermissiveness borrowFrom, TypePermissiveness borrowTo)
        {
            if (borrowFrom == TypePermissiveness.Owner)
            {
                if (borrowTo == TypePermissiveness.MutableReference)
                {
                    return BorrowMode.OwnerToMutable;
                }

                if (borrowTo == TypePermissiveness.ImmutableReference)
                {
                    return BorrowMode.OwnerToImmutable;
                }
            }
            else if (borrowFrom == TypePermissiveness.MutableReference &&
                     borrowTo == TypePermissiveness.ImmutableReference)
            {
                return BorrowMode.MutableToImmutable;
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
