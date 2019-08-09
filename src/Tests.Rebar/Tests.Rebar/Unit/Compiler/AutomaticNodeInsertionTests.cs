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
        public void OwnerWireConnectedToReferenceInputTerminal_AutomaticNodeInsertion_BorrowNodeAndTerminateLifetimeNodeInserted()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode outputOwner = new FunctionalNode(function.BlockDiagram, _outputOwnerSignature);
            FunctionalNode immutablePassthrough = new FunctionalNode(function.BlockDiagram, Signatures.ImmutablePassthroughType);
            Wire.Create(function.BlockDiagram, outputOwner.OutputTerminals[0], immutablePassthrough.InputTerminals[0]);

            RunCompilationUpToAutomaticNodeInsertion(function);

            var borrowNode = function.BlockDiagram.Nodes.OfType<ExplicitBorrowNode>().FirstOrDefault();
            Assert.IsNotNull(borrowNode);
            Assert.AreEqual(1, borrowNode.InputTerminals.Count);
            Assert.AreEqual(outputOwner.OutputTerminals[0], borrowNode.InputTerminals[0].GetImmediateSourceTerminal());
            Assert.AreEqual(borrowNode.OutputTerminals[0], immutablePassthrough.InputTerminals[0].GetImmediateSourceTerminal());
            TerminateLifetimeNode terminateLifetime = AssertDiagramContainsTerminateLifetimeWithSources(function.BlockDiagram, immutablePassthrough.OutputTerminals[0]);
            AssertDiagramContainsDropWithSource(function.BlockDiagram, terminateLifetime.OutputTerminals[0]);
        }

        [TestMethod]
        public void BorrowNodeWithUnwiredOutput_AutomaticNodeInsertion_TerminateLifetimeNodeInserted()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode outputOwner = new FunctionalNode(function.BlockDiagram, _outputOwnerSignature);
            ExplicitBorrowNode borrow = new ExplicitBorrowNode(function.BlockDiagram, BorrowMode.Immutable, 1, true, true);
            Wire.Create(function.BlockDiagram, outputOwner.OutputTerminals[0], borrow.InputTerminals[0]);

            RunCompilationUpToAutomaticNodeInsertion(function);

            AssertDiagramContainsTerminateLifetimeWithSources(function.BlockDiagram, borrow.OutputTerminals[0]);
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

            AssertDiagramContainsTerminateLifetimeWithSources(function.BlockDiagram, immutablePassthrough.OutputTerminals[0]);
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

            AssertDiagramContainsTerminateLifetimeWithSources(function.BlockDiagram, immutablePassthrough1.OutputTerminals[0], immutablePassthrough2.OutputTerminals[0]);
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
            AssertDiagramContainsTerminateLifetimeWithSources(function.BlockDiagram, outputTunnel.OutputTerminals[0]);
        }

        [TestMethod]
        public void UnconsumedOwnerVariable_AutomaticNodeInsertion_DropNodeInserted()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode outputOwner = new FunctionalNode(function.BlockDiagram, _outputOwnerSignature);

            RunCompilationUpToAutomaticNodeInsertion(function);

            AssertDiagramContainsDropWithSource(function.BlockDiagram, outputOwner.OutputTerminals[0]);
        }

        [TestMethod]
        public void BorrowNodeWithUnwiredOutput_AutomaticNodeInsertion_DropNodeInsertedDownstreamOfTerminateLifetime()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode outputOwner = new FunctionalNode(function.BlockDiagram, _outputOwnerSignature);
            ExplicitBorrowNode borrow = new ExplicitBorrowNode(function.BlockDiagram, BorrowMode.Immutable, 1, true, true);
            Wire.Create(function.BlockDiagram, outputOwner.OutputTerminals[0], borrow.InputTerminals[0]);

            RunCompilationUpToAutomaticNodeInsertion(function);

            var terminateLifetime = AssertDiagramContainsTerminateLifetimeWithSources(function.BlockDiagram, borrow.OutputTerminals[0]);
            AssertDiagramContainsDropWithSource(function.BlockDiagram, terminateLifetime.OutputTerminals[0]);
        }

        private TerminateLifetimeNode AssertDiagramContainsTerminateLifetimeWithSources(Diagram diagram, params Terminal[] sources)
        {
            TerminateLifetimeNode terminateLifetime = diagram.Nodes.OfType<TerminateLifetimeNode>().FirstOrDefault();
            Assert.IsNotNull(terminateLifetime);
            Assert.AreEqual(sources.Length, terminateLifetime.InputTerminals.Count);
            for (int i = 0; i < sources.Length; ++i)
            {
                Assert.AreEqual(sources[i], terminateLifetime.InputTerminals[i].GetImmediateSourceTerminal());
            }
            return terminateLifetime;
        }

        private DropNode AssertDiagramContainsDropWithSource(Diagram diagram, Terminal source)
        {
            var drop = diagram.Nodes.OfType<DropNode>().FirstOrDefault();
            Assert.IsNotNull(drop);
            Assert.AreEqual(source, drop.InputTerminals[0].GetImmediateSourceTerminal());
            return drop;
        }
    }
}
