using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler;
using Rebar.Compiler.Nodes;
using Rebar.RebarTarget.Execution;

namespace Tests.Rebar.Unit.Execution
{
    [TestClass]
    public class OptionTypeExecutionTests : ExecutionTestBase
    {
        [TestMethod]
        public void BranchedOptionWire_Execute_BothSinksHaveCorrectValues()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode initialSome = CreateInt32SomeConstructor(function.BlockDiagram, 5);
            FunctionalNode inspect1 = new FunctionalNode(function.BlockDiagram, Signatures.InspectType),
                inspect2 = new FunctionalNode(function.BlockDiagram, Signatures.InspectType);
            Wire.Create(function.BlockDiagram, initialSome.OutputTerminals[0], inspect1.InputTerminals[0], inspect2.InputTerminals[0]);

            ExecutionContext context = CompileAndExecuteFunction(function);

            byte[] inspect1Value = GetLastValueFromInspectNode(context, inspect1),
                inspect2Value = GetLastValueFromInspectNode(context, inspect2);
            AssertByteArrayIsSomeInteger(inspect1Value, 5);
            AssertByteArrayIsSomeInteger(inspect2Value, 5);
        }

        [TestMethod]
        public void CreateCopyWithOptionValue_Execute_CopyHasCorrectValue()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode some = CreateInt32SomeConstructor(function.BlockDiagram, 5);
            FunctionalNode createCopy = new FunctionalNode(function.BlockDiagram, Signatures.CreateCopyType);
            Wire.Create(function.BlockDiagram, some.OutputTerminals[0], createCopy.InputTerminals[0]);
            FunctionalNode inspect = new FunctionalNode(function.BlockDiagram, Signatures.InspectType);
            Wire.Create(function.BlockDiagram, createCopy.OutputTerminals[1], inspect.InputTerminals[0]);

            ExecutionContext context = CompileAndExecuteFunction(function);

            byte[] inspectValue = GetLastValueFromInspectNode(context, inspect);
            AssertByteArrayIsSomeInteger(inspectValue, 5);
        }

        [TestMethod]
        public void AssignOptionValueToNewSomeValue_Execute_CorrectFinalValue()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode assign, inspect;
            CreateAssignToSomeInt32ValueAndInspect(function.BlockDiagram, out assign, out inspect);
            FunctionalNode finalSome = CreateInt32SomeConstructor(function.BlockDiagram, 5);
            Wire.Create(function.BlockDiagram, finalSome.OutputTerminals[0], assign.InputTerminals[1]);

            ExecutionContext context = CompileAndExecuteFunction(function);

            byte[] inspectValue = GetLastValueFromInspectNode(context, inspect);
            AssertByteArrayIsSomeInteger(inspectValue, 5);
        }

        [TestMethod]
        public void AssignOptionValueToNoneValue_Execute_CorrectFinalValue()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode assign, inspect;
            CreateAssignToSomeInt32ValueAndInspect(function.BlockDiagram, out assign, out inspect);
            FunctionalNode finalNone = new FunctionalNode(function.BlockDiagram, Signatures.NoneConstructorType);
            Wire.Create(function.BlockDiagram, finalNone.OutputTerminals[0], assign.InputTerminals[1]);

            ExecutionContext context = CompileAndExecuteFunction(function);

            byte[] inspectValue = GetLastValueFromInspectNode(context, inspect);
            AssertByteArrayIsNoneInteger(inspectValue);
        }

        [TestMethod]
        public void ExchangeValuesWithSomeAndNoneInput_Execute_CorrectFinalValues()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode exchangeValues = new FunctionalNode(function.BlockDiagram, Signatures.ExchangeValuesType);
            FunctionalNode some = CreateInt32SomeConstructor(function.BlockDiagram, 5);
            Wire someWire = Wire.Create(function.BlockDiagram, some.OutputTerminals[0], exchangeValues.InputTerminals[0]);
            someWire.SetWireBeginsMutableVariable(true);
            FunctionalNode none = new FunctionalNode(function.BlockDiagram, Signatures.NoneConstructorType);
            Wire noneWire = Wire.Create(function.BlockDiagram, none.OutputTerminals[0], exchangeValues.InputTerminals[1]);
            noneWire.SetWireBeginsMutableVariable(true);
            FunctionalNode noneInspect = ConnectInspectToOutputTerminal(exchangeValues.OutputTerminals[0]);
            FunctionalNode someInspect = ConnectInspectToOutputTerminal(exchangeValues.OutputTerminals[1]);

            ExecutionContext context = CompileAndExecuteFunction(function);

            byte[] noneInspectValue = GetLastValueFromInspectNode(context, noneInspect);
            AssertByteArrayIsNoneInteger(noneInspectValue);
            byte[] someInspectValue = GetLastValueFromInspectNode(context, someInspect);
            AssertByteArrayIsSomeInteger(someInspectValue, 5);
        }

        [TestMethod]
        public void UnwrapOptionTunnelWithSomeInput_Execute_FrameExecutes()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode some = CreateInt32SomeConstructor(function.BlockDiagram, 5);
            Frame frame = Frame.Create(function.BlockDiagram);
            UnwrapOptionTunnel unwrapOptionTunnel = new UnwrapOptionTunnel(frame);
            Wire.Create(function.BlockDiagram, some.OutputTerminals[0], unwrapOptionTunnel.InputTerminals[0]);
            BorrowTunnel borrowTunnel = CreateBorrowTunnel(frame, BorrowMode.Mutable);
            ConnectConstantToInputTerminal(borrowTunnel.InputTerminals[0], PFTypes.Int32, true);
            FunctionalNode assign = new FunctionalNode(frame.Diagram, Signatures.AssignType);
            Wire.Create(frame.Diagram, borrowTunnel.OutputTerminals[0], assign.InputTerminals[0]);
            Wire.Create(frame.Diagram, unwrapOptionTunnel.OutputTerminals[0], assign.InputTerminals[1]);
            FunctionalNode inspect = ConnectInspectToOutputTerminal(borrowTunnel.TerminateLifetimeTunnel.OutputTerminals[0]);

            ExecutionContext context = CompileAndExecuteFunction(function);

            byte[] finalValue = GetLastValueFromInspectNode(context, inspect);
            AssertByteArrayIsInt32(finalValue, 5);
        }

        [TestMethod]
        public void UnwrapOptionTunnelWithSomeInputAndOutputTunnel_Execute_OutputTunnelOutputsSomeValue()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode some = CreateInt32SomeConstructor(function.BlockDiagram, 5);
            Frame frame = Frame.Create(function.BlockDiagram);
            UnwrapOptionTunnel unwrapOptionTunnel = new UnwrapOptionTunnel(frame);
            Wire.Create(function.BlockDiagram, some.OutputTerminals[0], unwrapOptionTunnel.InputTerminals[0]);
            Tunnel outputTunnel = CreateOutputTunnel(frame);
            Constant intConstant = ConnectConstantToInputTerminal(outputTunnel.InputTerminals[0], PFTypes.Int32, false);
            intConstant.Value = 5;
            FunctionalNode inspect = ConnectInspectToOutputTerminal(outputTunnel.OutputTerminals[0]);

            ExecutionContext context = CompileAndExecuteFunction(function);

            byte[] finalValue = GetLastValueFromInspectNode(context, inspect);
            AssertByteArrayIsSomeInteger(finalValue, 5);
        }

        [TestMethod]
        public void UnwrapOptionTunnelWithNoneInput_Execute_FrameDoesNotExecute()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode none = new FunctionalNode(function.BlockDiagram, Signatures.NoneConstructorType);
            Frame frame = Frame.Create(function.BlockDiagram);
            UnwrapOptionTunnel unwrapOptionTunnel = new UnwrapOptionTunnel(frame);
            Wire.Create(function.BlockDiagram, none.OutputTerminals[0], unwrapOptionTunnel.InputTerminals[0]);
            BorrowTunnel borrowTunnel = CreateBorrowTunnel(frame, BorrowMode.Mutable);
            ConnectConstantToInputTerminal(borrowTunnel.InputTerminals[0], PFTypes.Int32, true);
            FunctionalNode assign = new FunctionalNode(frame.Diagram, Signatures.AssignType);
            Wire.Create(frame.Diagram, borrowTunnel.OutputTerminals[0], assign.InputTerminals[0]);
            Wire.Create(frame.Diagram, unwrapOptionTunnel.OutputTerminals[0], assign.InputTerminals[1]);
            FunctionalNode inspect = ConnectInspectToOutputTerminal(borrowTunnel.TerminateLifetimeTunnel.OutputTerminals[0]);

            ExecutionContext context = CompileAndExecuteFunction(function);

            byte[] finalValue = GetLastValueFromInspectNode(context, inspect);
            AssertByteArrayIsInt32(finalValue, 0);
        }

        [TestMethod]
        public void UnwrapOptionTunnelWithNoneInputAndOutputTunnel_Execute_OutputTunnelOutputsNoneValue()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode none = new FunctionalNode(function.BlockDiagram, Signatures.NoneConstructorType);
            Frame frame = Frame.Create(function.BlockDiagram);
            UnwrapOptionTunnel unwrapOptionTunnel = new UnwrapOptionTunnel(frame);
            Wire.Create(function.BlockDiagram, none.OutputTerminals[0], unwrapOptionTunnel.InputTerminals[0]);
            FunctionalNode assign = new FunctionalNode(frame.Diagram, Signatures.AssignType);
            Wire unwrapWire = Wire.Create(frame.Diagram, unwrapOptionTunnel.OutputTerminals[0], assign.InputTerminals[0]);
            unwrapWire.SetWireBeginsMutableVariable(true);
            ConnectConstantToInputTerminal(assign.InputTerminals[1], PFTypes.Int32, false);
            Tunnel outputTunnel = CreateOutputTunnel(frame);
            ConnectConstantToInputTerminal(outputTunnel.InputTerminals[0], PFTypes.Int32, false);
            FunctionalNode inspect = ConnectInspectToOutputTerminal(outputTunnel.OutputTerminals[0]);

            ExecutionContext context = CompileAndExecuteFunction(function);

            byte[] finalValue = GetLastValueFromInspectNode(context, inspect);
            AssertByteArrayIsNoneInteger(finalValue);
        }

        private FunctionalNode CreateInt32SomeConstructor(Diagram diagram, int value)
        {
            FunctionalNode initialSome = new FunctionalNode(diagram, Signatures.SomeConstructorType);
            Constant constant = ConnectConstantToInputTerminal(initialSome.InputTerminals[0], PFTypes.Int32, false);
            constant.Value = value;
            return initialSome;
        }

        private void CreateAssignToSomeInt32ValueAndInspect(Diagram diagram, out FunctionalNode assign, out FunctionalNode inspect)
        {
            FunctionalNode initialSome = CreateInt32SomeConstructor(diagram, 0);
            assign = new FunctionalNode(diagram, Signatures.AssignType);
            Wire initialAssignWire = Wire.Create(diagram, initialSome.OutputTerminals[0], assign.InputTerminals[0]);
            initialAssignWire.SetWireBeginsMutableVariable(true);
            inspect = ConnectInspectToOutputTerminal(assign.OutputTerminals[0]);
        }
    }
}
