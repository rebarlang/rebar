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
        public void TerminateLifetimeWithNoInputLifetimesWired_ValidateVariableUsages_NonUniqueLifetimeErrorMessageReported()
        {
            DfirRoot function = DfirRoot.Create();
            TerminateLifetimeNode terminateLifetime = new TerminateLifetimeNode(function.BlockDiagram, 2, 1);

            RunSemanticAnalysisUpToValidation(function);

            Assert.IsTrue(terminateLifetime.InputTerminals[0].GetDfirMessages().Any(message => message.Descriptor == AllModelsOfComputationErrorMessages.RequiredTerminalUnconnected));
            Assert.IsTrue(terminateLifetime.InputTerminals[1].GetDfirMessages().Any(message => message.Descriptor == AllModelsOfComputationErrorMessages.RequiredTerminalUnconnected));
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
            Tunnel tunnel = frame.CreateTunnel(Direction.Input, TunnelMode.LastValue, PFTypes.Void, PFTypes.Void);
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
            Assert.IsTrue(borrow.InputTerminals[0].GetTrueVariable().ReferencesSame(terminateLifetime.OutputTerminals[0].GetTrueVariable()));
            Assert.IsTrue(borrow.InputTerminals[1].GetTrueVariable().ReferencesSame(terminateLifetime.OutputTerminals[1].GetTrueVariable()));
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
    }
}
