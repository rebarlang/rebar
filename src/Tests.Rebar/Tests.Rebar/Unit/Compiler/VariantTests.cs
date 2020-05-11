using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler;
using Rebar.Compiler.Nodes;

namespace Tests.Rebar.Unit.Compiler
{
    [TestClass]
    public class VariantTests : CompilerTestBase
    {
        [TestMethod]
        public void VariantConstructorWithValidFields_SetVariableTypes_CorrectFieldVariableTypes()
        {
            DfirRoot function = DfirRoot.Create();
            var variantConstructorNode = new VariantConstructorNode(function.BlockDiagram, VariantType, 0);
            ConnectConstantToInputTerminal(variantConstructorNode.InputTerminals[0], NITypes.Int32, false);

            RunSemanticAnalysisUpToSetVariableTypes(function);

            VariableReference variantVariable = variantConstructorNode.VariantOutputTerminal.GetTrueVariable();
            Assert.AreEqual(VariantType, variantVariable.Type);
        }

        [TestMethod]
        public void VariantMatchStructureWithVariantInput_SetVariableTypes_CorrectFieldTypesSetOnSelectorInnerTerminals()
        {
            DfirRoot function = DfirRoot.Create();
            var variantConstructorNode = new VariantConstructorNode(function.BlockDiagram, VariantType, 0);
            ConnectConstantToInputTerminal(variantConstructorNode.InputTerminals[0], NITypes.Int32, false);
            VariantMatchStructure variantMatchStructure = this.CreateVariantMatchStructure(function.BlockDiagram, 2);
            Wire.Create(function.BlockDiagram, variantConstructorNode.VariantOutputTerminal, variantMatchStructure.Selector.InputTerminals[0]);

            RunSemanticAnalysisUpToSetVariableTypes(function);

            VariableReference selectorInnerVariable0 = variantMatchStructure.Selector.OutputTerminals[0].GetTrueVariable();
            Assert.IsTrue(selectorInnerVariable0.Type.IsInt32());
            VariableReference selectorInnerVariable1 = variantMatchStructure.Selector.OutputTerminals[1].GetTrueVariable();
            Assert.IsTrue(selectorInnerVariable1.Type.IsBoolean());
        }

        [TestMethod]
        public void VariantMatchStructureWithNonVariantSelectorInput_ValidateVariableUsages_SelectorInputHasErrorMessage()
        {
            DfirRoot function = DfirRoot.Create();
            VariantMatchStructure variantMatchStructure = this.CreateVariantMatchStructure(function.BlockDiagram, 2);
            ConnectConstantToInputTerminal(variantMatchStructure.Selector.InputTerminals[0], NITypes.Int32, false);

            RunSemanticAnalysisUpToValidation(function);

            Assert.IsTrue(variantMatchStructure.Selector.InputTerminals[0].GetDfirMessages().Any(message => message.Descriptor == Messages.TypeIsNotVariantTypeDescriptor));
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
