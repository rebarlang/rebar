using System.Collections.Generic;
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
    public class BorrowTunnelTests : CompilerTestBase
    {
        [TestMethod]
        public void BorrowTunnelWithImmutableMode_SetVariableTypes_OutputLifetimeIsBoundedAndDoesNotOutlastDiagram()
        {
            DfirRoot function = DfirRoot.Create();
            Frame frame = Frame.Create(function.BlockDiagram);
            var borrowTunnel = CreateBorrowTunnel(frame, BorrowMode.Immutable);
            ConnectConstantToInputTerminal(borrowTunnel.InputTerminals[0], NITypes.Int32, false);

            RunSemanticAnalysisUpToSetVariableTypes(function);

            VariableReference borrowOutputVariable = borrowTunnel.OutputTerminals[0].GetTrueVariable();
            Assert.IsTrue(borrowOutputVariable.Type.IsImmutableReferenceType());
            Lifetime lifetime = borrowOutputVariable.Lifetime;
            Assert.IsTrue(lifetime.IsBounded);
            Assert.IsFalse(lifetime.DoesOutlastDiagram(frame.Diagram));
        }

        [TestMethod]
        public void BorrowTunnelWithMutableModeAndMutableVariableWired_SetVariableTypes_OutputReferenceIsMutable()
        {
            DfirRoot function = DfirRoot.Create();
            Frame frame = Frame.Create(function.BlockDiagram);
            var borrowTunnel = CreateBorrowTunnel(frame, BorrowMode.Mutable);
            ConnectConstantToInputTerminal(borrowTunnel.InputTerminals[0], NITypes.Int32, true);

            RunSemanticAnalysisUpToSetVariableTypes(function);

            VariableReference borrowOutputVariable = borrowTunnel.OutputTerminals[0].GetTrueVariable();
            Assert.IsTrue(borrowOutputVariable.Type.IsMutableReferenceType());
            Lifetime lifetime = borrowOutputVariable.Lifetime;
            Assert.IsTrue(lifetime.IsBounded);
            Assert.IsFalse(lifetime.DoesOutlastDiagram(frame.Diagram));
        }

        [TestMethod]
        public void BorrowTunnelWithMutableModeAndImmutableVariableWired_ValidateVariableUsages_MutableValueRequiredErrorReported()
        {
            DfirRoot function = DfirRoot.Create();
            Frame frame = Frame.Create(function.BlockDiagram);
            var borrowTunnel = CreateBorrowTunnel(frame, BorrowMode.Mutable);
            ConnectConstantToInputTerminal(borrowTunnel.InputTerminals[0], NITypes.Int32, false);

            RunSemanticAnalysisUpToValidation(function);

            Assert.IsTrue(borrowTunnel.InputTerminals[0].GetDfirMessages().Any(message => message.Descriptor == Messages.TerminalDoesNotAcceptImmutableType.Descriptor));
        }

        [TestMethod]
        public void BorrowTunnel_SetVariableTypes_OutputLifetimeHasCorrectInterruptedVariables()
        {
            DfirRoot function = DfirRoot.Create();
            Frame frame = Frame.Create(function.BlockDiagram);
            var borrowTunnel = CreateBorrowTunnel(frame, BorrowMode.Immutable);
            ConnectConstantToInputTerminal(borrowTunnel.InputTerminals[0], NITypes.Int32, false);
            var lifetimeAssociation = new LifetimeVariableAssociation();

            RunSemanticAnalysisUpToSetVariableTypes(function, null, null, lifetimeAssociation);

            VariableReference borrowOutputVariable = borrowTunnel.OutputTerminals[0].GetTrueVariable(),
                borrowInputVariable = borrowTunnel.InputTerminals[0].GetTrueVariable();
            Lifetime lifetime = borrowOutputVariable.Lifetime;
            IEnumerable<VariableReference> interruptedVariables = lifetimeAssociation.GetVariablesInterruptedByLifetime(lifetime);
            Assert.AreEqual(1, interruptedVariables.Count());
            Assert.AreEqual(borrowInputVariable, interruptedVariables.First());
        }
    }
}
