using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments;
using NationalInstruments.DataTypes;
using Rebar.Common;

namespace Tests.Rebar.Unit
{
    [TestClass]
    public class TypeInferenceUnitTests
    {
        [TestMethod]
        public void LiteralTypeAndTypeVariable_Unify_BothBecomeLiteralType()
        {
            TypeVariableSet typeVariableSet = new TypeVariableSet();
            TypeVariableReference literalReference = typeVariableSet.CreateTypeVariableReferenceFromNIType(PFTypes.Int32);
            TypeVariableReference typeVariable = typeVariableSet.CreateReferenceToNewTypeVariable();

            typeVariableSet.Unify(typeVariable, literalReference, new TestTypeUnificationResult());

            Assert.IsTrue(literalReference.RenderNIType().IsInt32());
            Assert.IsTrue(typeVariable.RenderNIType().IsInt32());
        }

        [TestMethod]
        public void TwoTypeVariables_Unify_BothBecomeSingleTypeVariable()
        {
            TypeVariableSet typeVariableSet = new TypeVariableSet();
            TypeVariableReference literalReference = typeVariableSet.CreateTypeVariableReferenceFromNIType(PFTypes.Int32);
            TypeVariableReference typeVariable1 = typeVariableSet.CreateReferenceToNewTypeVariable(),
                typeVariable2 = typeVariableSet.CreateReferenceToNewTypeVariable();

            typeVariableSet.Unify(typeVariable2, typeVariable1, new TestTypeUnificationResult());
            typeVariableSet.Unify(typeVariable1, literalReference, new TestTypeUnificationResult());

            Assert.IsTrue(typeVariable1.RenderNIType().IsInt32());
            Assert.IsTrue(typeVariable2.RenderNIType().IsInt32());
        }

        [TestMethod]
        public void TwoDifferentLiteralTypes_Unify_TypeMismatchReported()
        {
            TypeVariableSet typeVariableSet = new TypeVariableSet();
            TypeVariableReference literalReference1 = typeVariableSet.CreateTypeVariableReferenceFromNIType(PFTypes.Int32),
                literalReference2 = typeVariableSet.CreateTypeVariableReferenceFromNIType(PFTypes.Boolean);
            var testTypeUnificationResult = new TestTypeUnificationResult();

            typeVariableSet.Unify(literalReference2, literalReference1, testTypeUnificationResult);

            Assert.IsTrue(testTypeUnificationResult.TypeMismatch);
        }

        [TestMethod]
        public void LiteralTypeAndConstructorType_Unify_TypeMismatchReported()
        {
            TypeVariableSet typeVariableSet = new TypeVariableSet();
            TypeVariableReference literalReference = typeVariableSet.CreateTypeVariableReferenceFromNIType(PFTypes.Int32),
                constructorReference = typeVariableSet.CreateTypeVariableReferenceFromNIType(PFTypes.Int32.CreateVector());
            var testTypeUnificationResult = new TestTypeUnificationResult();

            typeVariableSet.Unify(constructorReference, literalReference, testTypeUnificationResult);

            Assert.IsTrue(testTypeUnificationResult.TypeMismatch);
        }

        #region Constructor Types

        [TestMethod]
        public void TwoConstructorTypesWithSameConstructorName_Unify_InnerTypesAreUnified()
        {
            TypeVariableSet typeVariableSet = new TypeVariableSet();
            TypeVariableReference innerTypeVariable = typeVariableSet.CreateReferenceToNewTypeVariable();
            TypeVariableReference constructorType1 = typeVariableSet.CreateReferenceToOptionType(innerTypeVariable);
            TypeVariableReference constructorType2 = typeVariableSet.CreateReferenceToOptionType(
                typeVariableSet.CreateTypeVariableReferenceFromNIType(PFTypes.Int32));

            typeVariableSet.Unify(constructorType1, constructorType2, new TestTypeUnificationResult());

            Assert.IsTrue(innerTypeVariable.RenderNIType().IsInt32());
        }

        [TestMethod]
        public void TwoConstructorTypesWithSameConstructorNameAndDifferentInnerTypes_Unify_TypeMismatchReported()
        {
            TypeVariableSet typeVariableSet = new TypeVariableSet();
            TypeVariableReference constructorType1 = typeVariableSet.CreateReferenceToOptionType(
                typeVariableSet.CreateTypeVariableReferenceFromNIType(PFTypes.Int32));
            TypeVariableReference constructorType2 = typeVariableSet.CreateReferenceToOptionType(
                typeVariableSet.CreateTypeVariableReferenceFromNIType(PFTypes.Boolean));
            var typeUnificationResult = new TestTypeUnificationResult();

            typeVariableSet.Unify(constructorType1, constructorType2, typeUnificationResult);

            Assert.IsTrue(typeUnificationResult.TypeMismatch);
        }

        [TestMethod]
        public void TwoConstructorTypesWithDifferentConstructorNames_Unify_TypeMismatchReported()
        {
            TypeVariableSet typeVariableSet = new TypeVariableSet();
            TypeVariableReference constructorType1 = typeVariableSet.CreateReferenceToOptionType(
                typeVariableSet.CreateTypeVariableReferenceFromNIType(PFTypes.Int32));
            TypeVariableReference constructorType2 = typeVariableSet.CreateReferenceToLockingCellType(
                typeVariableSet.CreateTypeVariableReferenceFromNIType(PFTypes.Int32));
            var typeUnificationResult = new TestTypeUnificationResult();

            typeVariableSet.Unify(constructorType1, constructorType2, typeUnificationResult);

            Assert.IsTrue(typeUnificationResult.TypeMismatch);
        }

        #endregion

        #region Type Constraints

        [TestMethod]
        public void TypeVariableWithCopyConstraintAndNonCopyableType_Unify_FailedConstraintReported()
        {
            TypeVariableSet typeVariableSet = new TypeVariableSet();
            TypeVariableReference literalReference = typeVariableSet.CreateReferenceToReferenceType(
                true,
                typeVariableSet.CreateTypeVariableReferenceFromNIType(PFTypes.Int32),
                typeVariableSet.CreateReferenceToLifetimeType(Lifetime.Static));
            var constraint = new SimpleTraitConstraint("Copy");
            TypeVariableReference typeVariable = typeVariableSet.CreateReferenceToNewTypeVariable(constraint.ToEnumerable());
            var testTypeUnificationResult = new TestTypeUnificationResult();

            typeVariableSet.Unify(typeVariable, literalReference, testTypeUnificationResult);

            Assert.IsTrue(testTypeUnificationResult.FailedConstraints.Contains(constraint));
        }

        #endregion

        [TestMethod]
        public void CreateTypeVariableReferenceFromIntegerType_TypeVariableReferenceHasExpectedTraits()
        {
            var typeVariableSet = new TypeVariableSet();
            TypeVariableReference integerType = typeVariableSet.CreateTypeVariableReferenceFromNIType(PFTypes.Int32);

            AssertTypeVariableReferenceHasParameterlessTrait(typeVariableSet, integerType, "Display");
            AssertTypeVariableReferenceHasParameterlessTrait(typeVariableSet, integerType, "Clone");
            AssertTypeVariableReferenceHasParameterlessTrait(typeVariableSet, integerType, "Copy");
        }

        [TestMethod]
        public void CreateTypeVariableReferenceFromBooleanType_TypeVariableReferenceHasExpectedTraits()
        {
            var typeVariableSet = new TypeVariableSet();
            TypeVariableReference booleanType = typeVariableSet.CreateTypeVariableReferenceFromNIType(PFTypes.Boolean);

            AssertTypeVariableReferenceHasParameterlessTrait(typeVariableSet, booleanType, "Display");
            AssertTypeVariableReferenceHasParameterlessTrait(typeVariableSet, booleanType, "Clone");
            AssertTypeVariableReferenceHasParameterlessTrait(typeVariableSet, booleanType, "Copy");
        }

        [TestMethod]
        public void CreateTypeVariableReferenceFromStringType_TypeVariableReferenceHasExpectedTraits()
        {
            var typeVariableSet = new TypeVariableSet();
            TypeVariableReference stringType = typeVariableSet.CreateTypeVariableReferenceFromNIType(PFTypes.String);

            AssertTypeVariableReferenceHasParameterlessTrait(typeVariableSet, stringType, "Display");
            AssertTypeVariableReferenceHasParameterlessTrait(typeVariableSet, stringType, "Clone");
        }

        [TestMethod]
        public void CreateTypeVariableReferenceFromStringSliceType_TypeVariableReferenceHasExpectedTraits()
        {
            var typeVariableSet = new TypeVariableSet();
            TypeVariableReference stringSliceType = typeVariableSet.CreateTypeVariableReferenceFromNIType(DataTypes.StringSliceType);

            AssertTypeVariableReferenceHasParameterlessTrait(typeVariableSet, stringSliceType, "Display");
        }

        [TestMethod]
        public void CreateTypeVariableReferenceFromRangeIteratorType_TypeVariableReferenceHasExpectedIteratorTrait()
        {
            var typeVariableSet = new TypeVariableSet();
            TypeVariableReference rangeIteratorType = typeVariableSet.CreateTypeVariableReferenceFromNIType(DataTypes.RangeIteratorType);

            TypeVariableReference iteratorTraitType;
            Assert.IsTrue(typeVariableSet.TryGetImplementedTrait(rangeIteratorType, "Iterator", out iteratorTraitType));
            Assert.IsTrue(typeVariableSet.GetTypeParameters(iteratorTraitType).First().RenderNIType().IsInt32());
        }

        [TestMethod]
        public void CreateTypeVariableReferenceFromOptionOfCopyType_TypeVariableReferenceHasCopyTrait()
        {
            var typeVariableSet = new TypeVariableSet();
            TypeVariableReference optionType = typeVariableSet.CreateTypeVariableReferenceFromNIType(PFTypes.Int32.CreateOption());

            AssertTypeVariableReferenceHasParameterlessTrait(typeVariableSet, optionType, "Copy");
        }

        [TestMethod]
        public void CreateTypeVariableReferenceFromOptionOfNonCopyType_TypeVariableReferenceDoesNotHaveCopyTrait()
        {
            var typeVariableSet = new TypeVariableSet();
            TypeVariableReference optionType = typeVariableSet.CreateTypeVariableReferenceFromNIType(PFTypes.String.CreateOption());

            AssertTypeVariableReferenceDoesNotHaveParameterlessTrait(typeVariableSet, optionType, "Copy");
        }

        [TestMethod]
        public void CreateTypeVariableReferenceFromSharedType_TypeVariableHasCloneAndDropTraits()
        {
            var typeVariableSet = new TypeVariableSet();
            TypeVariableReference sharedType = typeVariableSet.CreateTypeVariableReferenceFromNIType(PFTypes.String.CreateShared());

            AssertTypeVariableReferenceHasParameterlessTrait(typeVariableSet, sharedType, "Clone");
            AssertTypeVariableReferenceHasParameterlessTrait(typeVariableSet, sharedType, "Drop");
        }

        [TestMethod]
        public void CreateTypeVariableReferenceFromVectorOfCloneType_TypeVariableHasCloneTrait()
        {
            var typeVariableSet = new TypeVariableSet();
            TypeVariableReference vectorType = typeVariableSet.CreateTypeVariableReferenceFromNIType(PFTypes.String.CreateVector());

            AssertTypeVariableReferenceHasParameterlessTrait(typeVariableSet, vectorType, "Clone");
        }

        [TestMethod]
        public void CreateTypeVariableReferenceFromVectorOfCopyType_TypeVariableHasCloneTrait()
        {
            var typeVariableSet = new TypeVariableSet();
            TypeVariableReference vectorType = typeVariableSet.CreateTypeVariableReferenceFromNIType(PFTypes.Int32.CreateVector());

            AssertTypeVariableReferenceHasParameterlessTrait(typeVariableSet, vectorType, "Clone");
        }

        [TestMethod]
        public void CreateTypeVariableReferenceFromVectorOfNonCopyNonCloneType_TypeVariableDoesNotHaveCloneTrait()
        {
            var typeVariableSet = new TypeVariableSet();
            TypeVariableReference vectorType = typeVariableSet.CreateTypeVariableReferenceFromNIType(DataTypes.FileHandleType.CreateVector());

            AssertTypeVariableReferenceDoesNotHaveParameterlessTrait(typeVariableSet, vectorType, "Clone");
        }

        private void AssertTypeVariableReferenceHasParameterlessTrait(TypeVariableSet typeVariableSet, TypeVariableReference typeVariableReference, string traitName)
        {
            TypeVariableReference trait;
            Assert.IsTrue(typeVariableSet.TryGetImplementedTrait(typeVariableReference, traitName, out trait), "Failed to find expected trait: " + traitName);
        }

        private void AssertTypeVariableReferenceDoesNotHaveParameterlessTrait(TypeVariableSet typeVariableSet, TypeVariableReference typeVariableReference, string traitName)
        {
            TypeVariableReference trait;
            Assert.IsFalse(typeVariableSet.TryGetImplementedTrait(typeVariableReference, traitName, out trait), "Expected not to find trait: " + traitName);
        }
    }

    internal class TestTypeUnificationResult : ITypeUnificationResult
    {
        void ITypeUnificationResult.SetExpectedMutable()
        {
        }

        void ITypeUnificationResult.SetTypeMismatch()
        {
            TypeMismatch = true;
        }

        void ITypeUnificationResult.AddFailedTypeConstraint(Constraint constraint)
        {
            FailedConstraints.Add(constraint);
        }

        public bool TypeMismatch { get; private set; }

        public List<Constraint> FailedConstraints { get; } = new List<Constraint>();
    }
}
