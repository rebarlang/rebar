using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler.Nodes;
using Rebar.RebarTarget;

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
            Constant constant = ConnectConstantToInputTerminal(inspect.InputTerminals[0], PFTypes.Int32, 1, false);

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
            Constant finalValue = ConnectConstantToInputTerminal(assign.InputTerminals[1], PFTypes.Int32, 2, false);
            Constant initialValue = ConnectConstantToInputTerminal(assign.InputTerminals[0], PFTypes.Int32, true);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            byte[] inspectValue = executionInstance.GetLastValueFromInspectNode(inspect);
            AssertByteArrayIsInt32(inspectValue, 2);
        }

        [TestMethod]
        public void FunctionWithTwoI32sExchanged_Execute_CorrectFinalValues()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode exchangeValues = new FunctionalNode(function.BlockDiagram, Signatures.ExchangeValuesType);
            FunctionalNode inspect1 = ConnectInspectToOutputTerminal(exchangeValues.OutputTerminals[0]),
                inspect2 = ConnectInspectToOutputTerminal(exchangeValues.OutputTerminals[1]);
            Constant value1 = ConnectConstantToInputTerminal(exchangeValues.InputTerminals[0], PFTypes.Int32, 1, true),
                value2 = ConnectConstantToInputTerminal(exchangeValues.InputTerminals[1], PFTypes.Int32, 2, true);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            byte[] inspectValue1 = executionInstance.GetLastValueFromInspectNode(inspect1),
                inspectValue2 = executionInstance.GetLastValueFromInspectNode(inspect2);
            AssertByteArrayIsInt32(inspectValue1, 2);
            AssertByteArrayIsInt32(inspectValue2, 1);
        }

        [TestMethod]
        public void SelectReferenceWithTrueSelector_Execute_CorrectSelectedResult()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode selectReference = new FunctionalNode(function.BlockDiagram, Signatures.SelectReferenceType);
            Constant selectorConstant = ConnectConstantToInputTerminal(selectReference.InputTerminals[0], PFTypes.Boolean, true, false);
            Constant trueValueConstant = ConnectConstantToInputTerminal(selectReference.InputTerminals[1], PFTypes.Int32, 1, false);
            Constant falseValueConstant = ConnectConstantToInputTerminal(selectReference.InputTerminals[2], PFTypes.Int32, 0, false);
            FunctionalNode inspect = ConnectInspectToOutputTerminal(selectReference.OutputTerminals[1]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            byte[] inspectValue = executionInstance.GetLastValueFromInspectNode(inspect);
            AssertByteArrayIsInt32(inspectValue, 1);
        }

        [TestMethod]
        public void SelectReferenceWithFalseSelector_Execute_CorrectSelectedResult()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode selectReference = new FunctionalNode(function.BlockDiagram, Signatures.SelectReferenceType);
            Constant selectorConstant = ConnectConstantToInputTerminal(selectReference.InputTerminals[0], PFTypes.Boolean, false, false);
            Constant trueValueConstant = ConnectConstantToInputTerminal(selectReference.InputTerminals[1], PFTypes.Int32, 1, false);
            Constant falseValueConstant = ConnectConstantToInputTerminal(selectReference.InputTerminals[2], PFTypes.Int32, 0, false);
            FunctionalNode inspect = ConnectInspectToOutputTerminal(selectReference.OutputTerminals[1]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            byte[] inspectValue = executionInstance.GetLastValueFromInspectNode(inspect);
            AssertByteArrayIsInt32(inspectValue, 0);
        }

        [TestMethod]
        public void FunctionWithRangeWithInt32Inputs_Execute_CorrectRangeValue()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode range = new FunctionalNode(function.BlockDiagram, Signatures.RangeType);
            Constant lowValue = ConnectConstantToInputTerminal(range.InputTerminals[0], PFTypes.Int32, 0, false);
            Constant highValue = ConnectConstantToInputTerminal(range.InputTerminals[1], PFTypes.Int32, 10, false);
            FunctionalNode inspect = ConnectInspectToOutputTerminal(range.OutputTerminals[0]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            byte[] inspectValue = executionInstance.GetLastValueFromInspectNode(inspect);
            Assert.AreEqual(8, inspectValue.Length);
            Assert.AreEqual(-1, DataHelpers.ReadIntFromByteArray(inspectValue, 0));
            Assert.AreEqual(10, DataHelpers.ReadIntFromByteArray(inspectValue, 4));
        }
    }
}
