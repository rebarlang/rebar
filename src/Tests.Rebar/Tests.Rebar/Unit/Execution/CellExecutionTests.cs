using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler.Nodes;

namespace Tests.Rebar.Unit.Execution
{
    [TestClass]
    public class CellExecutionTests : ExecutionTestBase
    {
        [TestMethod]
        public void CreateSharedAndCloneAndDereferenceBothCopies_Execute_CorrectDereferencedValues()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode createShared = new FunctionalNode(function.BlockDiagram, Signatures.SharedCreateType);
            ConnectConstantToInputTerminal(createShared.InputTerminals[0], PFTypes.Int32, 5, false);
            FunctionalNode createCopy = new FunctionalNode(function.BlockDiagram, Signatures.CreateCopyType);
            Wire.Create(function.BlockDiagram, createShared.OutputTerminals[0], createCopy.InputTerminals[0]);
            FunctionalNode getCellValue0 = new FunctionalNode(function.BlockDiagram, Signatures.SharedGetValueType),
                getCellValue1 = new FunctionalNode(function.BlockDiagram, Signatures.SharedGetValueType);
            Wire.Create(function.BlockDiagram, createCopy.OutputTerminals[0], getCellValue0.InputTerminals[0]);
            Wire.Create(function.BlockDiagram, createCopy.OutputTerminals[1], getCellValue1.InputTerminals[0]);
            FunctionalNode inspect0 = ConnectInspectToOutputTerminal(getCellValue0.OutputTerminals[0]),
                inspect1 = ConnectInspectToOutputTerminal(getCellValue1.OutputTerminals[0]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            byte[] inspectValue = executionInstance.GetLastValueFromInspectNode(inspect0);
            AssertByteArrayIsInt32(inspectValue, 5);
            inspectValue = executionInstance.GetLastValueFromInspectNode(inspect1);
            AssertByteArrayIsInt32(inspectValue, 5);
        }

        [TestMethod]
        public void CreateSharedOfDroppableValueAndClone_Execute_InnerValueDropped()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode fakeDropCreate = CreateFakeDropWithId(function.BlockDiagram, 1);
            FunctionalNode createShared = new FunctionalNode(function.BlockDiagram, Signatures.SharedCreateType);
            Wire.Create(function.BlockDiagram, fakeDropCreate.OutputTerminals[0], createShared.InputTerminals[0]);
            FunctionalNode createCopy = new FunctionalNode(function.BlockDiagram, Signatures.CreateCopyType);
            Wire.Create(function.BlockDiagram, createShared.OutputTerminals[0], createCopy.InputTerminals[0]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            Assert.IsTrue(executionInstance.RuntimeServices.DroppedFakeDropIds.Contains(1));
        }
    }
}
