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
            var structConstructorNode = new VariantConstructorNode(function.BlockDiagram, VariantType, 0);
            ConnectConstantToInputTerminal(structConstructorNode.InputTerminals[0], NITypes.Int32, false);

            RunSemanticAnalysisUpToSetVariableTypes(function);

            VariableReference variantVariable = structConstructorNode.OutputTerminals[0].GetTrueVariable();
            Assert.AreEqual(VariantType, variantVariable.Type);
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
