using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler.Nodes;
using Rebar.RebarTarget.Execution;

namespace Tests.Rebar.Unit.Execution
{
    [TestClass]
    public class BasicExecutionTesting : ExecutionTestBase
    {
        [TestMethod]
        public void FunctionWithInt32Constant_Execute_CorrectValue()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode inspect = new FunctionalNode(function.BlockDiagram, Signatures.InspectType);
            Constant constant = ConnectConstantToInputTerminal(inspect.InputTerminals[0], PFTypes.Int32, false);
            constant.Value = 1;

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            byte[] inspectValue = executionInstance.GetLastValueFromInspectNode(inspect);
            AssertByteArrayIsInt32(inspectValue, 1);
        }

        [TestMethod]
        public void FunctionWithInt32AssignedToNewValue_Execute_CorrectFinalValue()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode inspect = new FunctionalNode(function.BlockDiagram, Signatures.InspectType);
            FunctionalNode assign = new FunctionalNode(function.BlockDiagram, Signatures.AssignType);
            Wire.Create(function.BlockDiagram, assign.OutputTerminals[0], inspect.InputTerminals[0]);
            Constant finalValue = ConnectConstantToInputTerminal(assign.InputTerminals[1], PFTypes.Int32, false);
            finalValue.Value = 2;
            Constant initialValue = ConnectConstantToInputTerminal(assign.InputTerminals[0], PFTypes.Int32, true);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            byte[] inspectValue = executionInstance.GetLastValueFromInspectNode(inspect);
            AssertByteArrayIsInt32(inspectValue, 2);
        }

        [TestMethod]
        public void FunctionWithRangeWithInt32Inputs_Execute_CorrectRangeValue()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode range = new FunctionalNode(function.BlockDiagram, Signatures.RangeType);
            Constant lowValue = ConnectConstantToInputTerminal(range.InputTerminals[0], PFTypes.Int32, false);
            lowValue.Value = 0;
            Constant highValue = ConnectConstantToInputTerminal(range.InputTerminals[1], PFTypes.Int32, false);
            highValue.Value = 10;
            FunctionalNode inspect = ConnectInspectToOutputTerminal(range.OutputTerminals[0]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            byte[] inspectValue = executionInstance.GetLastValueFromInspectNode(inspect);
            Assert.AreEqual(8, inspectValue.Length);
            Assert.AreEqual(-1, DataHelpers.ReadIntFromByteArray(inspectValue, 0));
            Assert.AreEqual(10, DataHelpers.ReadIntFromByteArray(inspectValue, 4));
        }
    }
}
