using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler.Nodes;
using Loop = Rebar.Compiler.Nodes.Loop;

namespace Tests.Rebar.Unit.Execution
{
    [TestClass]
    public class LoopExecutionTests : ExecutionTestBase
    {
        [TestMethod]
        public void LoopWithOutputTunnelThatDoesNotExecute_Execute_TunnelOutputsNoneValue()
        {
            DfirRoot function = DfirRoot.Create();
            Loop loop = new Loop(function.BlockDiagram);
            LoopConditionTunnel conditionTunnel = CreateLoopConditionTunnel(loop);
            Constant falseConstant = ConnectConstantToInputTerminal(conditionTunnel.InputTerminals[0], PFTypes.Boolean, false, false);
            Tunnel outputTunnel = CreateOutputTunnel(loop);
            ConnectConstantToInputTerminal(outputTunnel.InputTerminals[0], PFTypes.Int32, false);
            FunctionalNode inspect = ConnectInspectToOutputTerminal(outputTunnel.OutputTerminals[0]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            byte[] inspectValue = executionInstance.GetLastValueFromInspectNode(inspect);
            AssertByteArrayIsNoneInteger(inspectValue);
        }

        [TestMethod]
        public void LoopWithOutputTunnelThatExecutes_Execute_TunnelOutputsSomeValue()
        {
            DfirRoot function = DfirRoot.Create();
            Loop loop = new Loop(function.BlockDiagram);
            LoopConditionTunnel conditionTunnel = CreateLoopConditionTunnel(loop);
            Constant trueConstant = ConnectConstantToInputTerminal(conditionTunnel.InputTerminals[0], PFTypes.Boolean, true, false);
            FunctionalNode assign = new FunctionalNode(loop.Diagrams[0], Signatures.AssignType);
            Wire.Create(loop.Diagrams[0], conditionTunnel.OutputTerminals[0], assign.InputTerminals[0]);
            Constant falseConstant = ConnectConstantToInputTerminal(assign.InputTerminals[1], PFTypes.Boolean, false, false);
            Tunnel outputTunnel = CreateOutputTunnel(loop);
            Constant intConstant = ConnectConstantToInputTerminal(outputTunnel.InputTerminals[0], PFTypes.Int32, 5, false);
            FunctionalNode inspect = ConnectInspectToOutputTerminal(outputTunnel.OutputTerminals[0]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            byte[] inspectValue = executionInstance.GetLastValueFromInspectNode(inspect);
            AssertByteArrayIsSomeInteger(inspectValue, 5);
        }
    }
}
