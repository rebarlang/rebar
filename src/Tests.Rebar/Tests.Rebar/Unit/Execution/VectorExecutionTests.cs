using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler;
using Rebar.Compiler.Nodes;
using Loop = Rebar.Compiler.Nodes.Loop;

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

        [TestMethod]
        public void CreateVectorAndAppendTwoDroppableValues_Execute_BothDroppableValuesDropped()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode vectorCreate = new FunctionalNode(function.BlockDiagram, Signatures.VectorCreateType);
            FunctionalNode vectorAppend0 = new FunctionalNode(function.BlockDiagram, Signatures.VectorAppendType),
                vectorAppend1 = new FunctionalNode(function.BlockDiagram, Signatures.VectorAppendType);
            Wire.Create(function.BlockDiagram, vectorCreate.OutputTerminals[0], vectorAppend0.InputTerminals[0])
                .SetWireBeginsMutableVariable(true);
            Wire.Create(function.BlockDiagram, vectorAppend0.OutputTerminals[0], vectorAppend1.InputTerminals[0]);
            FunctionalNode fakeDrop0 = CreateFakeDropWithId(function.BlockDiagram, 1), fakeDrop1 = CreateFakeDropWithId(function.BlockDiagram, 2);
            Wire.Create(function.BlockDiagram, fakeDrop0.OutputTerminals[0], vectorAppend0.InputTerminals[1]);
            Wire.Create(function.BlockDiagram, fakeDrop1.OutputTerminals[0], vectorAppend1.InputTerminals[1]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            Assert.IsTrue(executionInstance.RuntimeServices.DroppedFakeDropIds.Contains(1));
            Assert.IsTrue(executionInstance.RuntimeServices.DroppedFakeDropIds.Contains(2));
        }

        private Tuple<DfirRoot, FunctionalNode> CreateInitializeVectorAndSliceIndexFunction(int elementValue, int index)
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode initializeVector = CreateInitializeVectorWithIntegerConstants(function.BlockDiagram, elementValue, 4);
            FunctionalNode vectorToSlice = new FunctionalNode(function.BlockDiagram, Signatures.VectorToSliceType);
            Wire.Create(function.BlockDiagram, initializeVector.OutputTerminals[0], vectorToSlice.InputTerminals[0]);
            FunctionalNode sliceIndex = new FunctionalNode(function.BlockDiagram, Signatures.SliceIndexType);
            ConnectConstantToInputTerminal(sliceIndex.InputTerminals[0], NITypes.Int32, index, false);
            Wire.Create(function.BlockDiagram, vectorToSlice.OutputTerminals[0], sliceIndex.InputTerminals[1]);
            Frame frame = Frame.Create(function.BlockDiagram);
            UnwrapOptionTunnel unwrapOption = new UnwrapOptionTunnel(frame);
            Wire.Create(function.BlockDiagram, sliceIndex.OutputTerminals[1], unwrapOption.InputTerminals[0]);
            FunctionalNode inspect = new FunctionalNode(frame.Diagram, Signatures.InspectType);
            Wire.Create(frame.Diagram, unwrapOption.OutputTerminals[0], inspect.InputTerminals[0]);
            return new Tuple<DfirRoot, FunctionalNode>(function, inspect);
        }

        [TestMethod]
        public void FillVectorEnoughToGrowItThenRemoveAllElements_Execute_AllElementsPreservedAndRemoved()
        {
            DfirRoot function = DfirRoot.Create();
            var createVector = new FunctionalNode(function.BlockDiagram, Signatures.VectorCreateType);
            Loop firstLoop = new Loop(function.BlockDiagram);
            CreateLoopConditionTunnel(firstLoop);
            BorrowTunnel firstLoopBorrowTunnel = CreateBorrowTunnel(firstLoop, BorrowMode.Mutable);
            Wire.Create(function.BlockDiagram, createVector.OutputTerminals[0], firstLoopBorrowTunnel.InputTerminals[0])
                .SetWireBeginsMutableVariable(true);
            IterateTunnel iterateTunnel = CreateRangeAndIterateTunnel(firstLoop, 1, 7);
            var appendToVector = new FunctionalNode(firstLoop.Diagram, Signatures.VectorAppendType);
            Wire.Create(firstLoop.Diagram, firstLoopBorrowTunnel.OutputTerminals[0], appendToVector.InputTerminals[0]);
            Wire.Create(firstLoop.Diagram, iterateTunnel.OutputTerminals[0], appendToVector.InputTerminals[1]);
            Loop secondLoop = new Loop(function.BlockDiagram);
            CreateLoopConditionTunnel(secondLoop);
            BorrowTunnel secondLoopBorrowTunnel = CreateBorrowTunnel(secondLoop, BorrowMode.Mutable);
            Wire.Create(function.BlockDiagram, firstLoopBorrowTunnel.TerminateLifetimeTunnel.OutputTerminals[0], secondLoopBorrowTunnel.InputTerminals[0]);
            CreateRangeAndIterateTunnel(secondLoop, 1, 7);
            var removeLastFromVector = new FunctionalNode(secondLoop.Diagram, Signatures.VectorRemoveLastType);
            Wire.Create(secondLoop.Diagram, secondLoopBorrowTunnel.OutputTerminals[0], removeLastFromVector.InputTerminals[0]);
            BorrowTunnel resultBorrowTunnel = CreateBorrowTunnel(secondLoop, BorrowMode.Mutable);
            ConnectConstantToInputTerminal(resultBorrowTunnel.InputTerminals[0], NITypes.Int32, 0, true);
            Frame unwrapFrame = Frame.Create(secondLoop.Diagram);
            UnwrapOptionTunnel unwrapTunnel = CreateUnwrapOptionTunnel(unwrapFrame);
            Wire.Create(secondLoop.Diagram, removeLastFromVector.OutputTerminals[1], unwrapTunnel.InputTerminals[0]);
            Tunnel inputTunnel = CreateInputTunnel(unwrapFrame);
            Wire.Create(secondLoop.Diagram, resultBorrowTunnel.OutputTerminals[0], inputTunnel.InputTerminals[0]);
            var accumulateAdd = new FunctionalNode(unwrapFrame.Diagram, Signatures.DefineMutatingBinaryFunction("AccumulateAdd", NITypes.Int32));
            Wire.Create(unwrapFrame.Diagram, inputTunnel.OutputTerminals[0], accumulateAdd.InputTerminals[0]);
            Wire.Create(unwrapFrame.Diagram, unwrapTunnel.OutputTerminals[0], accumulateAdd.InputTerminals[1]);
            FunctionalNode inspect = ConnectInspectToOutputTerminal(resultBorrowTunnel.TerminateLifetimeTunnel.OutputTerminals[0]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            byte[] inspectValue = executionInstance.GetLastValueFromInspectNode(inspect);
            AssertByteArrayIsInt32(inspectValue, 21);
        }

        private IterateTunnel CreateRangeAndIterateTunnel(Loop loop, int rangeLow, int rangeHigh)
        {
            var range = new FunctionalNode(loop.ParentDiagram, Signatures.RangeType);
            ConnectConstantToInputTerminal(range.InputTerminals[0], NITypes.Int32, 1, false);
            ConnectConstantToInputTerminal(range.InputTerminals[1], NITypes.Int32, 7, false);
            IterateTunnel iterateTunnel = CreateIterateTunnel(loop);
            Wire.Create(loop.ParentDiagram, range.OutputTerminals[0], iterateTunnel.InputTerminals[0])
                .SetWireBeginsMutableVariable(true);
            return iterateTunnel;
        }

        private FunctionalNode CreateInitializeVectorWithIntegerConstants(Diagram parentDiagram, int elementValue, int size)
        {
            FunctionalNode initializeVector = new FunctionalNode(parentDiagram, Signatures.VectorInitializeType);
            ConnectConstantToInputTerminal(initializeVector.InputTerminals[0], NITypes.Int32, elementValue, false);
            ConnectConstantToInputTerminal(initializeVector.InputTerminals[1], NITypes.Int32, size, false);
            return initializeVector;
        }

        [TestMethod]
        public void InitializeVectorAndSumWithSliceIterator_Execute_CorrectSumValue()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode initializeVector = CreateInitializeVectorWithIntegerConstants(function.BlockDiagram, 4, 4);
            FunctionalNode vectorToSlice = new FunctionalNode(function.BlockDiagram, Signatures.VectorToSliceType);
            Wire.Create(function.BlockDiagram, initializeVector.OutputTerminals[0], vectorToSlice.InputTerminals[0]);
            FunctionalNode sliceToIterator = new FunctionalNode(function.BlockDiagram, Signatures.SliceToIteratorType);
            Wire.Create(function.BlockDiagram, vectorToSlice.OutputTerminals[0], sliceToIterator.InputTerminals[0]);
            Loop loop = new Loop(function.BlockDiagram);
            CreateLoopConditionTunnel(loop);
            IterateTunnel iterateTunnel = CreateIterateTunnel(loop);
            BorrowTunnel borrowTunnel = CreateBorrowTunnel(loop, BorrowMode.Mutable);
            ConnectConstantToInputTerminal(borrowTunnel.InputTerminals[0], NITypes.Int32, 0, true);
            Wire.Create(function.BlockDiagram, sliceToIterator.OutputTerminals[0], iterateTunnel.InputTerminals[0])
                .SetWireBeginsMutableVariable(true);
            FunctionalNode accumulateAdd = new FunctionalNode(loop.Diagrams[0], Signatures.DefineMutatingBinaryFunction("AccumulateAdd", NITypes.Int32));
            Wire.Create(loop.Diagrams[0], borrowTunnel.OutputTerminals[0], accumulateAdd.InputTerminals[0]);
            Wire.Create(loop.Diagrams[0], iterateTunnel.OutputTerminals[0], accumulateAdd.InputTerminals[1]);
            FunctionalNode inspectNode = ConnectInspectToOutputTerminal(borrowTunnel.TerminateLifetimeTunnel.OutputTerminals[0]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            byte[] inspectValue = executionInstance.GetLastValueFromInspectNode(inspectNode);
            AssertByteArrayIsInt32(inspectValue, 16);
        }
    }
}
