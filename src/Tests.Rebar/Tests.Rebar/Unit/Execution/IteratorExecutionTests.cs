using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler;
using Rebar.Compiler.Nodes;
using Loop = Rebar.Compiler.Nodes.Loop;

namespace Tests.Rebar.Unit.Execution
{
    [TestClass]
    public class IteratorExecutionTests : ExecutionTestBase
    {
        [TestMethod]
        public void SumItemsFromRangeIterator_Execute_CorrectFinalResult()
        {
            DfirRoot function = DfirRoot.Create();
            Loop loop = new Loop(function.BlockDiagram);
            LoopConditionTunnel conditionTunnel = CreateLoopConditionTunnel(loop);
            IterateTunnel iterateTunnel = CreateIterateTunnel(loop);
            FunctionalNode range = new FunctionalNode(function.BlockDiagram, Signatures.RangeType);
            Wire rangeWire = Wire.Create(function.BlockDiagram, range.OutputTerminals[0], iterateTunnel.InputTerminals[0]);
            rangeWire.SetWireBeginsMutableVariable(true);
            Constant lowConstant = ConnectConstantToInputTerminal(range.InputTerminals[0], PFTypes.Int32, 0, false);
            Constant highConstant = ConnectConstantToInputTerminal(range.InputTerminals[1], PFTypes.Int32, 10, false);
            BorrowTunnel borrow = CreateBorrowTunnel(loop, BorrowMode.Mutable);
            Constant accumulateConstant = ConnectConstantToInputTerminal(borrow.InputTerminals[0], PFTypes.Int32, 0, true);
            FunctionalNode accumulateAdd = new FunctionalNode(loop.Diagram, Signatures.DefineMutatingBinaryFunction("AccumulateAdd", PFTypes.Int32));
            Wire.Create(loop.Diagram, borrow.OutputTerminals[0], accumulateAdd.InputTerminals[0]);
            Wire.Create(loop.Diagram, iterateTunnel.OutputTerminals[0], accumulateAdd.InputTerminals[1]);
            FunctionalNode inspect = ConnectInspectToOutputTerminal(borrow.TerminateLifetimeTunnel.OutputTerminals[0]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            byte[] inspectValue = executionInstance.GetLastValueFromInspectNode(inspect);
            AssertByteArrayIsInt32(inspectValue, 45);
        }
    }
}
