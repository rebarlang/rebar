using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.Compiler.SemanticAnalysis;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler;
using Rebar.Compiler.Nodes;

namespace Tests.Rebar.Unit.Compiler
{
    [TestClass]
    public class OptionPatternStructureTests : CompilerTestBase
    {
        [TestMethod]
        public void OptionPatternStructureWithOptionValueConnectedToSelector_SetVariableTypes_SelectorInnerTerminalOnSomeDiagramHasOptionInnerType()
        {
            DfirRoot function = DfirRoot.Create();
            OptionPatternStructure patternStructure = CreateOptionPatternStructure(function.BlockDiagram);
            FunctionalNode someConstructor = new FunctionalNode(function.BlockDiagram, Signatures.SomeConstructorType);
            Wire.Create(function.BlockDiagram, someConstructor.OutputTerminals[0], patternStructure.Selector.InputTerminals[0]);
            ConnectConstantToInputTerminal(someConstructor.InputTerminals[0], PFTypes.Int32, false);

            RunSemanticAnalysisUpToSetVariableTypes(function);

            VariableReference innerSelectorVariable = patternStructure.Selector.OutputTerminals[0].GetTrueVariable();
            Assert.IsTrue(innerSelectorVariable.Type.IsInt32());
        }

        [TestMethod]
        public void OptionPatternStructureWithUnwiredSelector_ValidateVariableUsages_RequiredTerminalUnconnectedError()
        {
            DfirRoot function = DfirRoot.Create();
            OptionPatternStructure patternStructure = CreateOptionPatternStructure(function.BlockDiagram);

            RunSemanticAnalysisUpToValidation(function);

            OptionPatternStructureSelector selector = patternStructure.Selector;
            Assert.IsTrue(selector.InputTerminals[0].GetDfirMessages().Any(message => message.Descriptor == AllModelsOfComputationErrorMessages.RequiredTerminalUnconnected));
        }

        [TestMethod]
        public void OptionPatternStructureWithOutputTunnelUnwiredOnAnyDiagram_ValidateVariableUsages_RequiredTerminalUnconnectedError()
        {
            DfirRoot function = DfirRoot.Create();
            OptionPatternStructure patternStructure = CreateOptionPatternStructure(function.BlockDiagram);
            Tunnel outputTunnel = CreateOutputTunnel(patternStructure);
            ConnectConstantToInputTerminal(outputTunnel.InputTerminals[0], PFTypes.Int32, false);

            RunSemanticAnalysisUpToValidation(function);

            Assert.IsTrue(outputTunnel.InputTerminals[1].GetDfirMessages().Any(message => message.Descriptor == AllModelsOfComputationErrorMessages.RequiredTerminalUnconnected));
        }
    }
}
