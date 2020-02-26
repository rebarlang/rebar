using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler.Nodes;

namespace Tests.Rebar.Unit.Execution
{
    [TestClass]
    public class PanicExecutionTests : ExecutionTestBase
    {
        [TestMethod]
        public void PanickingUnwrapOptionIntoOutput_Execute_RuntimeRegistersPanic()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode createNoneInteger = CreateNoneOfOptionIntegerType(function.BlockDiagram);
            var unwrapOption = new FunctionalNode(function.BlockDiagram, Signatures.UnwrapOptionType);
            Wire.Create(function.BlockDiagram, createNoneInteger.OutputTerminals[0], unwrapOption.InputTerminals[0]);
            var output = new FunctionalNode(function.BlockDiagram, Signatures.OutputType);
            Wire.Create(function.BlockDiagram, unwrapOption.OutputTerminals[0], output.InputTerminals[0]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            Assert.IsTrue(executionInstance.RuntimeServices.PanicOccurred);
        }

        [TestMethod]
        public void PanickingUnwrapOptionIntoOutput_Execute_NoOutputValue()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode createNoneInteger = CreateNoneOfOptionIntegerType(function.BlockDiagram);
            var unwrapOption = new FunctionalNode(function.BlockDiagram, Signatures.UnwrapOptionType);
            Wire.Create(function.BlockDiagram, createNoneInteger.OutputTerminals[0], unwrapOption.InputTerminals[0]);
            var output = new FunctionalNode(function.BlockDiagram, Signatures.OutputType);
            Wire.Create(function.BlockDiagram, unwrapOption.OutputTerminals[0], output.InputTerminals[0]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            Assert.IsNull(executionInstance.RuntimeServices.LastOutputValue);
        }

        [TestMethod]
        public void NonPanickingUnwrapOptionIntoInspect_Execute_CorrectValue()
        {
            DfirRoot function = DfirRoot.Create();
            var unwrapOption = new FunctionalNode(function.BlockDiagram, Signatures.UnwrapOptionType);
            FunctionalNode some = ConnectSomeConstructorToInputTerminal(unwrapOption.InputTerminals[0]);
            ConnectConstantToInputTerminal(some.InputTerminals[0], PFTypes.Int32, 5, false);
            FunctionalNode inspect = ConnectInspectToOutputTerminal(unwrapOption.OutputTerminals[0]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            byte[] inspectValue = executionInstance.GetLastValueFromInspectNode(inspect);
            AssertByteArrayIsInt32(inspectValue, 5);
        }

        private FunctionalNode CreateNoneOfOptionIntegerType(Diagram parentDiagram)
        {
            var assign = new FunctionalNode(parentDiagram, Signatures.AssignType);
            var someConstructor = ConnectSomeConstructorToInputTerminal(assign.InputTerminals[0], true);
            ConnectConstantToInputTerminal(someConstructor.InputTerminals[0], PFTypes.Int32, false);
            var noneConstructor = new FunctionalNode(parentDiagram, Signatures.NoneConstructorType);
            Wire.Create(parentDiagram, noneConstructor.OutputTerminals[0], assign.InputTerminals[1]);
            return assign;
        }
    }
}
