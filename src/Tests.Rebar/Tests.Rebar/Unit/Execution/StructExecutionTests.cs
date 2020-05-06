using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler.Nodes;

namespace Tests.Rebar.Unit.Execution
{
    [TestClass]
    public class StructExecutionTests : ExecutionTestBase
    {
        [TestMethod]
        public void StructConstructorIntoStructFieldAccessor_Execute_FieldValuesAccessed()
        {
            DfirRoot function = DfirRoot.Create();
            var structConstructorNode = new StructConstructorNode(function.BlockDiagram, StructType);
            ConnectConstantToInputTerminal(structConstructorNode.InputTerminals[0], NITypes.Int32, 5, false);
            ConnectConstantToInputTerminal(structConstructorNode.InputTerminals[1], NITypes.Boolean, true, false);
            var structFieldAccessor = new StructFieldAccessorNode(function.BlockDiagram, new string[] { "_0", "_1" });
            Wire.Create(function.BlockDiagram, structConstructorNode.OutputTerminals[0], structFieldAccessor.StructInputTerminal);
            FunctionalNode inspect0 = ConnectInspectToOutputTerminal(structFieldAccessor.OutputTerminals[0]);
            FunctionalNode inspect1 = ConnectInspectToOutputTerminal(structFieldAccessor.OutputTerminals[1]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            byte[] inspectValue = executionInstance.GetLastValueFromInspectNode(inspect0);
            AssertByteArrayIsInt32(inspectValue, 5);
            inspectValue = executionInstance.GetLastValueFromInspectNode(inspect1);
            AssertByteArrayIsBoolean(inspectValue, true);
        }

        private NIType StructType
        {
            get
            {
                NIClassBuilder builder = NITypes.Factory.DefineValueClass("struct.td");
                builder.DefineField(NITypes.Int32, "_0", NIFieldAccessPolicies.ReadWrite);
                builder.DefineField(NITypes.Boolean, "_1", NIFieldAccessPolicies.ReadWrite);
                return builder.CreateType();
            }
        }

        [TestMethod]
        public void StructConstructorContainingDroppableType_Execute_DroppableValueDropped()
        {
            DfirRoot function = DfirRoot.Create();
            var structConstructorNode = new StructConstructorNode(function.BlockDiagram, FakeDropStructType);
            FunctionalNode fakeDrop = CreateFakeDropWithId(function.BlockDiagram, 1);
            Wire.Create(function.BlockDiagram, fakeDrop.OutputTerminals[0], structConstructorNode.InputTerminals[0]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            Assert.IsTrue(executionInstance.RuntimeServices.DroppedFakeDropIds.Contains(1));
        }

        private NIType FakeDropStructType
        {
            get
            {
                NIClassBuilder builder = NITypes.Factory.DefineValueClass("fakeDropStruct.td");
                builder.DefineField(DataTypes.FakeDropType, "_0", NIFieldAccessPolicies.ReadWrite);
                return builder.CreateType();
            }
        }
    }
}
