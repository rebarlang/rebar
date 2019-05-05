using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler;
using Rebar.Compiler.Nodes;
using Loop = Rebar.Compiler.Nodes.Loop;

namespace Tests.Rebar.Unit.Compiler
{
    [TestClass]
    public class LoopConditionTunnelTests : CompilerTestBase
    {
        [TestMethod]
        public void LoopConditionTunnelWithUnwiredInput_SetVariableTypes_InputVariableIsBooleanType()
        {
            DfirRoot function = DfirRoot.Create();
            Loop loop = new Loop(function.BlockDiagram);
            LoopConditionTunnel loopConditionTunnel = CreateLoopConditionTunnel(loop);

            RunSemanticAnalysisUpToSetVariableTypes(function);

            VariableReference loopConditionInputVariable = loopConditionTunnel.InputTerminals[0].GetTrueVariable();
            Assert.IsTrue(loopConditionInputVariable.Type.IsBoolean());
        }

        [TestMethod]
        public void LoopConditionTunnel_SetVariableTypes_OutputLifetimeIsBoundedAndDoesNotOutlastDiagram()
        {
            DfirRoot function = DfirRoot.Create();
            Loop loop = new Loop(function.BlockDiagram);
            LoopConditionTunnel loopConditionTunnel = CreateLoopConditionTunnel(loop);

            RunSemanticAnalysisUpToSetVariableTypes(function);

            VariableReference loopConditionOutputVariable = loopConditionTunnel.OutputTerminals[0].GetTrueVariable();
            Assert.IsTrue(loopConditionOutputVariable.Type.IsMutableReferenceType());
            Lifetime lifetime = loopConditionOutputVariable.Lifetime;
            Assert.IsTrue(lifetime.IsBounded);
            Assert.IsFalse(lifetime.DoesOutlastDiagram(loop.Diagrams[0]));
        }

        [TestMethod]
        public void LoopConditionTunnel_SetVariableTypes_OutputLifetimeHasCorrectInterruptedVariables()
        {
            DfirRoot function = DfirRoot.Create();
            Loop loop = new Loop(function.BlockDiagram);
            LoopConditionTunnel loopConditionTunnel = CreateLoopConditionTunnel(loop);
            var lifetimeAssociation = new LifetimeVariableAssociation();

            RunSemanticAnalysisUpToSetVariableTypes(function, null, null, lifetimeAssociation);

            VariableReference loopConditionOutputVariable = loopConditionTunnel.OutputTerminals[0].GetTrueVariable(),
                loopConditionInputVariable = loopConditionTunnel.InputTerminals[0].GetTrueVariable();
            Lifetime lifetime = loopConditionOutputVariable.Lifetime;
            IEnumerable<VariableReference> interruptedVariables = lifetimeAssociation.GetVariablesInterruptedByLifetime(lifetime);
            Assert.AreEqual(1, interruptedVariables.Count());
            Assert.AreEqual(loopConditionInputVariable, interruptedVariables.First());
        }

        [TestMethod]
        public void LoopConditionTunnelWithNonBooleanInput_ValidateVariableUsage_TypeConflictErrorReported()
        {
            DfirRoot function = DfirRoot.Create();
            Loop loop = new Loop(function.BlockDiagram);
            LoopConditionTunnel loopConditionTunnel = CreateLoopConditionTunnel(loop);
            ConnectConstantToInputTerminal(loopConditionTunnel.InputTerminals[0], PFTypes.Int32, false);

            RunSemanticAnalysisUpToValidation(function);

            AssertTerminalHasTypeConflictMessage(loopConditionTunnel.InputTerminals[0]);
        }

        [TestMethod]
        public void LoopConditionTunnelWithBooleanReferenceInput_ValidateVariableUsage_TypeConflictErrorReported()
        {
            DfirRoot function = DfirRoot.Create();
            Loop loop = new Loop(function.BlockDiagram);
            LoopConditionTunnel loopConditionTunnel = CreateLoopConditionTunnel(loop);
            ExplicitBorrowNode borrow = ConnectExplicitBorrowToInputTerminals(loopConditionTunnel.InputTerminals[0]);
            ConnectConstantToInputTerminal(borrow.InputTerminals[0], PFTypes.Boolean, false);

            RunSemanticAnalysisUpToValidation(function);

            AssertTerminalHasTypeConflictMessage(loopConditionTunnel.InputTerminals[0]);
        }

        private static LoopConditionTunnel CreateLoopConditionTunnel(Loop loop)
        {
            var loopConditionTunnel = new LoopConditionTunnel(loop);
            var terminateLifetimeDfir = new TerminateLifetimeTunnel(loop);
            loopConditionTunnel.TerminateLifetimeTunnel = terminateLifetimeDfir;
            terminateLifetimeDfir.BeginLifetimeTunnel = loopConditionTunnel;
            return loopConditionTunnel;
        }
    }
}
