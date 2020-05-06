using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Compiler.Nodes;

namespace Tests.Rebar.Unit.Execution
{
    [TestClass]
    public class TupleExecutionTests : ExecutionTestBase
    {
        [TestMethod]
        public void BuildTupleAndDecomposeTupleAsBorrow_Execute_CorrectElementValues()
        {
            DfirRoot function = DfirRoot.Create();
            BuildTupleNode buildTuple = new BuildTupleNode(function.BlockDiagram, 2);
            ConnectConstantToInputTerminal(buildTuple.InputTerminals[0], NITypes.Int32, 1, false);
            ConnectConstantToInputTerminal(buildTuple.InputTerminals[1], NITypes.Boolean, true, false);
            var decomposeTuple = new DecomposeTupleNode(function.BlockDiagram, 2, DecomposeMode.Borrow);
            Wire.Create(function.BlockDiagram, buildTuple.OutputTerminals[0], decomposeTuple.InputTerminals[0]);
            FunctionalNode inspect0 = ConnectInspectToOutputTerminal(decomposeTuple.OutputTerminals[0]),
                inspect1 = ConnectInspectToOutputTerminal(decomposeTuple.OutputTerminals[1]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            byte[] inspectValue = executionInstance.GetLastValueFromInspectNode(inspect0);
            AssertByteArrayIsInt32(inspectValue, 1);
            inspectValue = executionInstance.GetLastValueFromInspectNode(inspect1);
            AssertByteArrayIsBoolean(inspectValue, true);
        }

        [TestMethod]
        public void BuildTupleAndDecomposeTupleAsMove_Execute_CorrectElementValues()
        {
            DfirRoot function = DfirRoot.Create();
            BuildTupleNode buildTuple = new BuildTupleNode(function.BlockDiagram, 2);
            ConnectConstantToInputTerminal(buildTuple.InputTerminals[0], NITypes.Int32, 1, false);
            ConnectConstantToInputTerminal(buildTuple.InputTerminals[1], NITypes.Boolean, true, false);
            var decomposeTuple = new DecomposeTupleNode(function.BlockDiagram, 2, DecomposeMode.Move);
            Wire.Create(function.BlockDiagram, buildTuple.OutputTerminals[0], decomposeTuple.InputTerminals[0]);
            FunctionalNode inspect0 = ConnectInspectToOutputTerminal(decomposeTuple.OutputTerminals[0]),
                inspect1 = ConnectInspectToOutputTerminal(decomposeTuple.OutputTerminals[1]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            byte[] inspectValue = executionInstance.GetLastValueFromInspectNode(inspect0);
            AssertByteArrayIsInt32(inspectValue, 1);
            inspectValue = executionInstance.GetLastValueFromInspectNode(inspect1);
            AssertByteArrayIsBoolean(inspectValue, true);
        }
    }
}
