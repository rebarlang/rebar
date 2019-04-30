using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.Dfir;
using Rebar.Compiler.Nodes;

namespace Tests.Rebar.Unit.Compiler
{
    [TestClass]
    public class ExchangeValuesTests : CompilerTestBase
    {
        [TestMethod]
        public void ExchangeValuesNode_FullyCompile()
        {
            DfirRoot function = DfirRoot.Create();
            ExchangeValuesNode exchangeValues = new ExchangeValuesNode(function.ParentDiagram);
        }
    }
}
