using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler;
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

        private FunctionalNode CreateNoneOfOptionIntegerType(Diagram parentDiagram)
        {
            var someConstructor = new FunctionalNode(parentDiagram, Signatures.SomeConstructorType);
            ConnectConstantToInputTerminal(someConstructor.InputTerminals[0], PFTypes.Int32, false);
            var noneConstructor = new FunctionalNode(parentDiagram, Signatures.NoneConstructorType);
            var assign = new FunctionalNode(parentDiagram, Signatures.AssignType);
            Wire.Create(parentDiagram, someConstructor.OutputTerminals[0], assign.InputTerminals[0])
                .SetWireBeginsMutableVariable(true);
            Wire.Create(parentDiagram, noneConstructor.OutputTerminals[0], assign.InputTerminals[1]);
            return assign;
        }
    }
}
