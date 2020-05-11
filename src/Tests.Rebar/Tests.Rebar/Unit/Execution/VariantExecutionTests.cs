using System;
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
        public void VariantConstructorsWithValidFields_Execute_CorrectVariantValue()
        {
            DfirRoot function = DfirRoot.Create();
            var variantConstructorNodeInt = new VariantConstructorNode(function.BlockDiagram, VariantType, 0);
            ConnectConstantToInputTerminal(variantConstructorNodeInt.InputTerminals[0], NITypes.Int32, 5, false);
            FunctionalNode inspectInt = ConnectInspectToOutputTerminal(variantConstructorNodeInt.VariantOutputTerminal);
            var variantConstructorNodeBool = new VariantConstructorNode(function.BlockDiagram, VariantType, 1);
            ConnectConstantToInputTerminal(variantConstructorNodeBool.InputTerminals[0], NITypes.Boolean, true, false);
            FunctionalNode inspectBool = ConnectInspectToOutputTerminal(variantConstructorNodeBool.VariantOutputTerminal);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            byte[] inspectIntValue = executionInstance.GetLastValueFromInspectNode(inspectInt);
            Assert.AreEqual((byte)0, inspectIntValue[0]);
            Assert.AreEqual(5, BitConverter.ToInt32(inspectIntValue, 1));
            byte[] inspectBoolValue = executionInstance.GetLastValueFromInspectNode(inspectBool);
            Assert.AreEqual((byte)1, inspectBoolValue[0]);
            Assert.AreEqual((byte)1, inspectBoolValue[1]);
        }

        [TestMethod]
        public void VariantMatchStructure_Execute_CorrectFrameExecutes()
        {
            DfirRoot function = DfirRoot.Create();
            var variantConstructorNodeInt = new VariantConstructorNode(function.BlockDiagram, VariantType, 0);
            ConnectConstantToInputTerminal(variantConstructorNodeInt.InputTerminals[0], NITypes.Int32, 5, false);
            VariantMatchStructure variantMatchStructure = this.CreateVariantMatchStructure(function.BlockDiagram, 2);
            Wire.Create(function.BlockDiagram, variantConstructorNodeInt.VariantOutputTerminal, variantMatchStructure.Selector.InputTerminals[0]);
            this.ConnectOutputToOutputTerminal(variantMatchStructure.Selector.OutputTerminals[0]);
            this.ConnectOutputToOutputTerminal(variantMatchStructure.Selector.OutputTerminals[1]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            Assert.AreEqual("5", executionInstance.RuntimeServices.LastOutputValue);
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
