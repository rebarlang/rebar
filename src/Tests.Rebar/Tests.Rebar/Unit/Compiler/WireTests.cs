using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler;
using Rebar.Compiler.Nodes;

namespace Tests.Rebar.Unit.Compiler
{
    [TestClass]
    public class WireTests : CompilerTestBase
    {
        [TestMethod]
        public void BranchedMutableWire_SetVariableTypes_AllSinkVariablesAreMutable()
        {
            NIType signatureType = Signatures.MutablePassthroughType;
            DfirRoot dfirRoot = DfirRoot.Create();
            FunctionalNode firstSink = new FunctionalNode(dfirRoot.BlockDiagram, signatureType),
                secondSink = new FunctionalNode(dfirRoot.BlockDiagram, signatureType);
            Constant constant = Constant.Create(dfirRoot.BlockDiagram, 0, PFTypes.Int32);
            constant.OutputTerminal.WireTogether(firstSink.InputTerminals[0], SourceModelIdSource.NoSourceModelId);
            constant.OutputTerminal.WireTogether(secondSink.InputTerminals[0], SourceModelIdSource.NoSourceModelId);
            Wire branchedWire = (Wire)constant.OutputTerminal.ConnectedTerminal.ParentNode;
            branchedWire.SetWireBeginsMutableVariable(true);

            RunSemanticAnalysisUpToSetVariableTypes(dfirRoot);

            VariableReference firstSinkVariable = firstSink.InputTerminals[0].GetFacadeVariable();
            Assert.IsTrue(firstSinkVariable.Mutable);
            VariableReference secondSinkVariable = secondSink.InputTerminals[0].GetFacadeVariable();
            Assert.IsTrue(secondSinkVariable.Mutable);
        }
    }
}
