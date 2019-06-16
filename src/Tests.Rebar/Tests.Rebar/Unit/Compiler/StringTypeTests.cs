using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler;

namespace Tests.Rebar.Unit.Compiler
{
    [TestClass]
    public class StringTypeTests : CompilerTestBase
    {
        [TestMethod]
        public void StringSliceConstant_SetVariableTypes_OutputsReferenceInStaticLifetime()
        {
            DfirRoot function = DfirRoot.Create();
            Constant constant = Constant.Create(function.BlockDiagram, null, DataTypes.StringSliceType.CreateImmutableReference());

            RunSemanticAnalysisUpToSetVariableTypes(function);

            VariableReference output = constant.OutputTerminal.GetTrueVariable();
            Assert.AreEqual(Lifetime.Static, output.Lifetime);
        }
    }
}
