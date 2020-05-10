using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Compiler.Nodes;

namespace Tests.Rebar.Unit.Execution
{
    [TestClass]
    public class VariantExecutionTests : ExecutionTestBase
    {
        [TestMethod]
        public void VariantConstructorWithValidFields_Execute_CorrectVariantValue()
        {
            DfirRoot function = DfirRoot.Create();
            var variantConstructorNode = new VariantConstructorNode(function.BlockDiagram, VariantType, 0);
            ConnectConstantToInputTerminal(variantConstructorNode.InputTerminals[0], NITypes.Int32, false);
            FunctionalNode inspect = ConnectInspectToOutputTerminal(variantConstructorNode.OutputTerminals[0]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            // TODO: assert structure of variant value
        }

        private NIType VariantType
        {
            get
            {
                NIUnionBuilder builder = NITypes.Factory.DefineUnion("variant.td");
                builder.DefineField(NITypes.Int32, "_0");
                builder.DefineField(NITypes.Boolean, "_1");
                return builder.CreateType();
            }
        }
    }
}
