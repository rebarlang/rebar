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
    public class WireTests : CompilerTestBase
    {
        [TestMethod]
        public void Wire_SetVariableTypes_SinkAndSourceTerminalHaveSameVariable()
        {
            NIType signatureType = Signatures.MutablePassthroughType;
            DfirRoot dfirRoot = DfirRoot.Create();
            FunctionalNode sink = new FunctionalNode(dfirRoot.BlockDiagram, signatureType);
            Constant constant = Constant.Create(dfirRoot.BlockDiagram, 0, PFTypes.Int32);
            Wire wire = Wire.Create(dfirRoot.BlockDiagram, constant.OutputTerminal, sink.InputTerminals[0]);

            RunSemanticAnalysisUpToSetVariableTypes(dfirRoot);

            VariableReference wireSourceVariable = wire.SourceTerminal.GetTrueVariable(),
                wireSinkVariable = wire.SinkTerminals[0].GetTrueVariable();
            AssertVariablesReferenceSame(wireSinkVariable, wireSourceVariable);
        }

        [TestMethod]
        public void BranchedWire_SetVariableTypes_AllSinkVariablesGetTypes()
        {
            NIType signatureType = Signatures.MutablePassthroughType;
            DfirRoot dfirRoot = DfirRoot.Create();
            FunctionalNode firstSink = new FunctionalNode(dfirRoot.BlockDiagram, signatureType),
                secondSink = new FunctionalNode(dfirRoot.BlockDiagram, signatureType);
            Constant constant = Constant.Create(dfirRoot.BlockDiagram, 0, PFTypes.Int32);
            constant.OutputTerminal.WireTogether(firstSink.InputTerminals[0], SourceModelIdSource.NoSourceModelId);
            constant.OutputTerminal.WireTogether(secondSink.InputTerminals[0], SourceModelIdSource.NoSourceModelId);

            RunSemanticAnalysisUpToSetVariableTypes(dfirRoot);

            VariableReference firstSinkVariable = firstSink.InputTerminals[0].GetFacadeVariable();
            Assert.IsTrue(firstSinkVariable.Type.IsInt32());
            VariableReference secondSinkVariable = secondSink.InputTerminals[0].GetFacadeVariable();
            Assert.IsTrue(secondSinkVariable.Type.IsInt32());
        }

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

        [TestMethod]
        public void BranchedMutableWireWithNonCopyableType_ValidateVariableUsages_WireCannotForkErrorReported()
        {
            NIType signatureType = Signatures.MutablePassthroughType;
            DfirRoot dfirRoot = DfirRoot.Create();
            FunctionalNode firstSink = new FunctionalNode(dfirRoot.BlockDiagram, signatureType),
                secondSink = new FunctionalNode(dfirRoot.BlockDiagram, signatureType);
            ExplicitBorrowNode mutableBorrow = new ExplicitBorrowNode(dfirRoot.BlockDiagram, BorrowMode.Mutable, 1, true, true);
            ConnectConstantToInputTerminal(mutableBorrow.InputTerminals[0], PFTypes.Int32, true);
            Wire branchedWire = Wire.Create(dfirRoot.BlockDiagram, mutableBorrow.OutputTerminals[0], firstSink.InputTerminals[0], secondSink.InputTerminals[0]);

            RunSemanticAnalysisUpToValidation(dfirRoot);

            Assert.IsTrue(branchedWire.GetDfirMessages().Any(message => message.Descriptor == Messages.WireCannotFork.Descriptor));
        }
    }
}
