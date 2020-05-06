using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler;
using Rebar.Compiler.Nodes;

namespace Tests.Rebar.Unit.Compiler
{
    [TestClass]
    public class StructTests : CompilerTestBase
    {
        [TestMethod]
        public void StructConstructorIntoStructFieldAccessorWithValidFields_SetVariableTypes_CorrectFieldVariableTypes()
        {
            DfirRoot function = DfirRoot.Create();
            var structConstructorNode = new StructConstructorNode(function.BlockDiagram, StructType);
            ConnectConstantToInputTerminal(structConstructorNode.InputTerminals[0], NITypes.Int32, false);
            ConnectConstantToInputTerminal(structConstructorNode.InputTerminals[1], NITypes.Boolean, false);
            var structFieldAccessor = new StructFieldAccessorNode(function.BlockDiagram, new string[] { "_0", "_1" });
            Wire.Create(function.BlockDiagram, structConstructorNode.OutputTerminals[0], structFieldAccessor.StructInputTerminal);

            RunSemanticAnalysisUpToSetVariableTypes(function);

            VariableReference fieldVariable0 = structFieldAccessor.OutputTerminals[0].GetTrueVariable();
            Assert.IsTrue(fieldVariable0.Type.GetReferentType().IsInt32());
            VariableReference fieldVariable1 = structFieldAccessor.OutputTerminals[1].GetTrueVariable();
            Assert.IsTrue(fieldVariable1.Type.GetReferentType().IsBoolean());
        }

        [TestMethod]
        public void StructConstructorIntoStructFieldAccessorWithInvalidField_SetVariableTypes_VoidFieldVariableType()
        {
            DfirRoot function = DfirRoot.Create();
            var structConstructorNode = new StructConstructorNode(function.BlockDiagram, StructType);
            ConnectConstantToInputTerminal(structConstructorNode.InputTerminals[0], NITypes.Int32, false);
            ConnectConstantToInputTerminal(structConstructorNode.InputTerminals[1], NITypes.Boolean, false);
            var structFieldAccessor = new StructFieldAccessorNode(function.BlockDiagram, new string[] { "_2" });
            Wire.Create(function.BlockDiagram, structConstructorNode.OutputTerminals[0], structFieldAccessor.StructInputTerminal);

            RunSemanticAnalysisUpToSetVariableTypes(function);

            VariableReference fieldVariable = structFieldAccessor.OutputTerminals[0].GetTrueVariable();
            Assert.IsTrue(fieldVariable.Type.GetReferentType().IsVoid());
        }

        [TestMethod]
        public void StructConstructorIntoStructFieldAccessorWithNullFieldName_SetVariableTypes_VoidFieldVariableType()
        {
            DfirRoot function = DfirRoot.Create();
            var structConstructorNode = new StructConstructorNode(function.BlockDiagram, StructType);
            ConnectConstantToInputTerminal(structConstructorNode.InputTerminals[0], NITypes.Int32, false);
            ConnectConstantToInputTerminal(structConstructorNode.InputTerminals[1], NITypes.Boolean, false);
            var structFieldAccessor = new StructFieldAccessorNode(function.BlockDiagram, new string[] { null });
            Wire.Create(function.BlockDiagram, structConstructorNode.OutputTerminals[0], structFieldAccessor.StructInputTerminal);

            RunSemanticAnalysisUpToSetVariableTypes(function);

            VariableReference fieldVariable = structFieldAccessor.OutputTerminals[0].GetTrueVariable();
            Assert.IsTrue(fieldVariable.Type.GetReferentType().IsVoid());
        }

        private NIType StructType
        {
            get
            {
                NIClassBuilder builder = NITypes.Factory.DefineValueClass("struct.td");
                builder.DefineField(NITypes.Int32, "_0", NIFieldAccessPolicies.ReadWrite);
                builder.DefineField(NITypes.Boolean, "_1", NIFieldAccessPolicies.ReadWrite);
                return builder.CreateType();
            }
        }
    }
}
