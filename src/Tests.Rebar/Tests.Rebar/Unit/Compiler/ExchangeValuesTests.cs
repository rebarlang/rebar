using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.Dfir;
using Rebar.Common;
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
            FunctionalNode exchangeValues = new FunctionalNode(function.BlockDiagram, Signatures.ExchangeValuesType);
        }
    }
}
