using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler.Nodes;

namespace Tests.Rebar.Unit.Execution
{
    [TestClass]
    public class VectorExecutionTests : ExecutionTestBase
    {
        [TestMethod]
        public void InitializeVectorAndSliceIndexWithValidIndex_Execute_CorrectElementValue()
        {
            const int elementValue = 5;
            var tuple = CreateInitializeVectorAndSliceIndexFunction(elementValue, 3);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(tuple.Item1);

            byte[] inspectValue = executionInstance.GetLastValueFromInspectNode(tuple.Item2);
            AssertByteArrayIsInt32(inspectValue, elementValue);
        }

        [TestMethod]
        public void InitializeVectorAndSliceIndexWithIndexPastEnd_Execute_NoElementValueReturned()
        {
            const int elementValue = 5;
            var tuple = CreateInitializeVectorAndSliceIndexFunction(elementValue, 5);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(tuple.Item1);

            byte[] inspectValue = executionInstance.GetLastValueFromInspectNode(tuple.Item2);
            AssertByteArrayIsInt32(inspectValue, 0);
        }

        [TestMethod]
        public void InitializeVectorAndSliceIndexWithNegativeIndex_Execute_NoElementValueReturned()
        {
            const int elementValue = 5;
            var tuple = CreateInitializeVectorAndSliceIndexFunction(elementValue, -1);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(tuple.Item1);

            byte[] inspectValue = executionInstance.GetLastValueFromInspectNode(tuple.Item2);
            AssertByteArrayIsInt32(inspectValue, 0);
        }

        private Tuple<DfirRoot, FunctionalNode> CreateInitializeVectorAndSliceIndexFunction(int elementValue, int index)
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode initializeVector = new FunctionalNode(function.BlockDiagram, Signatures.VectorInitializeType);
            ConnectConstantToInputTerminal(initializeVector.InputTerminals[0], PFTypes.Int32, elementValue, false);
            ConnectConstantToInputTerminal(initializeVector.InputTerminals[1], PFTypes.Int32, 4, false);
            FunctionalNode vectorToSlice = new FunctionalNode(function.BlockDiagram, Signatures.VectorToSliceType);
            Wire.Create(function.BlockDiagram, initializeVector.OutputTerminals[0], vectorToSlice.InputTerminals[0]);
            FunctionalNode sliceIndex = new FunctionalNode(function.BlockDiagram, Signatures.SliceIndexType);
            ConnectConstantToInputTerminal(sliceIndex.InputTerminals[0], PFTypes.Int32, index, false);
            Wire.Create(function.BlockDiagram, vectorToSlice.OutputTerminals[0], sliceIndex.InputTerminals[1]);
            Frame frame = Frame.Create(function.BlockDiagram);
            UnwrapOptionTunnel unwrapOption = new UnwrapOptionTunnel(frame);
            Wire.Create(function.BlockDiagram, sliceIndex.OutputTerminals[1], unwrapOption.InputTerminals[0]);
            FunctionalNode inspect = new FunctionalNode(frame.Diagram, Signatures.InspectType);
            Wire.Create(frame.Diagram, unwrapOption.OutputTerminals[0], inspect.InputTerminals[0]);
            return new Tuple<DfirRoot, FunctionalNode>(function, inspect);
        }
    }
}
