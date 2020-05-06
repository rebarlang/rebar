using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.CommonModel;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using NationalInstruments.ExecutionFramework;
using Rebar.Common;
using Rebar.Compiler.Nodes;
using Loop = Rebar.Compiler.Nodes.Loop;

namespace Tests.Rebar.Unit.Execution
{
    [TestClass]
    public class PanicExecutionTests : ExecutionTestBase
    {
        [TestMethod]
        public void PanickingUnwrapOptionIntoOutput_Execute_RuntimeRegistersPanic()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode unwrapOption = CreatePanickingUnwrapOption(function.BlockDiagram);
            ConnectOutputToOutputTerminal(unwrapOption.OutputTerminals[0]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            Assert.IsTrue(executionInstance.RuntimeServices.PanicOccurred);
        }

        [TestMethod]
        public void PanickingUnwrapOptionIntoOutput_Execute_NoOutputValue()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode unwrapOption = CreatePanickingUnwrapOption(function.BlockDiagram);
            ConnectOutputToOutputTerminal(unwrapOption.OutputTerminals[0]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            Assert.IsNull(executionInstance.RuntimeServices.LastOutputValue, "Expected no output value to be written.");
        }

        [TestMethod]
        public void NonPanickingUnwrapOptionIntoInspect_Execute_CorrectValue()
        {
            DfirRoot function = DfirRoot.Create();
            var unwrapOption = new FunctionalNode(function.BlockDiagram, Signatures.UnwrapOptionType);
            ConnectSomeOfIntegerToInputTerminal(unwrapOption.InputTerminals[0], 5, false);
            FunctionalNode inspect = ConnectInspectToOutputTerminal(unwrapOption.OutputTerminals[0]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            byte[] inspectValue = executionInstance.GetLastValueFromInspectNode(inspect);
            AssertByteArrayIsInt32(inspectValue, 5);
        }

        [TestMethod]
        public void PanickingAndNonpanickingUnwrapOptionIntoAddAndOutput_Execute_NoOutputValue()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode unwrapNone = CreatePanickingUnwrapOption(function.BlockDiagram);
            var unwrapSome = new FunctionalNode(function.BlockDiagram, Signatures.UnwrapOptionType);
            ConnectSomeOfIntegerToInputTerminal(unwrapSome.InputTerminals[0], 5);
            var add = new FunctionalNode(function.BlockDiagram, Signatures.DefinePureBinaryFunction("Add", NITypes.Int32, NITypes.Int32));
            Wire.Create(function.BlockDiagram, unwrapNone.OutputTerminals[0], add.InputTerminals[0]);
            Wire.Create(function.BlockDiagram, unwrapSome.OutputTerminals[0], add.InputTerminals[1]);
            ConnectOutputToOutputTerminal(add.OutputTerminals[2]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            AssertNoOutput(executionInstance);
        }

        [TestMethod]
        public void PanickingUnwrapOptionIntoFrame_Execute_FrameDoesNotExecute()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode unwrap = CreatePanickingUnwrapOption(function.BlockDiagram);
            Frame frame = Frame.Create(function.BlockDiagram);
            Tunnel inputTunnel = CreateInputTunnel(frame);
            Wire.Create(function.BlockDiagram, unwrap.OutputTerminals[0], inputTunnel.InputTerminals[0]);
            ConnectOutputToOutputTerminal(inputTunnel.OutputTerminals[0]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            AssertNoOutput(executionInstance);
        }

        [TestMethod]
        public void PanickingUnwrapOptionInsideFrame_Execute_DownstreamOfFrameDoesNotExecute()
        {
            DfirRoot function = DfirRoot.Create();
            Frame frame = Frame.Create(function.BlockDiagram);
            FunctionalNode unwrap = CreatePanickingUnwrapOption(frame.Diagram);
            Tunnel outputTunnel = CreateOutputTunnel(frame);
            Wire.Create(frame.Diagram, unwrap.OutputTerminals[0], outputTunnel.InputTerminals[0]);
            ConnectOutputToOutputTerminal(outputTunnel.OutputTerminals[0]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            AssertNoOutput(executionInstance);
        }

        [TestMethod]
        public void PanickingUnwrapOptionIntoConditionalFrame_Execute_FrameDoesNotExecute()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode unwrap = CreatePanickingUnwrapOption(function.BlockDiagram);
            Frame frame = Frame.Create(function.BlockDiagram);
            Tunnel inputTunnel = CreateInputTunnel(frame);
            Wire.Create(function.BlockDiagram, unwrap.OutputTerminals[0], inputTunnel.InputTerminals[0]);
            ConnectOutputToOutputTerminal(inputTunnel.OutputTerminals[0]);
            UnwrapOptionTunnel unwrapOptionTunnel = CreateUnwrapOptionTunnel(frame);
            ConnectSomeOfIntegerToInputTerminal(unwrapOptionTunnel.InputTerminals[0], 0);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            AssertNoOutput(executionInstance);
        }

        [TestMethod]
        public void PanickingUnwrapOptionInsideConditionalFrame_Execute_DownstreamOfFrameDoesNotExecute()
        {
            DfirRoot function = DfirRoot.Create();
            Frame frame = Frame.Create(function.BlockDiagram);
            UnwrapOptionTunnel unwrapOptionTunnel = CreateUnwrapOptionTunnel(frame);
            ConnectSomeOfIntegerToInputTerminal(unwrapOptionTunnel.InputTerminals[0], 0);
            FunctionalNode unwrap = CreatePanickingUnwrapOption(frame.Diagram);
            Tunnel outputTunnel = CreateOutputTunnel(frame);
            Wire.Create(frame.Diagram, unwrap.OutputTerminals[0], outputTunnel.InputTerminals[0]);
            Frame secondaryFrame = Frame.Create(function.BlockDiagram);
            UnwrapOptionTunnel secondaryUnwrapOption = CreateUnwrapOptionTunnel(secondaryFrame);
            Wire.Create(function.BlockDiagram, outputTunnel.OutputTerminals[0], secondaryUnwrapOption.InputTerminals[0]);
            ConnectOutputToOutputTerminal(secondaryUnwrapOption.OutputTerminals[0]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            AssertNoOutput(executionInstance);
        }

        // Failing because input tunnels on OptionPatternStructure aren't supported
        // [TestMethod]
        public void PanickingUnwrapOptionIntoUnwrapOptionStructure_Execute_StructureDoesNotExecute()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode unwrap = CreatePanickingUnwrapOption(function.BlockDiagram);
            OptionPatternStructure optionPatternStructure = CreateOptionPatternStructure(function.BlockDiagram);
            ConnectSomeOfIntegerToInputTerminal(optionPatternStructure.Selector.InputTerminals[0], 0);
            Tunnel inputTunnel = CreateInputTunnel(optionPatternStructure);
            Wire.Create(function.BlockDiagram, unwrap.OutputTerminals[0], inputTunnel.InputTerminals[0]);
            ConnectOutputToOutputTerminal(inputTunnel.OutputTerminals[0]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            AssertNoOutput(executionInstance);
        }

        [TestMethod]
        public void PanickingUnwrapOptionInsideOptionPatternStructureDiagram_Execute_DownstreamOfStructureDoesNotExecute()
        {
            DfirRoot function = DfirRoot.Create();
            OptionPatternStructure optionPatternStructure = CreateOptionPatternStructure(function.BlockDiagram);
            ConnectSomeOfIntegerToInputTerminal(optionPatternStructure.Selector.InputTerminals[0], 0);
            Diagram someDiagram = optionPatternStructure.Diagrams[0];
            FunctionalNode unwrap = CreatePanickingUnwrapOption(someDiagram);
            Tunnel outputTunnel = CreateOutputTunnel(optionPatternStructure);
            Wire.Create(someDiagram, unwrap.OutputTerminals[0], outputTunnel.InputTerminals[0]);
            ConnectConstantToInputTerminal(outputTunnel.InputTerminals[1], NITypes.Int32, 1, false);
            ConnectOutputToOutputTerminal(outputTunnel.OutputTerminals[0]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            AssertNoOutput(executionInstance);
        }

        [TestMethod]
        public void PanickingUnwrapOptionIntoLoop_Execute_LoopDoesNotExecute()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode unwrap = CreatePanickingUnwrapOption(function.BlockDiagram);
            Loop loop = new Loop(function.BlockDiagram);
            LoopConditionTunnel condition = CreateLoopConditionTunnel(loop);
            // Wire explicit true to condition so that it is initialized outside the PanicAndContinue's clump 
            ConnectConstantToInputTerminal(condition.InputTerminals[0], NITypes.Boolean, true, false);
            Tunnel inputTunnel = CreateInputTunnel(loop);
            Wire.Create(function.BlockDiagram, unwrap.OutputTerminals[0], inputTunnel.InputTerminals[0]);
            ConnectOutputToOutputTerminal(inputTunnel.OutputTerminals[0]);
            AssignFalseToLoopConditionOutputTerminal(condition);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            AssertNoOutput(executionInstance);
        }

        [TestMethod]
        public void PanickingUnwrapOptionInsideLoopDiagram_Execute_LoopTerminatesAndDownstreamOfLoopDoesNotExecute()
        {
            DfirRoot function = DfirRoot.Create();
            Loop loop = new Loop(function.BlockDiagram);
            LoopConditionTunnel condition = CreateLoopConditionTunnel(loop);
            FunctionalNode unwrap = CreatePanickingUnwrapOption(loop.Diagram);
            ConnectOutputToOutputTerminal(condition.TerminateLifetimeTunnel.OutputTerminals[0]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            AssertNoOutput(executionInstance);
        }

        [TestMethod]
        public void PanickingMethodCallWithNoParametersThatPanics_Execute_RuntimeRegistersPanic()
        {
            string calleeName = "callee";
            NIType calleeType = calleeName.DefineMethodType().CreateType();
            CompilableDefinitionName calleeDefinitionName = CreateTestCompilableDefinitionName(calleeName);
            DfirRoot calleeFunction = calleeType.CreateFunctionFromSignature(calleeDefinitionName);
            CreatePanickingUnwrapOption(calleeFunction.BlockDiagram);
            DfirRoot callerFunction = DfirRoot.Create();
            var methodCall = new MethodCallNode(callerFunction.BlockDiagram, calleeDefinitionName, calleeType);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(callerFunction, calleeFunction);

            Assert.IsTrue(executionInstance.RuntimeServices.PanicOccurred);
        }

        [TestMethod]
        public void PanickingMethodCallWithNoParametersThatDoesNotPanic_Execute_RuntimeDoesNotRegisterPanic()
        {
            string calleeName = "callee";
            NIType calleeType = calleeName.DefineMethodType().CreateType();
            CompilableDefinitionName calleeDefinitionName = CreateTestCompilableDefinitionName(calleeName);
            DfirRoot calleeFunction = calleeType.CreateFunctionFromSignature(calleeDefinitionName);
            CreateNonPanickingUnwrapOption(calleeFunction.BlockDiagram);
            DfirRoot callerFunction = DfirRoot.Create();
            var methodCall = new MethodCallNode(callerFunction.BlockDiagram, calleeDefinitionName, calleeType);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(callerFunction, calleeFunction);

            Assert.IsFalse(executionInstance.RuntimeServices.PanicOccurred);
        }

        [TestMethod]
        public void PanickingMethodCallWithInputAndOutputThatPanicsIntoOutput_Execute_NoOutputValue()
        {
            string calleeName = "callee";
            NIType calleeType = DefineFunctionTypeWithOptionIntInAndIntOut(calleeName);
            CompilableDefinitionName calleeDefinitionName = CreateTestCompilableDefinitionName(calleeName);
            DfirRoot calleeFunction = calleeType.CreateFunctionFromSignature(calleeDefinitionName);
            DataAccessor inputDataAccessor = DataAccessor.Create(calleeFunction.BlockDiagram, calleeFunction.DataItems[0], Direction.Output);
            DataAccessor outputDataAccessor = DataAccessor.Create(calleeFunction.BlockDiagram, calleeFunction.DataItems[1], Direction.Input);
            FunctionalNode unwrap = new FunctionalNode(calleeFunction.BlockDiagram, Signatures.UnwrapOptionType);
            Wire.Create(calleeFunction.BlockDiagram, inputDataAccessor.Terminal, unwrap.InputTerminals[0]);
            Wire.Create(calleeFunction.BlockDiagram, unwrap.OutputTerminals[0], outputDataAccessor.Terminal);
            DfirRoot callerFunction = DfirRoot.Create();
            var methodCall = new MethodCallNode(callerFunction.BlockDiagram, calleeDefinitionName, calleeType);
            FunctionalNode noneInteger = CreateNoneOfOptionIntegerType(callerFunction.BlockDiagram);
            Wire.Create(callerFunction.BlockDiagram, noneInteger.OutputTerminals[0], methodCall.InputTerminals[0]);
            ConnectOutputToOutputTerminal(methodCall.OutputTerminals[0]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(callerFunction, calleeFunction);

            AssertNoOutput(executionInstance);
        }

        [TestMethod]
        public void PanickingMethodCallWithTwoOutputsThatDoesNotPanicIntoAdd_Execute_CorrectValue()
        {
            string calleeName = "callee";
            NIType calleeType = DefineFunctionTypeWithTwoIntOuts(calleeName);
            CompilableDefinitionName calleeDefinitionName = CreateTestCompilableDefinitionName(calleeName);
            DfirRoot calleeFunction = calleeType.CreateFunctionFromSignature(calleeDefinitionName);
            DataAccessor outputDataAccessor0 = DataAccessor.Create(calleeFunction.BlockDiagram, calleeFunction.DataItems[0], Direction.Input);
            DataAccessor outputDataAccessor1 = DataAccessor.Create(calleeFunction.BlockDiagram, calleeFunction.DataItems[1], Direction.Input);
            FunctionalNode unwrap = new FunctionalNode(calleeFunction.BlockDiagram, Signatures.UnwrapOptionType);
            ConnectSomeOfIntegerToInputTerminal(unwrap.InputTerminals[0], 1);
            Wire.Create(calleeFunction.BlockDiagram, unwrap.OutputTerminals[0], outputDataAccessor0.Terminal, outputDataAccessor1.Terminal);
            DfirRoot callerFunction = DfirRoot.Create();
            var methodCall = new MethodCallNode(callerFunction.BlockDiagram, calleeDefinitionName, calleeType);
            FunctionalNode add = new FunctionalNode(callerFunction.BlockDiagram, Signatures.DefinePureBinaryFunction("Add", NITypes.Int32, NITypes.Int32));
            Wire.Create(callerFunction.BlockDiagram, methodCall.OutputTerminals[0], add.InputTerminals[0]);
            Wire.Create(callerFunction.BlockDiagram, methodCall.OutputTerminals[1], add.InputTerminals[1]);
            FunctionalNode inspect = ConnectInspectToOutputTerminal(add.OutputTerminals[2]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(callerFunction, calleeFunction);

            byte[] inspectValue = executionInstance.GetLastValueFromInspectNode(inspect);
            AssertByteArrayIsInt32(inspectValue, 2);
        }

        private NIType DefineFunctionTypeWithOptionIntInAndIntOut(string functionName)
        {
            return functionName.DefineMethodType().AddInput(NITypes.Int32.CreateOption(), "in").AddOutput(NITypes.Int32, "out").CreateType();
        }

        private NIType DefineFunctionTypeWithTwoIntOuts(string functionName)
        {
            return functionName.DefineMethodType().AddOutput(NITypes.Int32, "out0").AddOutput(NITypes.Int32, "out1").CreateType();
        }

        private FunctionalNode ConnectSomeOfIntegerToInputTerminal(Terminal inputTerminal, int value, bool mutable = false)
        {
            var someConstructor = ConnectSomeConstructorToInputTerminal(inputTerminal, mutable);
            ConnectConstantToInputTerminal(someConstructor.InputTerminals[0], NITypes.Int32, value, false);
            return someConstructor;
        }

        private FunctionalNode CreateNoneOfOptionIntegerType(Diagram parentDiagram)
        {
            var assign = new FunctionalNode(parentDiagram, Signatures.AssignType);
            ConnectSomeOfIntegerToInputTerminal(assign.InputTerminals[0], 0, true);
            var noneConstructor = new FunctionalNode(parentDiagram, Signatures.NoneConstructorType);
            Wire.Create(parentDiagram, noneConstructor.OutputTerminals[0], assign.InputTerminals[1]);
            return assign;
        }

        private FunctionalNode CreatePanickingUnwrapOption(Diagram parentDiagram)
        {
            FunctionalNode createNoneInteger = CreateNoneOfOptionIntegerType(parentDiagram);
            var unwrap = new FunctionalNode(parentDiagram, Signatures.UnwrapOptionType);
            Wire.Create(parentDiagram, createNoneInteger.OutputTerminals[0], unwrap.InputTerminals[0]);
            return unwrap;
        }

        private FunctionalNode CreateNonPanickingUnwrapOption(Diagram parentDiagram)
        {
            var unwrap = new FunctionalNode(parentDiagram, Signatures.UnwrapOptionType);
            ConnectSomeOfIntegerToInputTerminal(unwrap.InputTerminals[0], 0);
            return unwrap;
        }

        private FunctionalNode ConnectOutputToOutputTerminal(Terminal outputTerminal)
        {
            Diagram diagram = outputTerminal.ParentDiagram;
            var output = new FunctionalNode(diagram, Signatures.OutputType);
            Wire.Create(diagram, outputTerminal, output.InputTerminals[0]);
            return output;
        }

        private void AssignFalseToLoopConditionOutputTerminal(LoopConditionTunnel condition)
        {
            Diagram loopDiagram = condition.OutputTerminals[0].ParentDiagram;
            var assign = new FunctionalNode(loopDiagram, Signatures.AssignType);
            Wire.Create(loopDiagram, condition.OutputTerminals[0], assign.InputTerminals[0]);
            ConnectConstantToInputTerminal(assign.InputTerminals[1], NITypes.Boolean, false, false);
        }

        private void AssertNoOutput(TestExecutionInstance executionInstance)
        {
            Assert.IsNull(executionInstance.RuntimeServices.LastOutputValue, "Expected no output value to be written.");
        }
    }
}
