using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler.Nodes;

namespace Tests.Rebar.Unit.Compiler
{
    [TestClass]
    public class AutomaticNodeInsertionTests : CompilerTestBase
    {
        private static readonly NIType _outputOwnerSignature;

        static AutomaticNodeInsertionTests()
        {
            NIFunctionBuilder outputOwnerSignatureBuilder = PFTypes.Factory.DefineFunction("outputOwner");
            Signatures.AddOutputParameter(outputOwnerSignatureBuilder, PFTypes.Int32, "owner");
            _outputOwnerSignature = outputOwnerSignatureBuilder.CreateType();
        }

        [TestMethod]
        public void BorrowNodeWithUnwiredOutput_AutomaticNodeInsertion_TerminateLifetimeNodeInserted()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode outputOwner = new FunctionalNode(function.BlockDiagram, _outputOwnerSignature);
            ExplicitBorrowNode borrow = new ExplicitBorrowNode(function.BlockDiagram, BorrowMode.Immutable, 1, true, true);
            Wire.Create(function.BlockDiagram, outputOwner.OutputTerminals[0], borrow.InputTerminals[0]);

            RunCompilationUpToAutomaticNodeInsertion(function);

            var terminateLifetime = function.BlockDiagram.Nodes.OfType<TerminateLifetimeNode>().FirstOrDefault();
            Assert.IsNotNull(terminateLifetime);
            Assert.AreEqual(1, terminateLifetime.InputTerminals.Count);
            Assert.AreEqual(borrow.OutputTerminals[0], terminateLifetime.InputTerminals[0].GetImmediateSourceTerminal());
        }

        [TestMethod]
        public void BorrowNodeIntoImmutablePassthroughWithUnwiredOutput_AutomaticNodeInsertion_TerminateLifetimeNodeInserted()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode outputOwner = new FunctionalNode(function.BlockDiagram, _outputOwnerSignature);
            ExplicitBorrowNode borrow = new ExplicitBorrowNode(function.BlockDiagram, BorrowMode.Immutable, 1, true, true);
            Wire.Create(function.BlockDiagram, outputOwner.OutputTerminals[0], borrow.InputTerminals[0]);
            FunctionalNode immutablePassthrough = new FunctionalNode(function.BlockDiagram, Signatures.ImmutablePassthroughType);
            Wire.Create(function.BlockDiagram, borrow.OutputTerminals[0], immutablePassthrough.InputTerminals[0]);

            RunCompilationUpToAutomaticNodeInsertion(function);

            var terminateLifetime = function.BlockDiagram.Nodes.OfType<TerminateLifetimeNode>().FirstOrDefault();
            Assert.IsNotNull(terminateLifetime);
            Assert.AreEqual(1, terminateLifetime.InputTerminals.Count);
            Assert.AreEqual(immutablePassthrough.OutputTerminals[0], terminateLifetime.InputTerminals[0].GetImmediateSourceTerminal());
        }

        [TestMethod]
        public void BorrowNodeBranchedIntoTwoImmutablePassthrougshWithUnwiredOutputs_AutomaticNodeInsertion_TerminateLifetimeNodeInserted()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode outputOwner = new FunctionalNode(function.BlockDiagram, _outputOwnerSignature);
            ExplicitBorrowNode borrow = new ExplicitBorrowNode(function.BlockDiagram, BorrowMode.Immutable, 1, true, true);
            Wire.Create(function.BlockDiagram, outputOwner.OutputTerminals[0], borrow.InputTerminals[0]);
            FunctionalNode immutablePassthrough1 = new FunctionalNode(function.BlockDiagram, Signatures.ImmutablePassthroughType);
            FunctionalNode immutablePassthrough2 = new FunctionalNode(function.BlockDiagram, Signatures.ImmutablePassthroughType);
            Wire.Create(function.BlockDiagram, borrow.OutputTerminals[0], immutablePassthrough1.InputTerminals[0], immutablePassthrough2.InputTerminals[0]);

            RunCompilationUpToAutomaticNodeInsertion(function);

            var terminateLifetime = function.BlockDiagram.Nodes.OfType<TerminateLifetimeNode>().FirstOrDefault();
            Assert.IsNotNull(terminateLifetime);
            Assert.AreEqual(2, terminateLifetime.InputTerminals.Count);
            Assert.AreEqual(immutablePassthrough1.OutputTerminals[0], terminateLifetime.InputTerminals[0].GetImmediateSourceTerminal());
            Assert.AreEqual(immutablePassthrough2.OutputTerminals[0], terminateLifetime.InputTerminals[1].GetImmediateSourceTerminal());
        }

        [TestMethod]
        public void BorrowNodeIntoBorrowNodeWithUnwiredOutput_AutomaticNodeInsertion_TwoTerminateLifetimeNodesInserted()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode outputOwner = new FunctionalNode(function.BlockDiagram, _outputOwnerSignature);
            ExplicitBorrowNode outerBorrow = new ExplicitBorrowNode(function.BlockDiagram, BorrowMode.Immutable, 1, true, true);
            Wire.Create(function.BlockDiagram, outputOwner.OutputTerminals[0], outerBorrow.InputTerminals[0]);
            ExplicitBorrowNode innerBorrow = new ExplicitBorrowNode(function.BlockDiagram, BorrowMode.Immutable, 1, true, true);
            Wire.Create(function.BlockDiagram, outerBorrow.OutputTerminals[0], innerBorrow.InputTerminals[0]);

            RunCompilationUpToAutomaticNodeInsertion(function);

            var innerTerminateLifetime = function.BlockDiagram.Nodes.OfType<TerminateLifetimeNode>().FirstOrDefault(
                t => t.InputTerminals[0].GetImmediateSourceTerminal() == innerBorrow.OutputTerminals[0]);
            Assert.IsNotNull(innerTerminateLifetime);
            var outerTerminateLifetime = function.BlockDiagram.Nodes.OfType<TerminateLifetimeNode>().FirstOrDefault(
                t => t.InputTerminals[0].GetImmediateSourceTerminal() == innerTerminateLifetime.OutputTerminals[0]);
            Assert.IsNotNull(outerTerminateLifetime);
        }

        [TestMethod]
        public void BorrowNodeIntoFrameTunnelWithUnwiredOutput_AutomaticNodeInsertion_TunnelAndTerminateLifetimeNodeInserted()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode outputOwner = new FunctionalNode(function.BlockDiagram, _outputOwnerSignature);
            ExplicitBorrowNode borrow = new ExplicitBorrowNode(function.BlockDiagram, BorrowMode.Immutable, 1, true, true);
            Wire.Create(function.BlockDiagram, outputOwner.OutputTerminals[0], borrow.InputTerminals[0]);
            Frame frame = Frame.Create(function.BlockDiagram);
            Tunnel tunnel = CreateInputTunnel(frame);
            Wire.Create(function.BlockDiagram, borrow.OutputTerminals[0], tunnel.InputTerminals[0]);

            RunCompilationUpToAutomaticNodeInsertion(function);

            Tunnel outputTunnel = frame.BorderNodes.FirstOrDefault(t => t.Direction == Direction.Output) as Tunnel;
            Assert.IsNotNull(outputTunnel);
            Assert.AreEqual(tunnel.OutputTerminals[0], outputTunnel.InputTerminals[0].GetImmediateSourceTerminal());
            TerminateLifetimeNode terminateLifetime = function.BlockDiagram.Nodes.OfType<TerminateLifetimeNode>().FirstOrDefault();
            Assert.IsNotNull(terminateLifetime);
            Assert.AreEqual(1, terminateLifetime.InputTerminals.Count);
            Assert.AreEqual(outputTunnel.OutputTerminals[0], terminateLifetime.InputTerminals[0].GetImmediateSourceTerminal());
        }
    }
}
