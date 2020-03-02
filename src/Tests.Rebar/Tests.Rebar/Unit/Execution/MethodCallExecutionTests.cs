using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.Compiler;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using NationalInstruments.Linking;
using Rebar.Common;
using Rebar.Compiler;
using Rebar.Compiler.Nodes;

namespace Tests.Rebar.Unit.Execution
{
    [TestClass]
    public class MethodCallExecutionTests : ExecutionTestBase
    {
        [TestMethod]
        public void FunctionWithCallToParameterlessFunction_Execute_CalleeFunctionsExecutes()
        {
            string calleeName = "callee";
            NIType calleeType = DefineFunctionSignatureWithNoParameters(calleeName);
            ExtendedQualifiedName calleeQualifiedName = ExtendedQualifiedName.CreateName(new QualifiedName(calleeName), "component", null, ContentId.EmptyId, null);
            DfirRoot calleeFunction = CreateFunctionFromSignature(calleeType, calleeQualifiedName);
            Constant calleeConstant = Constant.Create(calleeFunction.BlockDiagram, 5, PFTypes.Int32);
            FunctionalNode inspect = ConnectInspectToOutputTerminal(calleeConstant.OutputTerminal);
            DfirRoot callerFunction = DfirRoot.Create();
            var methodCall = new MethodCallNode(callerFunction.BlockDiagram, calleeQualifiedName, calleeType);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(callerFunction, calleeFunction);

            byte[] inspectValue = executionInstance.GetLastValueFromInspectNode(inspect);
            AssertByteArrayIsInt32(inspectValue, 5);
        }

        [TestMethod]
        public void FunctionWithCallToFunctionWithInAndOutParameters_Execute_CalleeFunctionExecutesAndReturnsCorrectResult()
        {
            string calleeName = "callee";
            NIType calleeType = DefineFunctionSignatureWithInAndOutParameters(calleeName);
            ExtendedQualifiedName calleeQualifiedName = ExtendedQualifiedName.CreateName(new QualifiedName(calleeName), "component", null, ContentId.EmptyId, null);
            DfirRoot calleeFunction = CreateFunctionFromSignature(calleeType, calleeQualifiedName);
            DataAccessor inputDataAccessor = DataAccessor.Create(calleeFunction.BlockDiagram, calleeFunction.DataItems[0], Direction.Output);
            DataAccessor outputDataAccessor = DataAccessor.Create(calleeFunction.BlockDiagram, calleeFunction.DataItems[1], Direction.Input);
            FunctionalNode add = new FunctionalNode(calleeFunction.BlockDiagram, Signatures.DefinePureBinaryFunction("Add", PFTypes.Int32, PFTypes.Int32));
            Wire.Create(calleeFunction.BlockDiagram, inputDataAccessor.Terminal, add.InputTerminals[0]);
            ConnectConstantToInputTerminal(add.InputTerminals[1], PFTypes.Int32, 1, false);
            Wire.Create(calleeFunction.BlockDiagram, add.OutputTerminals[2], outputDataAccessor.Terminal);
            DfirRoot callerFunction = DfirRoot.Create();
            var methodCall = new MethodCallNode(callerFunction.BlockDiagram, calleeQualifiedName, calleeType);
            ConnectConstantToInputTerminal(methodCall.InputTerminals[0], PFTypes.Int32, 5, false);
            FunctionalNode inspect = ConnectInspectToOutputTerminal(methodCall.OutputTerminals[0]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(callerFunction, calleeFunction);

            byte[] inspectValue = executionInstance.GetLastValueFromInspectNode(inspect);
            AssertByteArrayIsInt32(inspectValue, 6);
        }

        [TestMethod]
        public void FunctionWithCallToYieldingFunctionWithInAndOutParameters_Execute_CalleeFunctionExecutesAndReturnsCorrectResult()
        {
            string calleeName = "callee";
            NIType calleeType = DefineFunctionSignatureWithInAndOutParameters(calleeName);
            ExtendedQualifiedName calleeQualifiedName = ExtendedQualifiedName.CreateName(new QualifiedName(calleeName), "component", null, ContentId.EmptyId, null);
            DfirRoot calleeFunction = CreateFunctionFromSignature(calleeType, calleeQualifiedName);
            DataAccessor inputDataAccessor = DataAccessor.Create(calleeFunction.BlockDiagram, calleeFunction.DataItems[0], Direction.Output);
            DataAccessor outputDataAccessor = DataAccessor.Create(calleeFunction.BlockDiagram, calleeFunction.DataItems[1], Direction.Input);
            FunctionalNode add = new FunctionalNode(calleeFunction.BlockDiagram, Signatures.DefinePureBinaryFunction("Add", PFTypes.Int32, PFTypes.Int32));
            Wire.Create(calleeFunction.BlockDiagram, inputDataAccessor.Terminal, add.InputTerminals[0]);
            ConnectConstantToInputTerminal(add.InputTerminals[1], PFTypes.Int32, 1, false);
            var yieldNode = new FunctionalNode(calleeFunction.BlockDiagram, Signatures.YieldType);
            Wire.Create(calleeFunction.BlockDiagram, add.OutputTerminals[2], yieldNode.InputTerminals[0]);
            Wire.Create(calleeFunction.BlockDiagram, yieldNode.OutputTerminals[0], outputDataAccessor.Terminal);
            DfirRoot callerFunction = DfirRoot.Create();
            var methodCall = new MethodCallNode(callerFunction.BlockDiagram, calleeQualifiedName, calleeType);
            ConnectConstantToInputTerminal(methodCall.InputTerminals[0], PFTypes.Int32, 5, false);
            FunctionalNode inspect = ConnectInspectToOutputTerminal(methodCall.OutputTerminals[0]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(callerFunction, calleeFunction);

            byte[] inspectValue = executionInstance.GetLastValueFromInspectNode(inspect);
            AssertByteArrayIsInt32(inspectValue, 6);
        }

        [TestMethod]
        public void FunctionWithCallToPanickingFunctionWithInAndOutParameters_Execute_CalleeFunctionPanics()
        {
            string calleeName = "callee";
            NIType calleeType = DefineFunctionSignatureWithInAndOutParameters(calleeName);
            ExtendedQualifiedName calleeQualifiedName = ExtendedQualifiedName.CreateName(new QualifiedName(calleeName), "component", null, ContentId.EmptyId, null);
            DfirRoot calleeFunction = CreateFunctionFromSignature(calleeType, calleeQualifiedName);
            DataAccessor inputDataAccessor = DataAccessor.Create(calleeFunction.BlockDiagram, calleeFunction.DataItems[0], Direction.Output);
            DataAccessor outputDataAccessor = DataAccessor.Create(calleeFunction.BlockDiagram, calleeFunction.DataItems[1], Direction.Input);
            var some = new FunctionalNode(calleeFunction.BlockDiagram, Signatures.SomeConstructorType);
            Wire.Create(calleeFunction.BlockDiagram, inputDataAccessor.Terminal, some.InputTerminals[0])
                .SetWireBeginsMutableVariable(true);
            var none = new FunctionalNode(calleeFunction.BlockDiagram, Signatures.NoneConstructorType);
            var assign = new FunctionalNode(calleeFunction.BlockDiagram, Signatures.AssignType);
            Wire.Create(calleeFunction.BlockDiagram, some.OutputTerminals[0], assign.InputTerminals[0]);
            Wire.Create(calleeFunction.BlockDiagram, none.OutputTerminals[0], assign.InputTerminals[1]);
            var unwrap = new FunctionalNode(calleeFunction.BlockDiagram, Signatures.UnwrapOptionType);
            Wire.Create(calleeFunction.BlockDiagram, assign.OutputTerminals[0], unwrap.InputTerminals[0]);
            Wire.Create(calleeFunction.BlockDiagram, unwrap.OutputTerminals[0], outputDataAccessor.Terminal);
            DfirRoot callerFunction = DfirRoot.Create();
            var methodCall = new MethodCallNode(callerFunction.BlockDiagram, calleeQualifiedName, calleeType);
            ConnectConstantToInputTerminal(methodCall.InputTerminals[0], PFTypes.Int32, 5, false);
            var output = new FunctionalNode(callerFunction.BlockDiagram, Signatures.OutputType);
            Wire.Create(callerFunction.BlockDiagram, methodCall.OutputTerminals[0], output.InputTerminals[0]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(callerFunction, calleeFunction);

            Assert.IsTrue(executionInstance.RuntimeServices.PanicOccurred);
            Assert.IsNull(executionInstance.RuntimeServices.LastOutputValue, "Expected no output value to be written.");
        }

        [TestMethod]
        public void FunctionWithCallToFunctionWithTwoOutParameters_Execute_CalleeFunctionExecutesAndReturnsCorrectResults()
        {
            string calleeName = "callee";
            NIType calleeType = DefineFunctionSignatureWithTwoOutParameters(calleeName);
            ExtendedQualifiedName calleeQualifiedName = ExtendedQualifiedName.CreateName(new QualifiedName(calleeName), "component", null, ContentId.EmptyId, null);
            DfirRoot calleeFunction = CreateFunctionFromSignature(calleeType, calleeQualifiedName);
            DataAccessor outputDataAccessor0 = DataAccessor.Create(calleeFunction.BlockDiagram, calleeFunction.DataItems[0], Direction.Input);
            DataAccessor outputDataAccessor1 = DataAccessor.Create(calleeFunction.BlockDiagram, calleeFunction.DataItems[1], Direction.Input);
            ConnectConstantToInputTerminal(outputDataAccessor0.Terminal, PFTypes.Int32, 5, false);
            ConnectConstantToInputTerminal(outputDataAccessor1.Terminal, PFTypes.Int32, 6, false);
            DfirRoot callerFunction = DfirRoot.Create();
            var methodCall = new MethodCallNode(callerFunction.BlockDiagram, calleeQualifiedName, calleeType);
            FunctionalNode inspect0 = ConnectInspectToOutputTerminal(methodCall.OutputTerminals[0]);
            FunctionalNode inspect1 = ConnectInspectToOutputTerminal(methodCall.OutputTerminals[1]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(callerFunction, calleeFunction);

            byte[] inspectValue = executionInstance.GetLastValueFromInspectNode(inspect0);
            AssertByteArrayIsInt32(inspectValue, 5);
            inspectValue = executionInstance.GetLastValueFromInspectNode(inspect1);
            AssertByteArrayIsInt32(inspectValue, 6);
        }

        private NIType DefineFunctionSignatureWithNoParameters(string functionName)
        {
            NIFunctionBuilder functionBuilder = PFTypes.Factory.DefineFunction(functionName);
            functionBuilder.IsStatic = true;
            return functionBuilder.CreateType();
        }

        private NIType DefineFunctionSignatureWithInAndOutParameters(string functionName)
        {
            NIFunctionBuilder functionBuilder = PFTypes.Factory.DefineFunction(functionName);
            functionBuilder.IsStatic = true;
            functionBuilder.DefineParameter(PFTypes.Int32, "in", NIParameterPassingRule.Required, NIParameterPassingRule.NotAllowed, "in");
            functionBuilder.DefineParameter(PFTypes.Int32, "out", NIParameterPassingRule.NotAllowed, NIParameterPassingRule.Optional, "out");
            return functionBuilder.CreateType();
        }

        private NIType DefineFunctionSignatureWithTwoOutParameters(string functionName)
        {
            NIFunctionBuilder functionBuilder = PFTypes.Factory.DefineFunction(functionName);
            functionBuilder.IsStatic = true;
            functionBuilder.DefineParameter(PFTypes.Int32, "out0", NIParameterPassingRule.NotAllowed, NIParameterPassingRule.Optional, "out0");
            functionBuilder.DefineParameter(PFTypes.Int32, "out1", NIParameterPassingRule.NotAllowed, NIParameterPassingRule.Optional, "out1");
            return functionBuilder.CreateType();
        }

        private DfirRoot CreateFunctionFromSignature(NIType functionSignature, ExtendedQualifiedName functionQualifiedName)
        {
            DfirRoot function = DfirRoot.Create(new SpecAndQName(null, functionQualifiedName));
            int connectorPaneIndex = 0;
            foreach (NIType parameter in functionSignature.GetParameters())
            {
                function.CreateDataItem(
                    parameter.GetName(),
                    parameter.GetDataType(),
                    null,   // defaultValue
                    parameter.GetInputParameterPassingRule(),
                    parameter.GetOutputParameterPassingRule(),
                    connectorPaneIndex++);
            }
            return function;
        }
    }
}
