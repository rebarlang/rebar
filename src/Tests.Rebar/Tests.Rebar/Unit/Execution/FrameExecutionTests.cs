using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler.Nodes;

namespace Tests.Rebar.Unit.Execution
{
    [TestClass]
    public class FrameExecutionTests : ExecutionTestBase
    {
        [TestMethod]
        public void ReferenceWiredThroughInputAndOutputTunnels_Execute_CompilesAndExecutesCorrectly()
        {
            DfirRoot function = DfirRoot.Create();
            ExplicitBorrowNode borrowNode = new ExplicitBorrowNode(function.BlockDiagram, BorrowMode.Immutable, 1, true, true);
            ConnectConstantToInputTerminal(borrowNode.InputTerminals[0], PFTypes.Int32, 5, false);
            Frame frame = Frame.Create(function.BlockDiagram);
            Tunnel inputTunnel = CreateInputTunnel(frame), outputTunnel = CreateOutputTunnel(frame);
            Wire.Create(function.BlockDiagram, borrowNode.OutputTerminals[0], inputTunnel.InputTerminals[0]);
            Wire.Create(frame.Diagram, inputTunnel.OutputTerminals[0], outputTunnel.InputTerminals[0]);
            FunctionalNode inspect = new FunctionalNode(function.BlockDiagram, Signatures.InspectType);
            Wire.Create(function.BlockDiagram, outputTunnel.OutputTerminals[0], inspect.InputTerminals[0]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            AssertByteArrayIsInt32(executionInstance.GetLastValueFromInspectNode(inspect), 5);
        }
    }
}
