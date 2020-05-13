using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
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
        public void VariantConstructorContainingDroppableValue_Execute_ValueIsDropped()
        {
            DfirRoot function = DfirRoot.Create();
            var variantConstructorNode = new VariantConstructorNode(function.BlockDiagram, VariantWithDropField, 1);
            FunctionalNode createFakeDrop = CreateFakeDropWithId(function.BlockDiagram, 1);
            Wire.Create(function.BlockDiagram, createFakeDrop.OutputTerminals[0], variantConstructorNode.InputTerminals[0]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            Assert.IsTrue(executionInstance.RuntimeServices.DroppedFakeDropIds.Contains(1));
        }

        [TestMethod]
        public void VariantMatchStructureWithTwoCasesAndFirstElementInput_Execute_CorrectFrameExecutes()
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

        [TestMethod]
        public void VariantMatchStructureWithTwoCasesAndSecondElementInput_Execute_CorrectFrameExecutes()
        {
            DfirRoot function = DfirRoot.Create();
            var variantConstructorNodeInt = new VariantConstructorNode(function.BlockDiagram, VariantType, 1);
            ConnectConstantToInputTerminal(variantConstructorNodeInt.InputTerminals[0], NITypes.Boolean, true, false);
            VariantMatchStructure variantMatchStructure = this.CreateVariantMatchStructure(function.BlockDiagram, 2);
            Wire.Create(function.BlockDiagram, variantConstructorNodeInt.VariantOutputTerminal, variantMatchStructure.Selector.InputTerminals[0]);
            this.ConnectOutputToOutputTerminal(variantMatchStructure.Selector.OutputTerminals[0]);
            this.ConnectOutputToOutputTerminal(variantMatchStructure.Selector.OutputTerminals[1]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            Assert.AreEqual("true", executionInstance.RuntimeServices.LastOutputValue);
        }

        [TestMethod]
        public void VariantMatchStructureWithThreeCasesAndThirdElementInput_Execute_CorrectFrameExecutes()
        {
            DfirRoot function = DfirRoot.Create();
            var variantConstructorNodeInt = new VariantConstructorNode(function.BlockDiagram, ThreeFieldVariantType, 2);
            ConnectConstantToInputTerminal(variantConstructorNodeInt.InputTerminals[0], NITypes.Int16, (short)5, false);
            VariantMatchStructure variantMatchStructure = this.CreateVariantMatchStructure(function.BlockDiagram, 3);
            Wire.Create(function.BlockDiagram, variantConstructorNodeInt.VariantOutputTerminal, variantMatchStructure.Selector.InputTerminals[0]);
            this.ConnectOutputToOutputTerminal(variantMatchStructure.Selector.OutputTerminals[2]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            Assert.AreEqual("5", executionInstance.RuntimeServices.LastOutputValue);
        }

        [TestMethod]
        public void VariantMatchStructureWithInputTunnel_Execute_InputTunnelValueCorrectlyTransferred()
        {
            DfirRoot function = DfirRoot.Create();
            var variantConstructorNodeInt = new VariantConstructorNode(function.BlockDiagram, VariantType, 0);
            ConnectConstantToInputTerminal(variantConstructorNodeInt.InputTerminals[0], NITypes.Int32, 5, false);
            VariantMatchStructure variantMatchStructure = this.CreateVariantMatchStructure(function.BlockDiagram, 2);
            Wire.Create(function.BlockDiagram, variantConstructorNodeInt.VariantOutputTerminal, variantMatchStructure.Selector.InputTerminals[0]);
            Tunnel inputTunnel = CreateInputTunnel(variantMatchStructure);
            ConnectConstantToInputTerminal(inputTunnel.InputTerminals[0], NITypes.Int32, 5, false);
            this.ConnectOutputToOutputTerminal(inputTunnel.OutputTerminals[0]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            Assert.AreEqual("5", executionInstance.RuntimeServices.LastOutputValue);
        }

        [TestMethod]
        public void VariantMatchStructureWithOutputTunnelAndFirstFieldInputValue_Execute_OutputTunnelHasCorrectValue()
        {
            DfirRoot function = DfirRoot.Create();
            var variantConstructorNodeInt = new VariantConstructorNode(function.BlockDiagram, VariantType, 0);
            ConnectConstantToInputTerminal(variantConstructorNodeInt.InputTerminals[0], NITypes.Int32, 5, false);
            VariantMatchStructure variantMatchStructure = this.CreateVariantMatchStructure(function.BlockDiagram, 2);
            Wire.Create(function.BlockDiagram, variantConstructorNodeInt.VariantOutputTerminal, variantMatchStructure.Selector.InputTerminals[0]);
            Tunnel outputTunnel = CreateOutputTunnel(variantMatchStructure);
            ConnectConstantToInputTerminal(outputTunnel.InputTerminals[0], NITypes.Int32, 5, false);
            ConnectConstantToInputTerminal(outputTunnel.InputTerminals[1], NITypes.Int32, 6, false);
            this.ConnectOutputToOutputTerminal(outputTunnel.OutputTerminals[0]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            Assert.AreEqual("5", executionInstance.RuntimeServices.LastOutputValue);
        }

        [TestMethod]
        public void VariantMatchStructureWithOutputTunnelAndSecondFieldInputValue_Execute_OutputTunnelHasCorrectValue()
        {
            DfirRoot function = DfirRoot.Create();
            var variantConstructorNodeInt = new VariantConstructorNode(function.BlockDiagram, VariantType, 1);
            ConnectConstantToInputTerminal(variantConstructorNodeInt.InputTerminals[0], NITypes.Boolean, true, false);
            VariantMatchStructure variantMatchStructure = this.CreateVariantMatchStructure(function.BlockDiagram, 2);
            Wire.Create(function.BlockDiagram, variantConstructorNodeInt.VariantOutputTerminal, variantMatchStructure.Selector.InputTerminals[0]);
            Tunnel outputTunnel = CreateOutputTunnel(variantMatchStructure);
            ConnectConstantToInputTerminal(outputTunnel.InputTerminals[0], NITypes.Int32, 5, false);
            ConnectConstantToInputTerminal(outputTunnel.InputTerminals[1], NITypes.Int32, 6, false);
            this.ConnectOutputToOutputTerminal(outputTunnel.OutputTerminals[0]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            Assert.AreEqual("6", executionInstance.RuntimeServices.LastOutputValue);
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

        private NIType VariantWithDropField
        {
            get
            {
                NIUnionBuilder builder = NITypes.Factory.DefineUnion("dropVariant.td");
                builder.DefineField(NITypes.Int32, "_0");
                builder.DefineField(DataTypes.FakeDropType, "_1");
                return builder.CreateType();
            }
        }

        private NIType ThreeFieldVariantType
        {
            get
            {
                NIUnionBuilder builder = NITypes.Factory.DefineUnion("threeFieldVariant.td");
                builder.DefineField(NITypes.Int32, "_0");
                builder.DefineField(NITypes.Boolean, "_1");
                builder.DefineField(NITypes.Int16, "_2");
                return builder.CreateType();
            }
        }
    }
}
