using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using NationalInstruments.Linking;
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
            ExtendedQualifiedName calleeQualifiedName = ExtendedQualifiedName.CreateName(new QualifiedName(calleeName), "component", null, ContentId.EmptyId, null);
            DfirRoot calleeFunction = calleeType.CreateFunctionFromSignature(calleeQualifiedName);
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
            DfirRoot calleeFunction = calleeType.CreateFunctionFromSignature(calleeQualifiedName);
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
            DfirRoot calleeFunction = calleeType.CreateFunctionFromSignature(calleeQualifiedName);
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
        public void FunctionWithCallToFunctionWithTwoOutParameters_Execute_CalleeFunctionExecutesAndReturnsCorrectResults()
        {
            string calleeName = "callee";
            NIType calleeType = DefineFunctionSignatureWithTwoOutParameters(calleeName);
            ExtendedQualifiedName calleeQualifiedName = ExtendedQualifiedName.CreateName(new QualifiedName(calleeName), "component", null, ContentId.EmptyId, null);
            DfirRoot calleeFunction = calleeType.CreateFunctionFromSignature(calleeQualifiedName);
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
            return functionName.DefineMethodType().CreateType();
        }

        private NIType DefineFunctionSignatureWithInAndOutParameters(string functionName)
        {
            return functionName.DefineMethodType().AddInput(PFTypes.Int32, "in").AddOutput(PFTypes.Int32, "out").CreateType();
        }

        private NIType DefineFunctionSignatureWithTwoOutParameters(string functionName)
        {
            return functionName.DefineMethodType().AddOutput(PFTypes.Int32, "out0").AddOutput(PFTypes.Int32, "out1").CreateType();
        }
    }
}
