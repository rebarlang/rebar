using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler.Nodes;
using Rebar.RebarTarget;
using Loop = Rebar.Compiler.Nodes.Loop;

namespace Tests.Rebar.Unit.Compiler
{
    [TestClass]
    public class AsyncNodeGrouperTests : CompilerTestBase
    {
        [TestMethod]
        public void UnconditionalFrameWithNoInteriorAwaits_GroupAsyncStates_AllFrameAsyncStateGroupsInSameFunction()
        {
            DfirRoot function = DfirRoot.Create();
            Frame frame = Frame.Create(function.BlockDiagram);
            Tunnel inputTunnel = CreateInputTunnel(frame);
            ConnectConstantToInputTerminal(inputTunnel.InputTerminals[0], NITypes.Int32, false);
            var output = new FunctionalNode(frame.Diagram, Signatures.OutputType);
            Wire.Create(frame.Diagram, inputTunnel.OutputTerminals[0], output.InputTerminals[0]);

            IEnumerable<AsyncStateGroup> asyncStateGroups = GroupAsyncStates(function);

            AsyncStateGroup firstGroup = asyncStateGroups.First();
            var groupFunctionId = firstGroup.FunctionId;
            Assert.IsTrue(asyncStateGroups.All(g => g.FunctionId == groupFunctionId));
        }

        [TestMethod]
        public void FrameWithUnwrapOptionTunnelAndNoInteriorAwaits_GroupAsyncStates_AllFrameAsyncStateGroupsInSameFunction()
        {
            DfirRoot function = DfirRoot.Create();
            Frame frame = Frame.Create(function.BlockDiagram);
            UnwrapOptionTunnel unwrapTunnel = CreateUnwrapOptionTunnel(frame);
            FunctionalNode someConstructor = ConnectSomeConstructorToInputTerminal(unwrapTunnel.InputTerminals[0]);
            ConnectConstantToInputTerminal(someConstructor.InputTerminals[0], NITypes.Int32, false);
            var output = new FunctionalNode(frame.Diagram, Signatures.OutputType);
            Wire.Create(frame.Diagram, unwrapTunnel.OutputTerminals[0], output.InputTerminals[0]);

            IEnumerable<AsyncStateGroup> asyncStateGroups = GroupAsyncStates(function);

            AsyncStateGroup frameInitialGroup = asyncStateGroups.First(g => g.GroupContainsStructureTraversalPoint(frame, frame.Diagram, StructureTraversalPoint.BeforeLeftBorderNodes)),
                frameDiagramInitialGroup = asyncStateGroups.First(g => g.GroupContainsNode(output)),
                frameTerminalGroup = asyncStateGroups.First(g => g.GroupContainsStructureTraversalPoint(frame, frame.Diagram, StructureTraversalPoint.AfterRightBorderNodes));
            Assert.AreEqual(frameInitialGroup.FunctionId, frameDiagramInitialGroup.FunctionId);
            Assert.AreEqual(frameInitialGroup.FunctionId, frameTerminalGroup.FunctionId);
        }

        [TestMethod]
        public void FrameContainingPromiseIntoAwait_GroupAsyncStates_Stuff()
        {
            DfirRoot function = DfirRoot.Create();
            Frame frame = Frame.Create(function.BlockDiagram);
            Tunnel inputTunnel = CreateInputTunnel(frame),
                outputTunnel = CreateOutputTunnel(frame);
            var createYieldPromise = new FunctionalNode(frame.Diagram, Signatures.CreateYieldPromiseType);
            var awaitNode = new AwaitNode(frame.Diagram);
            Wire.Create(frame.Diagram, inputTunnel.OutputTerminals[0], createYieldPromise.InputTerminals[0]);
            Wire.Create(frame.Diagram, createYieldPromise.OutputTerminals[0], awaitNode.InputTerminal);
            Wire.Create(frame.Diagram, awaitNode.OutputTerminal, outputTunnel.InputTerminals[0]);
            Constant constant = ConnectConstantToInputTerminal(inputTunnel.InputTerminals[0], NITypes.Int32, false);
            FunctionalNode inspect = new FunctionalNode(function.BlockDiagram, Signatures.ImmutablePassthroughType);
            Wire.Create(function.BlockDiagram, outputTunnel.OutputTerminals[0], inspect.InputTerminals[0]);

            IEnumerable<AsyncStateGroup> asyncStateGroups = GroupAsyncStates(function);
        }

        [TestMethod]
        public void LoopFollowedByLoop_GroupAsyncStates_SubsequentLoopStartsInPredecessorLoopTerminalGroup()
        {
            DfirRoot function = DfirRoot.Create();
            Loop firstLoop = new Loop(function.BlockDiagram);
            BorrowTunnel firstLoopBorrow = CreateBorrowTunnel(firstLoop, BorrowMode.Immutable);
            ConnectConstantToInputTerminal(firstLoopBorrow.InputTerminals[0], NITypes.Int32, false);
            TerminateLifetimeTunnel firstLoopTerminate = firstLoopBorrow.TerminateLifetimeTunnel;
            Loop secondLoop = new Loop(function.BlockDiagram);
            Tunnel loopTunnel = CreateInputTunnel(secondLoop);
            Wire.Create(function.BlockDiagram, firstLoopTerminate.OutputTerminals[0], loopTunnel.InputTerminals[0]);

            IEnumerable<AsyncStateGroup> asyncStateGroups = GroupAsyncStates(function);

            string terminalGroupName = $"loop{firstLoop.UniqueId}_terminalGroup";
            AsyncStateGroup firstLoopTerminalGroup = asyncStateGroups.First(g => g.Label == terminalGroupName);
            AsyncStateGroup secondLoopInitialGroup = asyncStateGroups.First(g => g.GroupContainsStructureTraversalPoint(secondLoop, secondLoop.Diagram, StructureTraversalPoint.BeforeLeftBorderNodes));
            Assert.AreEqual(firstLoopTerminalGroup, secondLoopInitialGroup);
        }

        private IEnumerable<AsyncStateGroup> GroupAsyncStates(DfirRoot function)
        {
            ExecutionOrderSortingVisitor.SortDiagrams(function);
            var asyncStateGrouper = new AsyncStateGrouper();
            asyncStateGrouper.Execute(function, new NationalInstruments.Compiler.CompileCancellationToken());
            return asyncStateGrouper.GetAsyncStateGroups();
        }
    }
}
