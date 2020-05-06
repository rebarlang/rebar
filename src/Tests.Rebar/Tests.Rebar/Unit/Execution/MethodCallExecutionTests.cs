using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.CommonModel;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using NationalInstruments.ExecutionFramework;
using Rebar.Common;
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
            CompilableDefinitionName calleeDefinitionName = CreateTestCompilableDefinitionName(calleeName);
            DfirRoot calleeFunction = calleeType.CreateFunctionFromSignature(calleeDefinitionName);
            Constant calleeConstant = Constant.Create(calleeFunction.BlockDiagram, 5, NITypes.Int32);
            FunctionalNode inspect = ConnectInspectToOutputTerminal(calleeConstant.OutputTerminal);
            DfirRoot callerFunction = DfirRoot.Create();
            var methodCall = new MethodCallNode(callerFunction.BlockDiagram, calleeDefinitionName, calleeType);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(callerFunction, calleeFunction);

            byte[] inspectValue = executionInstance.GetLastValueFromInspectNode(inspect);
            AssertByteArrayIsInt32(inspectValue, 5);
        }

        [TestMethod]
        public void FunctionWithCallToFunctionWithInAndOutParameters_Execute_CalleeFunctionExecutesAndReturnsCorrectResult()
        {
            string calleeName = "callee";
            NIType calleeType = DefineFunctionSignatureWithInAndOutParameters(calleeName);
            CompilableDefinitionName calleeDefinitionName = CreateTestCompilableDefinitionName(calleeName);
            DfirRoot calleeFunction = calleeType.CreateFunctionFromSignature(calleeDefinitionName);
            DataAccessor inputDataAccessor = DataAccessor.Create(calleeFunction.BlockDiagram, calleeFunction.DataItems[0], Direction.Output);
            DataAccessor outputDataAccessor = DataAccessor.Create(calleeFunction.BlockDiagram, calleeFunction.DataItems[1], Direction.Input);
            FunctionalNode add = new FunctionalNode(calleeFunction.BlockDiagram, Signatures.DefinePureBinaryFunction("Add", NITypes.Int32, NITypes.Int32));
            Wire.Create(calleeFunction.BlockDiagram, inputDataAccessor.Terminal, add.InputTerminals[0]);
            ConnectConstantToInputTerminal(add.InputTerminals[1], NITypes.Int32, 1, false);
            Wire.Create(calleeFunction.BlockDiagram, add.OutputTerminals[2], outputDataAccessor.Terminal);
            DfirRoot callerFunction = DfirRoot.Create();
            var methodCall = new MethodCallNode(callerFunction.BlockDiagram, calleeDefinitionName, calleeType);
            ConnectConstantToInputTerminal(methodCall.InputTerminals[0], NITypes.Int32, 5, false);
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
            CompilableDefinitionName calleeDefinitionName = CreateTestCompilableDefinitionName(calleeName);
            DfirRoot calleeFunction = calleeType.CreateFunctionFromSignature(calleeDefinitionName);
            DataAccessor inputDataAccessor = DataAccessor.Create(calleeFunction.BlockDiagram, calleeFunction.DataItems[0], Direction.Output);
            DataAccessor outputDataAccessor = DataAccessor.Create(calleeFunction.BlockDiagram, calleeFunction.DataItems[1], Direction.Input);
            FunctionalNode add = new FunctionalNode(calleeFunction.BlockDiagram, Signatures.DefinePureBinaryFunction("Add", NITypes.Int32, NITypes.Int32));
            Wire.Create(calleeFunction.BlockDiagram, inputDataAccessor.Terminal, add.InputTerminals[0]);
            ConnectConstantToInputTerminal(add.InputTerminals[1], NITypes.Int32, 1, false);
            var yieldNode = new FunctionalNode(calleeFunction.BlockDiagram, Signatures.YieldType);
            Wire.Create(calleeFunction.BlockDiagram, add.OutputTerminals[2], yieldNode.InputTerminals[0]);
            Wire.Create(calleeFunction.BlockDiagram, yieldNode.OutputTerminals[0], outputDataAccessor.Terminal);
            DfirRoot callerFunction = DfirRoot.Create();
            var methodCall = new MethodCallNode(callerFunction.BlockDiagram, calleeDefinitionName, calleeType);
            ConnectConstantToInputTerminal(methodCall.InputTerminals[0], NITypes.Int32, 5, false);
            FunctionalNode inspect = ConnectInspectToOutputTerminal(methodCall.OutputTerminals[0]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(callerFunction, calleeFunction);

            byte[] inspectValue = executionInstance.GetLastValueFromInspectNode(inspect);
            AssertByteArrayIsInt32(inspectValue, 6);
        }

        [TestMethod]
        public void FunctionWithCallToFunctionWithTwoOutParameters_Execute_CalleeFunctionExecutesAndReturnsCorrectResults()
        {
            string calleeName = "callee";
            NIType calleeType = DefineFunctionSignatureWithTwoOutParameters(calleeName);
            CompilableDefinitionName calleeDefinitionName = CreateTestCompilableDefinitionName(calleeName);
            DfirRoot calleeFunction = calleeType.CreateFunctionFromSignature(calleeDefinitionName);
            DataAccessor outputDataAccessor0 = DataAccessor.Create(calleeFunction.BlockDiagram, calleeFunction.DataItems[0], Direction.Input);
            DataAccessor outputDataAccessor1 = DataAccessor.Create(calleeFunction.BlockDiagram, calleeFunction.DataItems[1], Direction.Input);
            ConnectConstantToInputTerminal(outputDataAccessor0.Terminal, NITypes.Int32, 5, false);
            ConnectConstantToInputTerminal(outputDataAccessor1.Terminal, NITypes.Int32, 6, false);
            DfirRoot callerFunction = DfirRoot.Create();
            var methodCall = new MethodCallNode(callerFunction.BlockDiagram, calleeDefinitionName, calleeType);
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
            return functionName.DefineMethodType().CreateType();
        }

        private NIType DefineFunctionSignatureWithInAndOutParameters(string functionName)
        {
            return functionName.DefineMethodType().AddInput(NITypes.Int32, "in").AddOutput(NITypes.Int32, "out").CreateType();
        }

        private NIType DefineFunctionSignatureWithTwoOutParameters(string functionName)
        {
            return functionName.DefineMethodType().AddOutput(NITypes.Int32, "out0").AddOutput(NITypes.Int32, "out1").CreateType();
        }
    }
}
