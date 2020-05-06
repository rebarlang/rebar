using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler;
using Rebar.Compiler.Nodes;

namespace Tests.Rebar.Unit.Execution
{
    [TestClass]
    public class OptionPatternStructureExecutionTests : ExecutionTestBase
    {
        [TestMethod]
        public void OptionPatternStructureWithSomeValueWiredToSelector_Execute_SomeDiagramIsExecuted()
        {
            var test = new OptionPatternStructureWithInspectOnEachDiagramTest(this, true);

            test.CompileAndExecuteFunction();

            AssertByteArrayIsInt32(test.SomeInspectNodeValue, 1);
            AssertByteArrayIsInt32(test.NoneInspectNodeValue, 0);
        }

        [TestMethod]
        public void OptionPatternStructureWithNoneValueWiredToSelector_Execute_NoneDiagramIsExecuted()
        {
            var test = new OptionPatternStructureWithInspectOnEachDiagramTest(this, false);

            test.CompileAndExecuteFunction();

            AssertByteArrayIsInt32(test.SomeInspectNodeValue, 0);
            AssertByteArrayIsInt32(test.NoneInspectNodeValue, 1);
        }

        private class OptionPatternStructureWithInspectOnEachDiagramTest
        {
            private readonly OptionPatternStructureExecutionTests _test;
            private readonly DfirRoot _function;
            private readonly FunctionalNode _someInspectNode, _noneInspectNode;

            public OptionPatternStructureWithInspectOnEachDiagramTest(OptionPatternStructureExecutionTests test, bool selectorValueIsSome)
            {
                _test = test;
                _function = DfirRoot.Create();
                OptionPatternStructure patternStructure = _test.CreateOptionPatternStructureWithOptionValueWiredToSelector(
                    _function.BlockDiagram,
                    selectorValueIsSome ? (int?)0 : null);

                _someInspectNode = new FunctionalNode(patternStructure.Diagrams[0], Signatures.InspectType);
                _test.ConnectConstantToInputTerminal(_someInspectNode.InputTerminals[0], NITypes.Int32, 1, false);

                _noneInspectNode = new FunctionalNode(patternStructure.Diagrams[1], Signatures.InspectType);
                _test.ConnectConstantToInputTerminal(_noneInspectNode.InputTerminals[0], NITypes.Int32, 1, false);
            }

            public void CompileAndExecuteFunction()
            {
                var executionInstance = _test.CompileAndExecuteFunction(_function);
                SomeInspectNodeValue = executionInstance.GetLastValueFromInspectNode(_someInspectNode);
                NoneInspectNodeValue = executionInstance.GetLastValueFromInspectNode(_noneInspectNode);
            }

            public byte[] SomeInspectNodeValue { get; set; }

            public byte[] NoneInspectNodeValue { get; set; }
        }

        [TestMethod]
        public void OptionPatternStructureSelectorWiredToInspectOnSomeValueDiagram_Execute_CorrectValue()
        {
            DfirRoot function = DfirRoot.Create();
            OptionPatternStructure patternStructure = CreateOptionPatternStructureWithOptionValueWiredToSelector(function.BlockDiagram, 1);
            FunctionalNode someInspectNode = new FunctionalNode(patternStructure.Diagrams[0], Signatures.InspectType);
            Wire.Create(patternStructure.Diagrams[0], patternStructure.Selector.OutputTerminals[0], someInspectNode.InputTerminals[0]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            byte[] inspectValue = executionInstance.GetLastValueFromInspectNode(someInspectNode);
            AssertByteArrayIsInt32(inspectValue, 1);
        }

        [TestMethod]
        public void OptionPatternStructureWithOutputTunnelAndSomeValueWiredToSelector_Execute_CorrectValueFromOutputTunnel()
        {
            DfirRoot function = CreateOptionPatternStructureWithOutputTunnelAndInspect(true, 1, 0);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            var inspectNode = function.BlockDiagram.Nodes.OfType<FunctionalNode>().Where(f => f.Signature == Signatures.InspectType).First();
            byte[] inspectValue = executionInstance.GetLastValueFromInspectNode(inspectNode);
            AssertByteArrayIsInt32(inspectValue, 1);
        }

        [TestMethod]
        public void OptionPatternStructureWithOutputTunnelAndNoneValueWiredToSelector_Execute_CorrectValueFromOutputTunnel()
        {
            DfirRoot function = CreateOptionPatternStructureWithOutputTunnelAndInspect(false, 0, 1);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            var inspectNode = function.BlockDiagram.Nodes.OfType<FunctionalNode>().Where(f => f.Signature == Signatures.InspectType).First();
            byte[] inspectValue = executionInstance.GetLastValueFromInspectNode(inspectNode);
            AssertByteArrayIsInt32(inspectValue, 1);
        }

        private DfirRoot CreateOptionPatternStructureWithOutputTunnelAndInspect(bool selectorValueIsSome, int someDiagramTunnelValue, int noneDiagramTunnelValue)
        {
            DfirRoot function = DfirRoot.Create();
            OptionPatternStructure patternStructure = CreateOptionPatternStructureWithOptionValueWiredToSelector(
                function.BlockDiagram,
                selectorValueIsSome ? (int?)0 : null);
            Tunnel outputTunnel = CreateOutputTunnel(patternStructure);
            ConnectConstantToInputTerminal(outputTunnel.InputTerminals[0], NITypes.Int32, someDiagramTunnelValue, false);
            ConnectConstantToInputTerminal(outputTunnel.InputTerminals[1], NITypes.Int32, noneDiagramTunnelValue, false);
            ConnectInspectToOutputTerminal(outputTunnel.OutputTerminals[0]);
            return function;
        }

        private OptionPatternStructure CreateOptionPatternStructureWithOptionValueWiredToSelector(Diagram parentDiagram, int? selectorValue)
        {
            OptionPatternStructure patternStructure = CreateOptionPatternStructure(parentDiagram);
            FunctionalNode someConstructor = new FunctionalNode(parentDiagram, Signatures.SomeConstructorType);
            if (selectorValue != null)
            {
                Wire.Create(parentDiagram, someConstructor.OutputTerminals[0], patternStructure.Selector.InputTerminals[0]);
                ConnectConstantToInputTerminal(someConstructor.InputTerminals[0], NITypes.Int32, selectorValue, false);
            }
            else
            {
                ConnectConstantToInputTerminal(someConstructor.InputTerminals[0], NITypes.Int32, 0, false);
                FunctionalNode assign = new FunctionalNode(parentDiagram, Signatures.AssignType);
                Wire optionValueWire = Wire.Create(parentDiagram, someConstructor.OutputTerminals[0], assign.InputTerminals[0]);
                optionValueWire.SetWireBeginsMutableVariable(true);
                FunctionalNode noneConstructor = new FunctionalNode(parentDiagram, Signatures.NoneConstructorType);
                Wire.Create(parentDiagram, noneConstructor.OutputTerminals[0], assign.InputTerminals[1]);
                Wire.Create(parentDiagram, assign.OutputTerminals[0], patternStructure.Selector.InputTerminals[0]);
            }
            return patternStructure;
        }
    }
}
