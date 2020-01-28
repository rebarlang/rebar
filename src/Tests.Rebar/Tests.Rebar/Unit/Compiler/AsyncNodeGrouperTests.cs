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
            Constant constant = ConnectConstantToInputTerminal(inputTunnel.InputTerminals[0], PFTypes.Int32, false);
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
            ConnectConstantToInputTerminal(firstLoopBorrow.InputTerminals[0], PFTypes.Int32, false);
            TerminateLifetimeTunnel firstLoopTerminate = firstLoopBorrow.TerminateLifetimeTunnel;
            Loop secondLoop = new Loop(function.BlockDiagram);
            Tunnel loopTunnel = CreateInputTunnel(secondLoop);
            Wire.Create(function.BlockDiagram, firstLoopTerminate.OutputTerminals[0], loopTunnel.InputTerminals[0]);

            IEnumerable<AsyncStateGroup> asyncStateGroups = GroupAsyncStates(function);

            string terminalGroupName = $"loop{firstLoop.UniqueId}_terminalGroup";
            AsyncStateGroup firstLoopTerminalGroup = asyncStateGroups.First(g => g.Label == terminalGroupName);
            AsyncStateGroup secondLoopInitialGroup = asyncStateGroups.First(g => g.Visitations.Any(
                v =>
                {
                    var structureVisitation = v as StructureVisitation;
                    return structureVisitation != null
                        && structureVisitation.Structure == secondLoop
                        && structureVisitation.TraversalPoint == StructureTraversalPoint.BeforeLeftBorderNodes;
                }));
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
