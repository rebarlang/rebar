using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler.Nodes;
using Rebar.RebarTarget.Execution;

namespace Tests.Rebar.Unit.Execution
{
    [TestClass]
    public class StringExecutionTests : ExecutionTestBase
    {
        [TestMethod]
        public void BranchedStringSliceWire_Execute_BothSinksHaveCorrectValues()
        {
            DfirRoot function = DfirRoot.Create();
            Constant stringSliceConstant = Constant.Create(function.BlockDiagram, "test", DataTypes.StringSliceType.CreateImmutableReference());
            var inspect1Node = new FunctionalNode(function.BlockDiagram, Signatures.InspectType);
            ExplicitBorrowNode borrow1 = ConnectExplicitBorrowToInputTerminals(inspect1Node.InputTerminals[0]);
            var inspect2Node = new FunctionalNode(function.BlockDiagram, Signatures.InspectType);
            ExplicitBorrowNode borrow2 = ConnectExplicitBorrowToInputTerminals(inspect2Node.InputTerminals[0]);
            Wire.Create(function.BlockDiagram, stringSliceConstant.OutputTerminal, borrow1.InputTerminals[0], borrow2.InputTerminals[0]);

            ExecutionContext context = CompileAndExecuteFunction(function);

            byte[] inspect1Value = GetLastValueFromInspectNode(context, inspect1Node);
            Assert.AreEqual(8, inspect1Value.Length);
            byte[] inspect2Value = GetLastValueFromInspectNode(context, inspect2Node);
            Assert.AreEqual(8, inspect2Value.Length);
            Assert.IsTrue(inspect1Value.Zip(inspect2Value, (a, b) => a == b).All(b => b));
        }

        [TestMethod]
        public void StringSliceConstantToOutput_Execute_CorrectStringOutput()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode output = new FunctionalNode(function.BlockDiagram, Signatures.OutputType);
            FunctionalNode stringFromSlice = new FunctionalNode(function.BlockDiagram, Signatures.StringFromSliceType);
            Wire.Create(function.BlockDiagram, stringFromSlice.OutputTerminals[1], output.InputTerminals[0]);
            Constant stringConstant = ConnectConstantToInputTerminal(stringFromSlice.InputTerminals[0], DataTypes.StringSliceType.CreateImmutableReference(), false);
            stringConstant.Value = "test";

            var runtimeServices = new TestRuntimeServices();
            ExecutionContext context = CompileAndExecuteFunction(function, runtimeServices);

            Assert.AreEqual("test", runtimeServices.LastOutputValue);
        }

        [TestMethod]
        public void StringToStringSliceToOutput_Execute_CorrectStringOutput()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode output = new FunctionalNode(function.BlockDiagram, Signatures.OutputType);
            FunctionalNode stringToSlice = new FunctionalNode(function.BlockDiagram, Signatures.StringToSliceType);
            Wire.Create(function.BlockDiagram, stringToSlice.OutputTerminals[0], output.InputTerminals[0]);
            FunctionalNode stringFromSlice = new FunctionalNode(function.BlockDiagram, Signatures.StringFromSliceType);
            Wire.Create(function.BlockDiagram, stringFromSlice.OutputTerminals[1], stringToSlice.InputTerminals[0]);
            Constant stringConstant = ConnectConstantToInputTerminal(stringFromSlice.InputTerminals[0], DataTypes.StringSliceType.CreateImmutableReference(), false);
            stringConstant.Value = "test";

            var runtimeServices = new TestRuntimeServices();
            ExecutionContext context = CompileAndExecuteFunction(function, runtimeServices);

            Assert.AreEqual("test", runtimeServices.LastOutputValue);
        }
    }
}
