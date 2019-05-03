using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler;
using Rebar.Compiler.Nodes;

namespace Tests.Rebar.Unit.Compiler
{
    [TestClass]
    public class BorrowNodeTests : CompilerTestBase
    {
        [TestMethod]
        public void ABLAndACRBorrowNodeWithTwoNonReferenceInputsWired_SetVariableTypes_OutputsReferencesInSameLifetime()
        {
            DfirRoot function = DfirRoot.Create();
            ExplicitBorrowNode borrow = new ExplicitBorrowNode(function.BlockDiagram, BorrowMode.Immutable, 2, true, true);
            ConnectConstantToInputTerminal(borrow.InputTerminals[0], PFTypes.Int32, false);
            ConnectConstantToInputTerminal(borrow.InputTerminals[1], PFTypes.Int32, false);

            RunSemanticAnalysisUpToSetVariableTypes(function);

            VariableReference borrowOutput1 = borrow.OutputTerminals[0].GetTrueVariable(),
                borrowOutput2 = borrow.OutputTerminals[1].GetTrueVariable();
            Assert.IsTrue(borrowOutput1.Type.IsImmutableReferenceType());
            Assert.IsTrue(borrowOutput2.Type.IsImmutableReferenceType());
            Assert.AreEqual(borrowOutput1.Lifetime, borrowOutput2.Lifetime);
            Assert.IsTrue(borrowOutput1.Lifetime.IsBounded);
            Assert.IsFalse(borrowOutput1.Lifetime.DoesOutlastDiagram(function.BlockDiagram));
        }

        [TestMethod]
        public void ABLAndACRBorrowNodeWithTwoNonReferenceInputsWired_SetVariableTypes_LifetimeInterruptsExpectedVariables()
        {
            DfirRoot function = DfirRoot.Create();
            ExplicitBorrowNode borrow = new ExplicitBorrowNode(function.BlockDiagram, BorrowMode.Immutable, 2, true, true);
            ConnectConstantToInputTerminal(borrow.InputTerminals[0], PFTypes.Int32, false);
            ConnectConstantToInputTerminal(borrow.InputTerminals[1], PFTypes.Int32, false);
            var lifetimeVariableAssociation = new LifetimeVariableAssociation();

            RunSemanticAnalysisUpToSetVariableTypes(function, null, null, lifetimeVariableAssociation);

            VariableReference borrowOutput = borrow.OutputTerminals[0].GetTrueVariable();
            IEnumerable<VariableReference> interruptedVariables = lifetimeVariableAssociation.GetVariablesInterruptedByLifetime(borrowOutput.Lifetime);
            Assert.AreEqual(2, interruptedVariables.Count());
            Assert.IsTrue(interruptedVariables.Contains(borrow.InputTerminals[0].GetTrueVariable()));
            Assert.IsTrue(interruptedVariables.Contains(borrow.InputTerminals[1].GetTrueVariable()));
        }
    }
}
