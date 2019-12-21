using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler.Nodes;

namespace Tests.Rebar.Unit.Execution
{
    [TestClass]
    public class AsyncExecutionTests : ExecutionTestBase
    {
        [TestMethod]
        public void CreateYieldPromiseAndAwait_Execute_ExecutionFinishesAndYieldedValueReadFromInspect()
        {
            DfirRoot function = DfirRoot.Create();
            var yieldNode = new FunctionalNode(function.BlockDiagram, Signatures.YieldType);
            ConnectConstantToInputTerminal(yieldNode.InputTerminals[0], PFTypes.Int32, 5, false);
            FunctionalNode inspect = ConnectInspectToOutputTerminal(yieldNode.OutputTerminals[0]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            byte[] inspectValue = executionInstance.GetLastValueFromInspectNode(inspect);
            AssertByteArrayIsInt32(inspectValue, 5);
        }

        [TestMethod]
        public void CreateParallelYieldPromisesAwaitAndSumResults_ExecutionFinishesAndSumReadFromInspect()
        {
            DfirRoot function = DfirRoot.Create();
            var yieldNode0 = new FunctionalNode(function.BlockDiagram, Signatures.YieldType);
            ConnectConstantToInputTerminal(yieldNode0.InputTerminals[0], PFTypes.Int32, 5, false);
            var yieldNode1 = new FunctionalNode(function.BlockDiagram, Signatures.YieldType);
            ConnectConstantToInputTerminal(yieldNode1.InputTerminals[0], PFTypes.Int32, 6, false);
            var add = new FunctionalNode(function.BlockDiagram, Signatures.DefinePureBinaryFunction("Add", PFTypes.Int32, PFTypes.Int32));
            Wire.Create(function.BlockDiagram, yieldNode0.OutputTerminals[0], add.InputTerminals[0]);
            Wire.Create(function.BlockDiagram, yieldNode1.OutputTerminals[0], add.InputTerminals[1]);
            FunctionalNode inspect = ConnectInspectToOutputTerminal(add.OutputTerminals[2]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            byte[] inspectValue = executionInstance.GetLastValueFromInspectNode(inspect);
            AssertByteArrayIsInt32(inspectValue, 11);
        }

        [TestMethod]
        public void UnwrapOptionTunnelWithSomeInputAndYieldingFrameInterior_Execute_FrameExecutes()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode some = new FunctionalNode(function.BlockDiagram, Signatures.SomeConstructorType);
            ConnectConstantToInputTerminal(some.InputTerminals[0], PFTypes.Int32, 5, false);
            Frame frame = Frame.Create(function.BlockDiagram);
            UnwrapOptionTunnel unwrapOptionTunnel = new UnwrapOptionTunnel(frame);
            Wire.Create(function.BlockDiagram, some.OutputTerminals[0], unwrapOptionTunnel.InputTerminals[0]);
            BorrowTunnel borrowTunnel = CreateBorrowTunnel(frame, BorrowMode.Mutable);
            ConnectConstantToInputTerminal(borrowTunnel.InputTerminals[0], PFTypes.Int32, 0, true);
            FunctionalNode assign = new FunctionalNode(frame.Diagram, Signatures.AssignType);
            var yieldNode = new FunctionalNode(frame.Diagram, Signatures.YieldType);
            Wire.Create(frame.Diagram, borrowTunnel.OutputTerminals[0], yieldNode.InputTerminals[0]);
            Wire.Create(frame.Diagram, yieldNode.OutputTerminals[0], assign.InputTerminals[0]);
            Wire.Create(frame.Diagram, unwrapOptionTunnel.OutputTerminals[0], assign.InputTerminals[1]);
            FunctionalNode inspect = ConnectInspectToOutputTerminal(borrowTunnel.TerminateLifetimeTunnel.OutputTerminals[0]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            byte[] finalValue = executionInstance.GetLastValueFromInspectNode(inspect);
            AssertByteArrayIsInt32(finalValue, 5);
        }

        [TestMethod]
        public void UnwrapOptionTunnelWithNoneInputAndYieldingFrameInterior_Execute_FrameDoesNotExecute()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode none = new FunctionalNode(function.BlockDiagram, Signatures.NoneConstructorType);
            Frame frame = Frame.Create(function.BlockDiagram);
            UnwrapOptionTunnel unwrapOptionTunnel = new UnwrapOptionTunnel(frame);
            Wire.Create(function.BlockDiagram, none.OutputTerminals[0], unwrapOptionTunnel.InputTerminals[0]);
            BorrowTunnel borrowTunnel = CreateBorrowTunnel(frame, BorrowMode.Mutable);
            ConnectConstantToInputTerminal(borrowTunnel.InputTerminals[0], PFTypes.Int32, 0, true);
            FunctionalNode assign = new FunctionalNode(frame.Diagram, Signatures.AssignType);
            var yieldNode = new FunctionalNode(frame.Diagram, Signatures.YieldType);
            Wire.Create(frame.Diagram, borrowTunnel.OutputTerminals[0], yieldNode.InputTerminals[0]);
            Wire.Create(frame.Diagram, yieldNode.OutputTerminals[0], assign.InputTerminals[0]);
            Wire.Create(frame.Diagram, unwrapOptionTunnel.OutputTerminals[0], assign.InputTerminals[1]);
            FunctionalNode inspect = ConnectInspectToOutputTerminal(borrowTunnel.TerminateLifetimeTunnel.OutputTerminals[0]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            byte[] finalValue = executionInstance.GetLastValueFromInspectNode(inspect);
            AssertByteArrayIsInt32(finalValue, 0);
        }
    }
}
