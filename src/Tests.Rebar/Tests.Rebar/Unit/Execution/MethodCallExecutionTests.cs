using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.Compiler;
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
        public void FunctionWithCallToFunction_Execute_CalleeFunctionExecutesAndReturnsCorrectResult()
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

        private NIType DefineFunctionSignatureWithInAndOutParameters(string functionName)
        {
            NIFunctionBuilder functionBuilder = PFTypes.Factory.DefineFunction(functionName);
            functionBuilder.IsStatic = true;
            functionBuilder.DefineParameter(PFTypes.Int32, "in", NIParameterPassingRule.Required, NIParameterPassingRule.NotAllowed, "in");
            functionBuilder.DefineParameter(PFTypes.Int32, "out", NIParameterPassingRule.NotAllowed, NIParameterPassingRule.Optional, "out");
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
