using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.Compiler.SemanticAnalysis;
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

        #endregion

        #region SetVariableTypes

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

            Assert.IsTrue(functionalNode.GetDfirMessages().Any(message => message.Descriptor == Messages.TerminalDoesNotAcceptImmutableType.Descriptor));
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
        public void FunctionNodeWithNonReferenceInSignatureParameterAndReferenceVariableWired_ValidateVariableUsages_ErrorCreated()
        {
            NIType signatureType = Signatures.RangeType;
            DfirRoot dfirRoot = DfirRoot.Create();
            FunctionalNode functionalNode = new FunctionalNode(dfirRoot.BlockDiagram, signatureType);
            ExplicitBorrowNode borrow = new ExplicitBorrowNode(dfirRoot.BlockDiagram, BorrowMode.Immutable, 1, true, true);
            Terminal inputTerminal = functionalNode.InputTerminals[0];
            Wire wire = Wire.Create(inputTerminal.ParentDiagram, borrow.OutputTerminals[0], inputTerminal);
            ConnectConstantToInputTerminal(borrow.InputTerminals[0], PFTypes.Int32, false);

            RunSemanticAnalysisUpToValidation(dfirRoot);

            Assert.IsTrue(functionalNode.GetDfirMessages().Any(message => message.Descriptor == Messages.TerminalDoesNotAcceptReference.Descriptor));
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
            NIType signatureType = Signatures.OutputType;
            DfirRoot dfirRoot = DfirRoot.Create();
            FunctionalNode functionalNode = new FunctionalNode(dfirRoot.BlockDiagram, signatureType);
            ConnectConstantToInputTerminal(functionalNode.InputTerminals[0], PFTypes.Boolean, false);

            RunSemanticAnalysisUpToValidation(dfirRoot);

            Assert.IsTrue(functionalNode.InputTerminals[0].GetDfirMessages().Any(message => message.Descriptor == AllModelsOfComputationErrorMessages.TypeConflict));
        }

        [TestMethod]
        public void FunctionNodeWithNonGenericSignatureParameterAndCorrectTypeWired_ValidateVariableUsages_NoErrorCreated()
        {
            NIType signatureType = Signatures.OutputType;
            DfirRoot dfirRoot = DfirRoot.Create();
            FunctionalNode functionalNode = new FunctionalNode(dfirRoot.BlockDiagram, signatureType);
            ConnectConstantToInputTerminal(functionalNode.InputTerminals[0], PFTypes.Int32, false);

            RunSemanticAnalysisUpToValidation(dfirRoot);

            Assert.IsFalse(functionalNode.InputTerminals[0].GetDfirMessages().Any(message => message.Descriptor == AllModelsOfComputationErrorMessages.TypeConflict));
        }

        [TestMethod]
        public void FunctionNodeWithGenericSignatureParameterAndIncorrectTypeWired_ValidateVariableUsages_ErrorCreated()
        {
            NIType signatureType = Signatures.VectorInsertType;
            DfirRoot dfirRoot = DfirRoot.Create();
            FunctionalNode functionalNode = new FunctionalNode(dfirRoot.BlockDiagram, signatureType);
            ConnectConstantToInputTerminal(functionalNode.InputTerminals[0], PFTypes.Boolean, false);

            RunSemanticAnalysisUpToValidation(dfirRoot);

            Assert.IsTrue(functionalNode.InputTerminals[0].GetDfirMessages().Any(message => message.Descriptor == AllModelsOfComputationErrorMessages.TypeConflict));
        }
        #endregion
    }
}
