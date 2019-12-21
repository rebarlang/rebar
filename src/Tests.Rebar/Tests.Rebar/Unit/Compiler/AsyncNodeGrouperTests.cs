using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler.Nodes;
using Rebar.RebarTarget;

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

        private IEnumerable<AsyncStateGroup> GroupAsyncStates(DfirRoot function)
        {
            ExecutionOrderSortingVisitor.SortDiagrams(function);
            var asyncStateGrouper = new AsyncStateGrouper();
            asyncStateGrouper.Execute(function, new NationalInstruments.Compiler.CompileCancellationToken());
            return asyncStateGrouper.GetAsyncStateGroups();
        }
    }
}
