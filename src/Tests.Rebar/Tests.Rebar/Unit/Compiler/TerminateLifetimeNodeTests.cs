using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.Compiler.SemanticAnalysis;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler;
using Rebar.Compiler.Nodes;

namespace Tests.Rebar.Unit.Compiler
{
    [TestClass]
    public class TerminateLifetimeNodeTests : CompilerTestBase
    {
        [TestMethod]
        public void TerminateLifetimeWithNoInputLifetimesWired_SetVariableTypes_OutputVariableHasType()
        {
            DfirRoot function = DfirRoot.Create();
            TerminateLifetimeNode terminateLifetime = new TerminateLifetimeNode(function.BlockDiagram, 1, 1);

            RunSemanticAnalysisUpToSetVariableTypes(function);

            VariableReference outputVariable = terminateLifetime.OutputTerminals[0].GetFacadeVariable();
            Assert.IsNotNull(outputVariable.TypeVariableReference.TypeVariableSet);
            Assert.IsTrue(outputVariable.Type.IsVoid());
        }

        [TestMethod]
        public void TerminateLifetimeWithNoInputLifetimesWired_ValidateVariableUsages_NonUniqueLifetimeErrorMessageReported()
        {
            DfirRoot function = DfirRoot.Create();
            TerminateLifetimeNode terminateLifetime = new TerminateLifetimeNode(function.BlockDiagram, 2, 1);

            RunSemanticAnalysisUpToValidation(function);

            AssertTerminalHasRequiredTerminalUnconnectedMessage(terminateLifetime.InputTerminals[0]);
            AssertTerminalHasRequiredTerminalUnconnectedMessage(terminateLifetime.InputTerminals[1]);
        }

        [TestMethod]
        public void TerminateLifetimeWithMultipleInputLifetimesWired_ValidateVariableUsages_NonUniqueLifetimeErrorMessageReported()
        {
            DfirRoot function = DfirRoot.Create();
            TerminateLifetimeNode terminateLifetime = new TerminateLifetimeNode(function.BlockDiagram, 2, 1);
            ExplicitBorrowNode borrow1 = ConnectExplicitBorrowToInputTerminals(terminateLifetime.InputTerminals[0]);
            ConnectConstantToInputTerminal(borrow1.InputTerminals[0], PFTypes.Int32, false);
            ExplicitBorrowNode borrow2 = ConnectExplicitBorrowToInputTerminals(terminateLifetime.InputTerminals[1]);
            ConnectConstantToInputTerminal(borrow2.InputTerminals[0], PFTypes.Int32, false);

            RunSemanticAnalysisUpToValidation(function);

            Assert.IsTrue(terminateLifetime.GetDfirMessages().Any(message => message.Descriptor == Messages.TerminateLifetimeInputLifetimesNotUnique.Descriptor));
        }

        [TestMethod]
        public void TerminateLifetimeWithUnboundedInputLifetimeWired_ValidateVariableUsages_LifetimeCannotBeTerminatedErrorMessageReported()
        {
            DfirRoot function = DfirRoot.Create();
            TerminateLifetimeNode terminateLifetime = new TerminateLifetimeNode(function.BlockDiagram, 1, 1);
            ConnectConstantToInputTerminal(terminateLifetime.InputTerminals[0], PFTypes.Int32, false);

            RunSemanticAnalysisUpToValidation(function);

            Assert.IsTrue(terminateLifetime.GetDfirMessages().Any(message => message.Descriptor == Messages.TerminateLifetimeInputLifetimeCannotBeTerminated.Descriptor));
        }

        [TestMethod]
        public void TerminateLifetimeWithInputLifetimeThatOutlastsDiagramWired_ValidateVariableUsages_LifetimeCannotBeTerminatedErrorMessageReported()
        {
            DfirRoot function = DfirRoot.Create();
            Frame frame = Frame.Create(function.BlockDiagram);
            Tunnel tunnel = CreateInputTunnel(frame);
            ExplicitBorrowNode borrow = ConnectExplicitBorrowToInputTerminals(tunnel.InputTerminals[0]);
            ConnectConstantToInputTerminal(borrow.InputTerminals[0], PFTypes.Int32, false);
            TerminateLifetimeNode terminateLifetime = new TerminateLifetimeNode(frame.Diagram, 1, 1);
            Wire wire = Wire.Create(frame.Diagram, tunnel.OutputTerminals[0], terminateLifetime.InputTerminals[0]);

            RunSemanticAnalysisUpToValidation(function);

            Assert.IsTrue(terminateLifetime.GetDfirMessages().Any(message => message.Descriptor == Messages.TerminateLifetimeInputLifetimeCannotBeTerminated.Descriptor));
        }

        [TestMethod]
        public void TerminateLifetimeWithStructureBorderInputLifetimeWired_ValidateVariableUsages_LifetimeCannotBeTerminatedErrorMessageReported()
        {
            DfirRoot function = DfirRoot.Create();
            Frame frame = Frame.Create(function.BlockDiagram);
            BorrowTunnel borrowTunnel = CreateBorrowTunnel(frame, BorrowMode.Immutable);
            ConnectConstantToInputTerminal(borrowTunnel.InputTerminals[0], PFTypes.Int32, false);
            TerminateLifetimeNode terminateLifetime = new TerminateLifetimeNode(frame.Diagram, 1, 1);
            Wire wire = Wire.Create(frame.Diagram, borrowTunnel.OutputTerminals[0], terminateLifetime.InputTerminals[0]);

            RunSemanticAnalysisUpToValidation(function);

            Assert.IsTrue(terminateLifetime.GetDfirMessages().Any(message => message.Descriptor == Messages.TerminateLifetimeInputLifetimeCannotBeTerminated.Descriptor));
        }

        [TestMethod]
        public void TerminateLifetimeWithNotAllVariablesInLifetimeWired_ValidateVariableUsages_NotAllVariablesInLifetimeConnectedErrorMessageReported()
        {
            DfirRoot function = DfirRoot.Create();
            TerminateLifetimeNode terminateLifetime = new TerminateLifetimeNode(function.BlockDiagram, 1, 1);
            ExplicitBorrowNode borrow = ConnectExplicitBorrowToInputTerminals(terminateLifetime.InputTerminals[0]);
            ConnectConstantToInputTerminal(borrow.InputTerminals[0], PFTypes.Int32, false);
            FunctionalNode passthrough = new FunctionalNode(function.BlockDiagram, Signatures.ImmutablePassthroughType);
            borrow.OutputTerminals[0].WireTogether(passthrough.InputTerminals[0], SourceModelIdSource.NoSourceModelId);

            RunSemanticAnalysisUpToValidation(function);

            Assert.IsTrue(terminateLifetime.GetDfirMessages().Any(message => message.Descriptor == Messages.TerminateLifetimeNotAllVariablesInLifetimeConnected.Descriptor));
        }

        [TestMethod]
        public void TerminateLifetimeWithAllVariablesInLifetimeWired_SetVariableTypes_LifetimeDecomposedVariablesSetOnOutputs()
        {
            DfirRoot function = DfirRoot.Create();
            TerminateLifetimeNode terminateLifetime = new TerminateLifetimeNode(function.BlockDiagram, 2, 1);
            ExplicitBorrowNode borrow = ConnectExplicitBorrowToInputTerminals(terminateLifetime.InputTerminals[0], terminateLifetime.InputTerminals[1]);
            ConnectConstantToInputTerminal(borrow.InputTerminals[0], PFTypes.Int32, false);
            ConnectConstantToInputTerminal(borrow.InputTerminals[1], PFTypes.Int32, false);

            RunSemanticAnalysisUpToSetVariableTypes(function);

            Assert.AreEqual(2, terminateLifetime.OutputTerminals.Count);
            AssertVariablesReferenceSame(terminateLifetime.OutputTerminals[0].GetTrueVariable(), borrow.InputTerminals[0].GetTrueVariable());
            AssertVariablesReferenceSame(terminateLifetime.OutputTerminals[1].GetTrueVariable(), borrow.InputTerminals[1].GetTrueVariable());
        }

        [TestMethod]
        public void TerminateLifetimeWithAllVariablesInLifetimeWired_ValidateVariableUsages_NoError()
        {
            DfirRoot function = DfirRoot.Create();
            TerminateLifetimeNode terminateLifetime = new TerminateLifetimeNode(function.BlockDiagram, 2, 1);
            ExplicitBorrowNode borrow = ConnectExplicitBorrowToInputTerminals(terminateLifetime.InputTerminals[0], terminateLifetime.InputTerminals[1]);
            ConnectConstantToInputTerminal(borrow.InputTerminals[0], PFTypes.Int32, false);
            ConnectConstantToInputTerminal(borrow.InputTerminals[1], PFTypes.Int32, false);

            RunSemanticAnalysisUpToValidation(function);

            Assert.AreEqual(TerminateLifetimeErrorState.NoError, terminateLifetime.ErrorState);
            Assert.IsFalse(terminateLifetime.GetDfirMessages().Any());
        }

        [TestMethod]
        public void TypeDeterminantDownstreamOfTerminateLifetime_SetVariableTypes_UpstreamTypeSetCorrectly()
        {
            DfirRoot function = DfirRoot.Create();
            var genericOutput = new FunctionalNode(function.BlockDiagram, DefineGenericOutputFunctionSignature());
            var terminateLifetime = new TerminateLifetimeNode(function.BlockDiagram, 1, 1);
            var borrow = ConnectExplicitBorrowToInputTerminals(terminateLifetime.InputTerminals[0]);
            genericOutput.OutputTerminals[0].WireTogether(borrow.InputTerminals[0], SourceModelIdSource.NoSourceModelId);
            var assignNode = new FunctionalNode(function.BlockDiagram, Signatures.AssignType);
            terminateLifetime.OutputTerminals[0].WireTogether(assignNode.InputTerminals[0], SourceModelIdSource.NoSourceModelId);
            ConnectConstantToInputTerminal(assignNode.InputTerminals[1], PFTypes.Int32, false);

            RunSemanticAnalysisUpToSetVariableTypes(function);

            Assert.IsTrue(genericOutput.OutputTerminals[0].GetTrueVariable().Type.IsInt32());
        }

        #region Lifetimes with consumed variables

        [TestMethod]
        public void TerminateLifetimeOnLifetimeContainingVariablesConsumedByFunctionalNode_SetVariableTypes_TerminateLifetimeHasNoError()
        {
            DfirRoot function = DfirRoot.Create();
            TerminateLifetimeNode terminateLifetime = new TerminateLifetimeNode(function.BlockDiagram, 1, 1);
            FunctionalNode selectReference = new FunctionalNode(function.BlockDiagram, Signatures.SelectReferenceType);
            Wire.Create(function.BlockDiagram, selectReference.OutputTerminals[1], terminateLifetime.InputTerminals[0]);
            ExplicitBorrowNode borrow = ConnectExplicitBorrowToInputTerminals(selectReference.InputTerminals[1], selectReference.InputTerminals[2]);
            ConnectConstantToInputTerminal(borrow.InputTerminals[0], PFTypes.Int32, false);
            ConnectConstantToInputTerminal(borrow.InputTerminals[1], PFTypes.Int32, false);

            RunSemanticAnalysisUpToSetVariableTypes(function);

            Assert.AreEqual(TerminateLifetimeErrorState.NoError, terminateLifetime.ErrorState);
            Assert.AreEqual(2, terminateLifetime.OutputTerminals.Count);
        }

        [TestMethod]
        public void TerminateLifetimeOnLifetimeContainingVariablesConsumedByTunnel_SetVariableTypes_TerminateLifetimeHasNoError()
        {
            DfirRoot function = DfirRoot.Create();
            TerminateLifetimeNode terminateLifetime = new TerminateLifetimeNode(function.BlockDiagram, 1, 1);
            Frame frame = Frame.Create(function.BlockDiagram);
            Tunnel inputTunnel1 = CreateInputTunnel(frame);
            Tunnel inputTunnel2 = CreateInputTunnel(frame);
            Tunnel outputTunnel = CreateOutputTunnel(frame);
            FunctionalNode selectReference = new FunctionalNode(frame.Diagram, Signatures.SelectReferenceType);
            Wire.Create(frame.Diagram, inputTunnel1.OutputTerminals[0], selectReference.InputTerminals[1]);
            Wire.Create(frame.Diagram, inputTunnel2.OutputTerminals[0], selectReference.InputTerminals[2]);
            Wire.Create(frame.Diagram, selectReference.OutputTerminals[1], outputTunnel.InputTerminals[0]);
            Wire.Create(function.BlockDiagram, outputTunnel.OutputTerminals[0], terminateLifetime.InputTerminals[0]);
            ExplicitBorrowNode borrow = ConnectExplicitBorrowToInputTerminals(inputTunnel1.InputTerminals[0], inputTunnel2.InputTerminals[0]);
            ConnectConstantToInputTerminal(borrow.InputTerminals[0], PFTypes.Int32, false);
            ConnectConstantToInputTerminal(borrow.InputTerminals[1], PFTypes.Int32, false);

            RunSemanticAnalysisUpToSetVariableTypes(function);

            Assert.AreEqual(TerminateLifetimeErrorState.NoError, terminateLifetime.ErrorState);
            Assert.AreEqual(2, terminateLifetime.OutputTerminals.Count);
        }

        [TestMethod]
        public void TerminateLifetimeOnLifetimeContainingVariablesConsumedByUnwrapOptionTunnel_SetVariableTypes_TerminateLifetimeHasNoError()
        {
            DfirRoot function = DfirRoot.Create();
            Frame frame = Frame.Create(function.BlockDiagram);
            UnwrapOptionTunnel unwrapOptionTunnel = new UnwrapOptionTunnel(frame);
            Tunnel outputTunnel = CreateOutputTunnel(frame);
            Wire.Create(frame.Diagram, unwrapOptionTunnel.OutputTerminals[0], outputTunnel.InputTerminals[0]);
            TerminateLifetimeNode terminateLifetime = new TerminateLifetimeNode(function.BlockDiagram, 1, 1);
            Wire.Create(function.BlockDiagram, outputTunnel.OutputTerminals[0], terminateLifetime.InputTerminals[0]);
            FunctionalNode someConstructor = new FunctionalNode(function.BlockDiagram, Signatures.SomeConstructorType);
            Wire.Create(function.BlockDiagram, someConstructor.OutputTerminals[0], unwrapOptionTunnel.InputTerminals[0]);
            ExplicitBorrowNode borrow = ConnectExplicitBorrowToInputTerminals(someConstructor.InputTerminals[0]);
            ConnectConstantToInputTerminal(borrow.InputTerminals[0], PFTypes.Int32, false);

            RunSemanticAnalysisUpToSetVariableTypes(function);

            Assert.AreEqual(TerminateLifetimeErrorState.NoError, terminateLifetime.ErrorState);
            Assert.AreEqual(1, terminateLifetime.OutputTerminals.Count);
        }

        #endregion

        [TestMethod]
        public void LifetimeWithLiveVariablesOnMainAndNestedDiagrams_ValidateVariableUsages_TerminateLifetimeNodeHasCorrectInputTerminalCount()
        {
            var signatureBuilder = PFTypes.Factory.DefineFunction("outputString");
            Signatures.AddOutputParameter(signatureBuilder, PFTypes.String, "owner");
            NIType outputOwnerStringSignature = signatureBuilder.CreateType();

            DfirRoot function = DfirRoot.Create();
            var outputString = new FunctionalNode(function.BlockDiagram, outputOwnerStringSignature);
            ExplicitBorrowNode borrow = new ExplicitBorrowNode(function.BlockDiagram, BorrowMode.Immutable, 1, true, true);
            Wire.Create(function.BlockDiagram, outputString.OutputTerminals[0], borrow.InputTerminals[0]);
            Frame outerFrame = Frame.Create(function.BlockDiagram);
            Tunnel outerFrameInputTunnel = CreateInputTunnel(outerFrame);
            Wire.Create(function.BlockDiagram, borrow.OutputTerminals[0], outerFrameInputTunnel.InputTerminals[0]);
            Frame innerFrame = Frame.Create(outerFrame.Diagram);
            Tunnel innerFrameInputTunnel = CreateInputTunnel(innerFrame), innerFrameOutputTunnel = CreateOutputTunnel(innerFrame);
            FunctionalNode outerStringToSlice = new FunctionalNode(outerFrame.Diagram, Signatures.StringToSliceType);
            Wire.Create(outerFrame.Diagram, outerFrameInputTunnel.OutputTerminals[0], innerFrameInputTunnel.InputTerminals[0], outerStringToSlice.InputTerminals[0]);
            FunctionalNode innerStringToSlice = new FunctionalNode(innerFrame.Diagram, Signatures.StringToSliceType);
            Wire.Create(innerFrame.Diagram, innerFrameInputTunnel.OutputTerminals[0], innerStringToSlice.InputTerminals[0]);
            Wire.Create(innerFrame.Diagram, innerStringToSlice.OutputTerminals[0], innerFrameOutputTunnel.InputTerminals[0]);
            Tunnel outerFrameOutputTunnel = CreateOutputTunnel(outerFrame);
            Wire.Create(outerFrame.Diagram, outerStringToSlice.OutputTerminals[0], outerFrameOutputTunnel.InputTerminals[0]);
            TerminateLifetimeNode terminateLifetime = new TerminateLifetimeNode(function.BlockDiagram, 1, 1);
            Wire.Create(function.BlockDiagram, outerFrameOutputTunnel.OutputTerminals[0], terminateLifetime.InputTerminals[0]);

            RunSemanticAnalysisUpToValidation(function);

            Assert.AreEqual(2, terminateLifetime.InputTerminals.Count);
        }
    }
}
