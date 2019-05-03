using System;
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
            TypeVariableReference literalReference = typeVariableSet.CreateReferenceToLiteralType(PFTypes.Int32);
            TypeVariableReference typeVariable = typeVariableSet.CreateReferenceToNewTypeVariable();

            typeVariableSet.Unify(typeVariable, literalReference, new TestTypeUnificationResult());

            Assert.IsTrue(literalReference.RenderNIType().IsInt32());
            Assert.IsTrue(typeVariable.RenderNIType().IsInt32());
        }

        [TestMethod]
        public void TwoTypeVariables_Unify_BothBecomeSingleTypeVariable()
        {
            TypeVariableSet typeVariableSet = new TypeVariableSet();
            TypeVariableReference literalReference = typeVariableSet.CreateReferenceToLiteralType(PFTypes.Int32);
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
            TypeVariableReference literalReference1 = typeVariableSet.CreateReferenceToLiteralType(PFTypes.Int32),
                literalReference2 = typeVariableSet.CreateReferenceToLiteralType(PFTypes.Boolean);
            var testTypeUnificationResult = new TestTypeUnificationResult();

            typeVariableSet.Unify(literalReference2, literalReference1, testTypeUnificationResult);

            Assert.IsTrue(testTypeUnificationResult.TypeMismatch);
        }

        [TestMethod]
        public void LiteralTypeAndConstructorType_Unify_TypeMismatchReported()
        {
            TypeVariableSet typeVariableSet = new TypeVariableSet();
            TypeVariableReference literalReference = typeVariableSet.CreateReferenceToLiteralType(PFTypes.Int32),
                constructorReference = typeVariableSet.CreateReferenceToConstructorType("Vector", literalReference);
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
            TypeVariableReference constructorType1 = typeVariableSet.CreateReferenceToConstructorType("Vector",
                innerTypeVariable);
            TypeVariableReference constructorType2 = typeVariableSet.CreateReferenceToConstructorType("Vector",
                typeVariableSet.CreateReferenceToLiteralType(PFTypes.Int32));

            typeVariableSet.Unify(constructorType1, constructorType2, new TestTypeUnificationResult());

            Assert.IsTrue(innerTypeVariable.RenderNIType().IsInt32());
        }

        [TestMethod]
        public void TwoConstructorTypesWithSameConstructorNameAndDifferentInnerTypes_Unify_TypeMismatchReported()
        {
            TypeVariableSet typeVariableSet = new TypeVariableSet();
            TypeVariableReference constructorType1 = typeVariableSet.CreateReferenceToConstructorType("Vector",
                typeVariableSet.CreateReferenceToLiteralType(PFTypes.Int32));
            TypeVariableReference constructorType2 = typeVariableSet.CreateReferenceToConstructorType("Vector",
                typeVariableSet.CreateReferenceToLiteralType(PFTypes.Boolean));
            var typeUnificationResult = new TestTypeUnificationResult();

            typeVariableSet.Unify(constructorType1, constructorType2, typeUnificationResult);

            Assert.IsTrue(typeUnificationResult.TypeMismatch);
        }

        [TestMethod]
        public void TwoConstructorTypesWithDifferentConstructorNames_Unify_TypeMismatchReported()
        {
            TypeVariableSet typeVariableSet = new TypeVariableSet();
            TypeVariableReference constructorType1 = typeVariableSet.CreateReferenceToConstructorType("Vector",
                typeVariableSet.CreateReferenceToLiteralType(PFTypes.Int32));
            TypeVariableReference constructorType2 = typeVariableSet.CreateReferenceToConstructorType("Option",
                typeVariableSet.CreateReferenceToLiteralType(PFTypes.Int32));
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
                typeVariableSet.CreateReferenceToLiteralType(PFTypes.Int32),
                typeVariableSet.CreateReferenceToLifetimeType(Lifetime.Static));
            var constraint = new CopyConstraint();
            TypeVariableReference typeVariable = typeVariableSet.CreateReferenceToNewTypeVariable(constraint.ToEnumerable());
            var testTypeUnificationResult = new TestTypeUnificationResult();

            typeVariableSet.Unify(typeVariable, literalReference, testTypeUnificationResult);

            Assert.IsTrue(testTypeUnificationResult.FailedConstraints.Contains(constraint));
        }

        #endregion
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
