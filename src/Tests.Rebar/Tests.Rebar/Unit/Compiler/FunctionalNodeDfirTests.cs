using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler;
using Rebar.Compiler.Nodes;
using Signatures = Rebar.Common.Signatures;

namespace Tests.Rebar.Unit.Compiler
{
    [TestClass]
    public class FunctionalNodeDfirTests : CompilerTestBase
    {
        #region Creation

        [TestMethod]
        public void FunctionNodeWithInOutSignatureParameter_Create_HasInputAndOutputTerminal()
        {
            NIType signatureType = Signatures.ImmutablePassthroughType;
            DfirRoot dfirRoot = DfirRoot.Create();

            FunctionalNode functionalNode = new FunctionalNode(dfirRoot.BlockDiagram, signatureType);

            Assert.AreEqual(2, functionalNode.Terminals.Count());
            Assert.AreEqual(Direction.Input, functionalNode.Terminals[0].Direction);
            Assert.AreEqual(Direction.Output, functionalNode.Terminals[1].Direction);
        }

        #endregion

        #region CreateNodeFacades

        [TestMethod]
        public void FunctionNodeWithImmutableInOutSignatureParameter_CreateNodeFacades_CreatesTrueAndFacadeVariablesForBothTerminals()
        {
            NIType signatureType = Signatures.ImmutablePassthroughType;
            DfirRoot dfirRoot = DfirRoot.Create();
            FunctionalNode functionalNode = new FunctionalNode(dfirRoot.BlockDiagram, signatureType);

            RunSemanticAnalysisUpToCreateNodeFacades(dfirRoot);

            AutoBorrowNodeFacade nodeFacade = AutoBorrowNodeFacade.GetNodeFacade(functionalNode);
            Terminal inputTerminal = functionalNode.InputTerminals[0];
            Assert.IsNotNull(nodeFacade[inputTerminal]);
            Terminal outputTerminal = functionalNode.OutputTerminals[0];
            Assert.IsNotNull(nodeFacade[outputTerminal]);
        }

        [TestMethod]
        public void FunctionNodeWithMutableInOutSignatureParameter_CreateNodeFacades_CreatesTrueAndFacadeVariablesForBothTerminals()
        {
            NIType signatureType = Signatures.MutablePassthroughType;
            DfirRoot dfirRoot = DfirRoot.Create();
            FunctionalNode functionalNode = new FunctionalNode(dfirRoot.BlockDiagram, signatureType);

            RunSemanticAnalysisUpToCreateNodeFacades(dfirRoot);

            AutoBorrowNodeFacade nodeFacade = AutoBorrowNodeFacade.GetNodeFacade(functionalNode);
            Terminal inputTerminal = functionalNode.InputTerminals[0];
            Assert.IsNotNull(nodeFacade[inputTerminal]);
            Terminal outputTerminal = functionalNode.OutputTerminals[0];
            Assert.IsNotNull(nodeFacade[outputTerminal]);
        }

        [TestMethod]
        public void FunctionNodeWithOutSignatureParameter_CreateNodeFacades_CreatesSimpleFacadeForTerminal()
        {
            NIType signatureType = Signatures.CreateCopyType;
            DfirRoot dfirRoot = DfirRoot.Create();
            FunctionalNode functionalNode = new FunctionalNode(dfirRoot.BlockDiagram, signatureType);

            RunSemanticAnalysisUpToCreateNodeFacades(dfirRoot);

            AutoBorrowNodeFacade nodeFacade = AutoBorrowNodeFacade.GetNodeFacade(functionalNode);
            Terminal outputTerminal = functionalNode.OutputTerminals[1];
            Assert.IsInstanceOfType(nodeFacade[outputTerminal], typeof(SimpleTerminalFacade));
        }

        [TestMethod]
        public void FunctionNodeWithNonReferenceInSignatureParameter_CreateNodeFacades_CreatesSimpleFacadeForTerminal()
        {
            NIType signatureType = Signatures.VectorInsertType;
            DfirRoot dfirRoot = DfirRoot.Create();
            FunctionalNode functionalNode = new FunctionalNode(dfirRoot.BlockDiagram, signatureType);

            RunSemanticAnalysisUpToCreateNodeFacades(dfirRoot);

            AutoBorrowNodeFacade nodeFacade = AutoBorrowNodeFacade.GetNodeFacade(functionalNode);
            Terminal inputTerminal = functionalNode.InputTerminals[2];
            Assert.IsInstanceOfType(nodeFacade[inputTerminal], typeof(SimpleTerminalFacade));
        }

        [TestMethod]
        public void FunctionNodeWithSelectReferenceSignature_CreateNodeFacades_CreatesFacades()
        {
            NIType signatureType = Signatures.SelectReferenceType;
            DfirRoot dfirRoot = DfirRoot.Create();
            FunctionalNode functionalNode = new FunctionalNode(dfirRoot.BlockDiagram, signatureType);

            RunSemanticAnalysisUpToCreateNodeFacades(dfirRoot);

            AutoBorrowNodeFacade nodeFacade = AutoBorrowNodeFacade.GetNodeFacade(functionalNode);
            Terminal inputTerminal = functionalNode.InputTerminals[1];
            Assert.IsNotInstanceOfType(nodeFacade[inputTerminal], typeof(SimpleTerminalFacade));
        }

        #endregion

        #region SetVariableTypes

        [TestMethod]
        public void FunctionNodeWithImmutableInOutParameter_SetVariableTypes_TrueVariableLifetimeInterruptsCorrectTypes()
        {
            NIType signatureType = Signatures.ImmutablePassthroughType;
            DfirRoot dfirRoot = DfirRoot.Create();
            FunctionalNode functionalNode = new FunctionalNode(dfirRoot.BlockDiagram, signatureType);
            ConnectConstantToInputTerminal(functionalNode.InputTerminals[0], PFTypes.Int32, false);
            var lifetimeVariableAssociation = new LifetimeVariableAssociation();

            RunSemanticAnalysisUpToSetVariableTypes(dfirRoot, null, null, lifetimeVariableAssociation);

            Lifetime lifetime = functionalNode.InputTerminals[0].GetTrueVariable().Lifetime;
            Assert.IsTrue(lifetime.IsBounded);
            VariableReference inputVariable = functionalNode.InputTerminals[0].GetFacadeVariable();
            IEnumerable<VariableReference> interruptedVariables = lifetimeVariableAssociation.GetVariablesInterruptedByLifetime(lifetime);
            Assert.AreEqual(1, interruptedVariables.Count());
            Assert.AreEqual(inputVariable, interruptedVariables.First());
        }

        [TestMethod]
        public void FunctionNodeWithOutParameterAndInOutParameterLinkedByType_SetVariableTypes_TypePropagatedToOutput()
        {
            NIType signatureType = Signatures.CreateCopyType;
            DfirRoot dfirRoot = DfirRoot.Create();
            FunctionalNode functionalNode = new FunctionalNode(dfirRoot.BlockDiagram, signatureType);
            ConnectConstantToInputTerminal(functionalNode.InputTerminals[0], PFTypes.Int32, false);

            RunSemanticAnalysisUpToSetVariableTypes(dfirRoot);

            Terminal outputTerminal = functionalNode.OutputTerminals[1];
            Assert.IsTrue(outputTerminal.GetTrueVariable().Type.IsInt32());
        }

        [TestMethod]
        public void FunctionNodeWithNonGenericOutParameter_SetVariableTypes_TypeSetOnOutput()
        {
            NIType signatureType = Signatures.DefinePureUnaryFunction("unary", PFTypes.Int32, PFTypes.Int32);
            DfirRoot dfirRoot = DfirRoot.Create();
            FunctionalNode functionalNode = new FunctionalNode(dfirRoot.BlockDiagram, signatureType);

            RunSemanticAnalysisUpToSetVariableTypes(dfirRoot);

            Terminal outputTerminal = functionalNode.OutputTerminals[1];
            Assert.IsTrue(outputTerminal.GetTrueVariable().Type.IsInt32());
        }

        [TestMethod]
        public void FunctionNodeWithGenericOutParameterWithDownstreamTypeDeterminant_SetVariableTypes_TypeSetOnOutput()
        {
            DfirRoot function = DfirRoot.Create();
            var genericOutput = new FunctionalNode(function.BlockDiagram, DefineGenericOutputFunctionSignature());
            var assignNode = new FunctionalNode(function.BlockDiagram, Signatures.AssignType);
            genericOutput.OutputTerminals[0].WireTogether(assignNode.InputTerminals[0], SourceModelIdSource.NoSourceModelId);
            ConnectConstantToInputTerminal(assignNode.InputTerminals[1], PFTypes.Int32, false);

            RunSemanticAnalysisUpToSetVariableTypes(function);

            Assert.IsTrue(genericOutput.OutputTerminals[0].GetTrueVariable().Type.IsInt32());
        }

        [TestMethod]
        public void FunctionNodeWithSelectReferenceSignatureAndImmutableValuesWired_SetVariableTypes_ImmutableReferenceTypeSetOnOutput()
        {
            NIType signatureType = Signatures.SelectReferenceType;
            DfirRoot dfirRoot = DfirRoot.Create();
            FunctionalNode functionalNode = new FunctionalNode(dfirRoot.BlockDiagram, signatureType);
            ConnectConstantToInputTerminal(functionalNode.InputTerminals[1], PFTypes.Int32, false);
            ConnectConstantToInputTerminal(functionalNode.InputTerminals[2], PFTypes.Int32, false);

            RunSemanticAnalysisUpToSetVariableTypes(dfirRoot);

            AutoBorrowNodeFacade nodeFacade = AutoBorrowNodeFacade.GetNodeFacade(functionalNode);
            Terminal outputTerminal = functionalNode.OutputTerminals[1];
            VariableReference outputTerminalVariable = outputTerminal.GetTrueVariable();
            Assert.IsTrue(outputTerminalVariable.Type.IsImmutableReferenceType());
            Assert.IsTrue(outputTerminalVariable.Type.GetReferentType().IsInt32());
        }

        [TestMethod]
        public void FunctionNodeWithSelectReferenceSignatureAndSameLifetimeImmutableReferencesWired_SetVariableTypes_SameLifetimeSetOnOutput()
        {
            NIType signatureType = Signatures.SelectReferenceType;
            DfirRoot dfirRoot = DfirRoot.Create();
            FunctionalNode functionalNode = new FunctionalNode(dfirRoot.BlockDiagram, signatureType);
            ExplicitBorrowNode borrow = new ExplicitBorrowNode(dfirRoot.BlockDiagram, BorrowMode.Immutable, 2, true, true);
            Wire wire1 = Wire.Create(dfirRoot.BlockDiagram, borrow.OutputTerminals[0], functionalNode.InputTerminals[1]);
            Wire wire2 = Wire.Create(dfirRoot.BlockDiagram, borrow.OutputTerminals[1], functionalNode.InputTerminals[2]);
            ConnectConstantToInputTerminal(borrow.InputTerminals[0], PFTypes.Int32, false);
            ConnectConstantToInputTerminal(borrow.InputTerminals[1], PFTypes.Int32, false);

            RunSemanticAnalysisUpToSetVariableTypes(dfirRoot);

            AutoBorrowNodeFacade nodeFacade = AutoBorrowNodeFacade.GetNodeFacade(functionalNode);
            Terminal inputTerminal = functionalNode.InputTerminals[1];
            Lifetime inputLifetime = inputTerminal.GetTrueVariable().Lifetime;
            Terminal outputTerminal = functionalNode.OutputTerminals[1];
            Assert.AreEqual(inputLifetime, outputTerminal.GetTrueVariable().Lifetime);
        }

        [TestMethod]
        public void FunctionNodeWithSelectReferenceSignatureAndMutableValuesWired_SetVariableTypes_MutableReferenceTypeSetOnOutput()
        {
            NIType signatureType = Signatures.SelectReferenceType;
            DfirRoot dfirRoot = DfirRoot.Create();
            FunctionalNode functionalNode = new FunctionalNode(dfirRoot.BlockDiagram, signatureType);
            ConnectConstantToInputTerminal(functionalNode.InputTerminals[1], PFTypes.Int32, true);
            ConnectConstantToInputTerminal(functionalNode.InputTerminals[2], PFTypes.Int32, true);

            RunSemanticAnalysisUpToSetVariableTypes(dfirRoot);

            AutoBorrowNodeFacade nodeFacade = AutoBorrowNodeFacade.GetNodeFacade(functionalNode);
            Terminal outputTerminal = functionalNode.OutputTerminals[1];
            VariableReference outputTerminalVariable = outputTerminal.GetTrueVariable();
            Assert.IsTrue(outputTerminalVariable.Type.IsMutableReferenceType());
            Assert.IsTrue(outputTerminalVariable.Type.GetReferentType().IsInt32());
        }

        [TestMethod]
        public void FunctionNodeWithSelectReferenceSignatureAndSameLifetimeMutableReferencesWired_SetVariableTypes_SameLifetimeSetOnOutput()
        {
            NIType signatureType = Signatures.SelectReferenceType;
            DfirRoot dfirRoot = DfirRoot.Create();
            FunctionalNode functionalNode = new FunctionalNode(dfirRoot.BlockDiagram, signatureType);
            ExplicitBorrowNode borrow = new ExplicitBorrowNode(dfirRoot.BlockDiagram, BorrowMode.Mutable, 2, true, true);
            Wire wire1 = Wire.Create(dfirRoot.BlockDiagram, borrow.OutputTerminals[0], functionalNode.InputTerminals[1]);
            Wire wire2 = Wire.Create(dfirRoot.BlockDiagram, borrow.OutputTerminals[1], functionalNode.InputTerminals[2]);
            ConnectConstantToInputTerminal(functionalNode.InputTerminals[1], PFTypes.Int32, true);
            ConnectConstantToInputTerminal(functionalNode.InputTerminals[2], PFTypes.Int32, true);

            RunSemanticAnalysisUpToSetVariableTypes(dfirRoot);

            Terminal inputTerminal = functionalNode.InputTerminals[1];
            Lifetime inputLifetime = inputTerminal.GetTrueVariable().Lifetime;
            Terminal outputTerminal = functionalNode.OutputTerminals[1];
            VariableReference outputTerminalVariable = outputTerminal.GetTrueVariable();
            Assert.IsTrue(outputTerminalVariable.Type.IsMutableReferenceType());
            Assert.AreEqual(inputLifetime, outputTerminalVariable.Lifetime);
        }

        [TestMethod]
        public void FunctionNodeWithSelectReferenceSignatureAndNoInputsWired_SetVariableTypes_VoidReferenceSetOnOutput()
        {
            NIType signatureType = Signatures.SelectReferenceType;
            DfirRoot dfirRoot = DfirRoot.Create();
            FunctionalNode functionalNode = new FunctionalNode(dfirRoot.BlockDiagram, signatureType);

            RunSemanticAnalysisUpToSetVariableTypes(dfirRoot);

            Terminal outputTerminal = functionalNode.OutputTerminals[1];
            NIType outputTerminalType = outputTerminal.GetTrueVariable().Type;
            Assert.IsTrue(outputTerminalType.IsImmutableReferenceType());
            Assert.IsTrue(outputTerminalType.GetReferentType().IsVoid());
        }

        [TestMethod]
        public void FunctionNodeWithConstrainedGenericReferenceInputParameterAndAllowableInputWired_SetVariableTypes_InputTerminalTypeSet()
        {
            NIType signatureType = Signatures.OutputType;
            DfirRoot dfirRoot = DfirRoot.Create();
            FunctionalNode functionalNode = new FunctionalNode(dfirRoot.BlockDiagram, signatureType);
            ConnectConstantToInputTerminal(functionalNode.InputTerminals[0], PFTypes.Int32, false);

            RunSemanticAnalysisUpToSetVariableTypes(dfirRoot);

            Terminal inputTerminal = functionalNode.InputTerminals[0];
            NIType inputType = inputTerminal.GetTrueVariable().Type;
            Assert.IsTrue(inputType.IsImmutableReferenceType());
            Assert.IsTrue(inputType.GetReferentType().IsInt32());
        }

        #endregion

        #region ValidateVariableUsages

        [TestMethod]
        public void FunctionNodeWithMutableInOutSignatureParameterAndImmutableVariableWired_ValidateVariableUsages_ErrorCreated()
        {
            NIType signatureType = Signatures.MutablePassthroughType;
            DfirRoot dfirRoot = DfirRoot.Create();
            FunctionalNode functionalNode = new FunctionalNode(dfirRoot.BlockDiagram, signatureType);
            ConnectConstantToInputTerminal(functionalNode.InputTerminals[0], PFTypes.Int32, false);

            RunSemanticAnalysisUpToValidation(dfirRoot);

            Assert.IsTrue(functionalNode.InputTerminals[0].GetDfirMessages().Any(message => message.Descriptor == Messages.TerminalDoesNotAcceptImmutableType.Descriptor));
        }

        [TestMethod]
        public void FunctionNodeWithMutableInOutSignatureParameterAndImmutableReferenceWired_ValidateVariableUsages_ErrorCreated()
        {
            NIType signatureType = Signatures.MutablePassthroughType;
            DfirRoot dfirRoot = DfirRoot.Create();
            FunctionalNode functionalNode = new FunctionalNode(dfirRoot.BlockDiagram, signatureType);
            ExplicitBorrowNode borrow = ConnectExplicitBorrowToInputTerminal(functionalNode.InputTerminals[0]);
            ConnectConstantToInputTerminal(borrow.InputTerminals[0], PFTypes.Int32, false);

            RunSemanticAnalysisUpToValidation(dfirRoot);

            Assert.IsTrue(functionalNode.InputTerminals[0].GetDfirMessages().Any(message => message.Descriptor == Messages.TerminalDoesNotAcceptImmutableType.Descriptor));
        }

        [TestMethod]
        public void FunctionNodeWithMutableInOutSignatureParameterAndMutableVariableWired_ValidateVariableUsages_NoErrorCreated()
        {
            NIType signatureType = Signatures.MutablePassthroughType;
            DfirRoot dfirRoot = DfirRoot.Create();
            FunctionalNode functionalNode = new FunctionalNode(dfirRoot.BlockDiagram, signatureType);
            ConnectConstantToInputTerminal(functionalNode.InputTerminals[0], PFTypes.Int32, true);

            RunSemanticAnalysisUpToValidation(dfirRoot);

            Assert.IsFalse(functionalNode.GetDfirMessages().Any(message => message.Descriptor == Messages.TerminalDoesNotAcceptImmutableType.Descriptor));
        }

        [TestMethod]
        public void FunctionNodeWithGenericImmutableInOutSignatureParameterAndImmutableReferenceVariableWired_ValidateVariableUsages_NoErrorCreated()
        {
            NIType signatureType = Signatures.ImmutablePassthroughType;
            DfirRoot dfirRoot = DfirRoot.Create();
            FunctionalNode functionalNode = new FunctionalNode(dfirRoot.BlockDiagram, signatureType);
            ExplicitBorrowNode borrow = ConnectExplicitBorrowToInputTerminal(functionalNode.InputTerminals[0]);
            ConnectConstantToInputTerminal(borrow.InputTerminals[0], PFTypes.Int32, false);

            RunSemanticAnalysisUpToValidation(dfirRoot);

            Assert.IsFalse(functionalNode.InputTerminals[0].GetDfirMessages().Any());
        }

        [TestMethod]
        public void FunctionNodeWithNonReferenceInSignatureParameterAndReferenceVariableWired_ValidateVariableUsages_ErrorCreated()
        {
            NIType signatureType = Signatures.RangeType;
            DfirRoot dfirRoot = DfirRoot.Create();
            FunctionalNode functionalNode = new FunctionalNode(dfirRoot.BlockDiagram, signatureType);
            ExplicitBorrowNode borrow = ConnectExplicitBorrowToInputTerminal(functionalNode.InputTerminals[0]);
            ConnectConstantToInputTerminal(borrow.InputTerminals[0], PFTypes.Int32, false);

            RunSemanticAnalysisUpToValidation(dfirRoot);

            AssertTerminalHasTypeConflictMessage(functionalNode.InputTerminals[0]);
        }

        [TestMethod]
        public void FunctionNodeWithNonReferenceInSignatureParameterAndNonReferenceVariableWired_ValidateVariableUsages_NoErrorCreated()
        {
            NIType signatureType = Signatures.RangeType;
            DfirRoot dfirRoot = DfirRoot.Create();
            FunctionalNode functionalNode = new FunctionalNode(dfirRoot.BlockDiagram, signatureType);
            ConnectConstantToInputTerminal(functionalNode.InputTerminals[0], PFTypes.Int32, false);

            RunSemanticAnalysisUpToValidation(dfirRoot);

            Assert.IsFalse(functionalNode.GetDfirMessages().Any(message => message.Descriptor == Messages.TerminalDoesNotAcceptReference.Descriptor));
        }

        [TestMethod]
        public void FunctionNodeWithNonGenericSignatureParameterAndIncorrectTypeWired_ValidateVariableUsages_ErrorCreated()
        {
            DfirRoot dfirRoot = DfirRoot.Create();
            NIFunctionBuilder signatureTypeBuilder = PFTypes.Factory.DefineFunction("NonGenericInput");
            Signatures.AddInputParameter(signatureTypeBuilder, PFTypes.Int32, "input");
            NIType signatureType = signatureTypeBuilder.CreateType();
            FunctionalNode functionalNode = new FunctionalNode(dfirRoot.BlockDiagram, signatureType);
            ConnectConstantToInputTerminal(functionalNode.InputTerminals[0], PFTypes.Boolean, false);

            RunSemanticAnalysisUpToValidation(dfirRoot);

            AssertTerminalHasTypeConflictMessage(functionalNode.InputTerminals[0]);
        }

        [TestMethod]
        public void FunctionNodeWithNonGenericSignatureParameterAndCorrectTypeWired_ValidateVariableUsages_NoErrorCreated()
        {
            NIType signatureType = Signatures.OutputType;
            DfirRoot dfirRoot = DfirRoot.Create();
            FunctionalNode functionalNode = new FunctionalNode(dfirRoot.BlockDiagram, signatureType);
            ConnectConstantToInputTerminal(functionalNode.InputTerminals[0], PFTypes.Int32, false);

            RunSemanticAnalysisUpToValidation(dfirRoot);

            AssertTerminalDoesNotHaveTypeConflictMessage(functionalNode.InputTerminals[0]);
        }

        [TestMethod]
        public void FunctionNodeWithGenericSignatureParameterAndIncorrectTypeWired_ValidateVariableUsages_ErrorCreated()
        {
            NIType signatureType = Signatures.VectorInsertType;
            DfirRoot dfirRoot = DfirRoot.Create();
            FunctionalNode functionalNode = new FunctionalNode(dfirRoot.BlockDiagram, signatureType);
            ConnectConstantToInputTerminal(functionalNode.InputTerminals[0], PFTypes.Boolean, false);

            RunSemanticAnalysisUpToValidation(dfirRoot);

            AssertTerminalHasTypeConflictMessage(functionalNode.InputTerminals[0]);
        }

        [TestMethod]
        public void FunctionNodeWithSelectReferenceSignatureAndDifferentValueTypesWired_ValidateVariableUsages_TypeMismatchErrorCreated()
        {
            NIType signatureType = Signatures.SelectReferenceType;
            DfirRoot dfirRoot = DfirRoot.Create();
            FunctionalNode functionalNode = new FunctionalNode(dfirRoot.BlockDiagram, signatureType);
            ConnectConstantToInputTerminal(functionalNode.InputTerminals[1], PFTypes.Boolean, false);
            ConnectConstantToInputTerminal(functionalNode.InputTerminals[2], PFTypes.Int32, false);

            RunSemanticAnalysisUpToValidation(dfirRoot);

            AssertTerminalHasTypeConflictMessage(functionalNode.InputTerminals[2]);
        }

        [TestMethod]
        public void FunctionNodeWithConstrainedGenericInputParameterAndDisallowedTypeWired_ValidateVariableUsages_ErrorCreated()
        {
            NIType signatureType = Signatures.OutputType;
            DfirRoot dfirRoot = DfirRoot.Create();
            FunctionalNode functionalNode = new FunctionalNode(dfirRoot.BlockDiagram, signatureType);
            ConnectConstantToInputTerminal(functionalNode.InputTerminals[0], PFTypes.Boolean, false);

            RunSemanticAnalysisUpToValidation(dfirRoot);

            Assert.IsTrue(functionalNode.InputTerminals[0].GetDfirMessages().Any(message => message.Descriptor == Messages.TypeDoesNotHaveRequiredTrait.Descriptor));
        }

        #endregion
    }
}
