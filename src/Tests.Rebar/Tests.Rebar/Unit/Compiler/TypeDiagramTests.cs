using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.Compiler;
using NationalInstruments.Compiler.SemanticAnalysis;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Compiler;
using Rebar.Compiler.Nodes;
using Rebar.Compiler.TypeDiagram;

namespace Tests.Rebar.Unit.Compiler
{
    [TestClass]
    public class TypeDiagramTests : CompilerTestBase
    {
        [TestMethod]
        public void PrimitiveTypeIntoSelf_InferTypes_SelfTypeIsPrimitiveType()
        {
            DfirRoot typeDiagram = DfirRoot.Create();
            var primitive = new PrimitiveTypeNode(typeDiagram.BlockDiagram, PFTypes.Int32);
            var selfType = new SelfTypeNode(typeDiagram.BlockDiagram);
            Wire.Create(typeDiagram.BlockDiagram, primitive.OutputTerminal, selfType.InputTerminal);

            RunSemanticAnalysisUpToTypeInference(typeDiagram);

            Assert.IsTrue(typeDiagram.GetSelfType().IsInt32());
        }

        [TestMethod]
        public void UnwiredSelfTypeNode_Validate_SelfTypeNodeInputTerminalHasUnwiredTerminalError()
        {
            DfirRoot typeDiagram = DfirRoot.Create();
            var selfType = new SelfTypeNode(typeDiagram.BlockDiagram);

            RunTypeDiagramSemanticAnalysisUpToValidation(typeDiagram);

            AssertTerminalHasRequiredTerminalUnconnectedMessage(selfType.InputTerminal);
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
