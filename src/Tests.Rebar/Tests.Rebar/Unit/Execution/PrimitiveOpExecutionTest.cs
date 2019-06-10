using System;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Compiler.Nodes;

namespace Tests.Rebar.Unit.Execution
{
    public abstract class PrimitiveOpExecutionTest : ExecutionTestBase
    {
        protected void TestPrimitiveOperation(
            NIType operationSignature,
            object leftValue,
            object rightValue,
            NIType inputType,
            bool mutating,
            Action<byte[]> testExpectedValue)
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode functionNode = new FunctionalNode(function.BlockDiagram, operationSignature);
            Constant leftValueConstant = ConnectConstantToInputTerminal(functionNode.InputTerminals[0], inputType, mutating);
            leftValueConstant.Value = leftValue;
            int lastIndex = 2;
            if (rightValue != null)
            {
                Constant rightValueConstant = ConnectConstantToInputTerminal(functionNode.InputTerminals[1], inputType, false);
                rightValueConstant.Value = rightValue;
            }
            else
            {
                lastIndex = 1;
            }
            FunctionalNode inspect = ConnectInspectToOutputTerminal(mutating
                ? functionNode.OutputTerminals[0]
                : functionNode.OutputTerminals[lastIndex]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            byte[] inspectValue = executionInstance.GetLastValueFromInspectNode(inspect);
            testExpectedValue(inspectValue);
        }
    }
}
