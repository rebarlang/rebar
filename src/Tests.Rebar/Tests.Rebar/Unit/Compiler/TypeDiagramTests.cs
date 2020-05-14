using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.Compiler;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler.Nodes;
using Rebar.Compiler.TypeDiagram;

namespace Tests.Rebar.Unit.Compiler
{
    [TestClass]
    public class TypeDiagramTests : CompilerTestBase
    {
        [TestMethod]
        public void TypeDiagram_InferTypes_DefinedTypeNameMatchesDfirRootName()
        {
            DfirRoot typeDiagram = DfirRoot.Create(CreateTestCompilableDefinitionName("type"));
            var selfTypeNode = new SelfTypeNode(typeDiagram.BlockDiagram, SelfTypeMode.Struct, 1);
            ConnectPrimitiveTypeToInputTerminal(selfTypeNode.InputTerminals[0], NITypes.Int32);

            RunSemanticAnalysisUpToTypeInference(typeDiagram);

            NIType selfType = typeDiagram.GetSelfType();
            Assert.AreEqual("type", selfType.GetName());
        }

        [TestMethod]
        public void PrimitiveTypeIntoSelf_InferTypes_SelfTypeIsPrimitiveType()
        {
            DfirRoot typeDiagram = DfirRoot.Create(CreateTestCompilableDefinitionName("type"));
            var selfTypeNode = new SelfTypeNode(typeDiagram.BlockDiagram, SelfTypeMode.Struct, 1);
            ConnectPrimitiveTypeToInputTerminal(selfTypeNode.InputTerminals[0], NITypes.Int32);

            RunSemanticAnalysisUpToTypeInference(typeDiagram);

            NIType selfType = typeDiagram.GetSelfType();
            Assert.IsTrue(selfType.IsValueClass());
            Assert.AreEqual(1, selfType.GetFields().Count());
            Assert.IsTrue(selfType.GetFields().ElementAt(0).GetDataType().IsInt32());
        }

        [TestMethod]
        public void PrimitivesIntoSelfStructType_InferTypes_SelfTypeIsClusterType()
        {
            DfirRoot typeDiagram = DfirRoot.Create(CreateTestCompilableDefinitionName("type"));
            var selfTypeNode = new SelfTypeNode(typeDiagram.BlockDiagram, SelfTypeMode.Struct, 2);
            ConnectPrimitiveTypeToInputTerminal(selfTypeNode.InputTerminals[0], NITypes.Int32);
            ConnectPrimitiveTypeToInputTerminal(selfTypeNode.InputTerminals[1], NITypes.Boolean);

            RunSemanticAnalysisUpToTypeInference(typeDiagram);

            NIType selfType = typeDiagram.GetSelfType();
            Assert.IsTrue(selfType.IsValueClass());
            Assert.AreEqual(2, selfType.GetFields().Count());
        }

        [TestMethod]
        public void PrimitivesIntoSelfVariantType_InferTypes_SelfTypeIsUnionType()
        {
            DfirRoot typeDiagram = DfirRoot.Create(CreateTestCompilableDefinitionName("type"));
            var selfTypeNode = new SelfTypeNode(typeDiagram.BlockDiagram, SelfTypeMode.Variant, 2);
            ConnectPrimitiveTypeToInputTerminal(selfTypeNode.InputTerminals[0], NITypes.Int32);
            ConnectPrimitiveTypeToInputTerminal(selfTypeNode.InputTerminals[1], NITypes.Boolean);

            RunSemanticAnalysisUpToTypeInference(typeDiagram);

            NIType selfType = typeDiagram.GetSelfType();
            Assert.IsTrue(selfType.IsUnion());
            Assert.AreEqual(2, selfType.GetFields().Count());
        }

        [TestMethod]
        public void SelfVariantTypeWithUnwiredInput_InferTypes_SelfTypeIsUnionTypeWithEmptyField()
        {
            DfirRoot typeDiagram = DfirRoot.Create(CreateTestCompilableDefinitionName("type"));
            var selfTypeNode = new SelfTypeNode(typeDiagram.BlockDiagram, SelfTypeMode.Variant, 1);

            RunSemanticAnalysisUpToTypeInference(typeDiagram);

            NIType selfType = typeDiagram.GetSelfType();
            Assert.IsTrue(selfType.IsUnion());
            Assert.AreEqual(1, selfType.GetFields().Count());
            Assert.IsTrue(selfType.GetFields().First().IsUnit());
        }

        [TestMethod]
        public void UnwiredStructSelfTypeNode_Validate_SelfTypeNodeInputTerminalHasUnwiredTerminalError()
        {
            DfirRoot typeDiagram = DfirRoot.Create(CreateTestCompilableDefinitionName("type"));
            var selfType = new SelfTypeNode(typeDiagram.BlockDiagram, SelfTypeMode.Struct, 1);

            RunTypeDiagramSemanticAnalysisUpToValidation(typeDiagram);

            AssertTerminalHasRequiredTerminalUnconnectedMessage(selfType.InputTerminals[0]);
        }

        [TestMethod]
        public void UnwiredVariantSelfTypeNode_Validate_SelfTypeNodeInputTerminalHasUnwiredTerminalError()
        {
            DfirRoot typeDiagram = DfirRoot.Create(CreateTestCompilableDefinitionName("type"));
            var selfType = new SelfTypeNode(typeDiagram.BlockDiagram, SelfTypeMode.Variant, 1);

            RunTypeDiagramSemanticAnalysisUpToValidation(typeDiagram);

            Assert.IsFalse(selfType.InputTerminals[0].GetDfirMessages().Any());
        }

        private PrimitiveTypeNode ConnectPrimitiveTypeToInputTerminal(Terminal inputTerminal, NIType type)
        {
            var primitive = new PrimitiveTypeNode(inputTerminal.ParentDiagram, type);
            Wire.Create(inputTerminal.ParentDiagram, primitive.OutputTerminal, inputTerminal);
            return primitive;
        }

        protected void RunSemanticAnalysisUpToTypeInference(DfirRoot typeDiagram, CompileCancellationToken cancellationToken = null)
        {
            cancellationToken = cancellationToken ?? new CompileCancellationToken();
            new CreateTypeDiagramNodeFacadesTransform().Execute(typeDiagram, cancellationToken);
            new UnifyTypesAcrossWiresTransform().Execute(typeDiagram, cancellationToken);
        }

        protected void RunTypeDiagramSemanticAnalysisUpToValidation(DfirRoot typeDiagram)
        {
            var cancellationToken = new CompileCancellationToken();
            RunSemanticAnalysisUpToTypeInference(typeDiagram, cancellationToken);
            new ValidateTypeUsagesTransform().Execute(typeDiagram, cancellationToken);
        }
    }
}
