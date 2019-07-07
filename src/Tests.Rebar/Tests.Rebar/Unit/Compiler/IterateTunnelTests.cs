using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.Compiler.SemanticAnalysis;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler;
using Rebar.Compiler.Nodes;
using Loop = Rebar.Compiler.Nodes.Loop;

namespace Tests.Rebar.Unit.Compiler
{
    [TestClass]
    public class IterateTunnelTests : CompilerTestBase
    {
        [TestMethod]
        public void IterateTunnelWithRangeIteratorTypeWired_SetVariableTypes_OutputIsInt32()
        {
            DfirRoot function = DfirRoot.Create();
            Loop loop = new Loop(function.BlockDiagram);
            var iterateTunnel = CreateIterateTunnel(loop);
            ConnectRangeWithIntegerInputsToInputTerminal(iterateTunnel.InputTerminals[0]);

            RunSemanticAnalysisUpToSetVariableTypes(function);

            VariableReference iterateOutputVariable = iterateTunnel.OutputTerminals[0].GetTrueVariable();
            Assert.IsTrue(iterateOutputVariable.Type.IsInt32());
        }

#if FALSE
        [TestMethod]
        public void IterateTunnelWithIterableTypeWired_SetVariableTypes_OutputLifetimeHasCorrectInterruptedVariables()
        {
            DfirRoot function = DfirRoot.Create();
            Loop loop = new Loop(function.BlockDiagram);
            var borrowTunnel = CreateIterateTunnel(loop);
            ConnectConstantToInputTerminal(borrowTunnel.InputTerminals[0], PFTypes.Int32, false);
            var lifetimeAssociation = new LifetimeVariableAssociation();

            RunSemanticAnalysisUpToSetVariableTypes(function, null, null, lifetimeAssociation);

            VariableReference borrowOutputVariable = borrowTunnel.OutputTerminals[0].GetTrueVariable(),
                borrowInputVariable = borrowTunnel.InputTerminals[0].GetTrueVariable();
            Lifetime lifetime = borrowOutputVariable.Lifetime;
            IEnumerable<VariableReference> interruptedVariables = lifetimeAssociation.GetVariablesInterruptedByLifetime(lifetime);
            Assert.AreEqual(1, interruptedVariables.Count());
            Assert.AreEqual(borrowInputVariable, interruptedVariables.First());
        }
#endif

        [TestMethod]
        public void IterateTunnelWithImmutableRangeIteratorTypeWired_ValidateVariableUsages_MutableVariableRequiredErrorReported()
        {
            DfirRoot function = DfirRoot.Create();
            Loop loop = new Loop(function.BlockDiagram);
            var iterateTunnel = CreateIterateTunnel(loop);
            ConnectRangeWithIntegerInputsToInputTerminal(iterateTunnel.InputTerminals[0], false);

            RunSemanticAnalysisUpToValidation(function);

            Assert.IsTrue(iterateTunnel.InputTerminals[0].GetDfirMessages().Any(message => message.Descriptor == Messages.TerminalDoesNotAcceptImmutableType.Descriptor));
        }

        [TestMethod]
        public void IterateTunnelWithNonIteratorTypeWired_ValidateVariableUsages_TypeConflictErrorReported()
        {
            DfirRoot function = DfirRoot.Create();
            Loop loop = new Loop(function.BlockDiagram);
            var iterateTunnel = CreateIterateTunnel(loop);
            ConnectConstantToInputTerminal(iterateTunnel.InputTerminals[0], PFTypes.Int32, false);

            RunSemanticAnalysisUpToValidation(function);

            AssertTerminalHasMissingTraitMessage(iterateTunnel.InputTerminals[0]);
        }

        private static IterateTunnel CreateIterateTunnel(Loop loop)
        {
            var iterateTunnel = new IterateTunnel(loop);
            var terminateLifetimeDfir = new TerminateLifetimeTunnel(loop);
            iterateTunnel.TerminateLifetimeTunnel = terminateLifetimeDfir;
            terminateLifetimeDfir.BeginLifetimeTunnel = iterateTunnel;
            return iterateTunnel;
        }

        private FunctionalNode ConnectRangeWithIntegerInputsToInputTerminal(Terminal inputTerminal, bool mutable = true)
        {
            FunctionalNode range = new FunctionalNode(inputTerminal.ParentDiagram, Signatures.RangeType);
            ConnectConstantToInputTerminal(range.InputTerminals[0], PFTypes.Int32, false);
            ConnectConstantToInputTerminal(range.InputTerminals[1], PFTypes.Int32, false);
            Wire wire = Wire.Create(inputTerminal.ParentDiagram, range.OutputTerminals[0], inputTerminal);
            wire.SetWireBeginsMutableVariable(mutable);
            return range;
        }
    }
}
