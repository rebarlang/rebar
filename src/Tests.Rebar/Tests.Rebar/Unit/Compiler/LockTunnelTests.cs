using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.Compiler.SemanticAnalysis;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler;
using Rebar.Compiler.Nodes;

namespace Tests.Rebar.Unit.Compiler
{
    [TestClass]
    public class LockTunnelTests : CompilerTestBase
    {
        [TestMethod]
        public void LockTunnel_SetVariableTypes_OutputLifetimeIsBoundedAndDoesNotOutlastDiagram()
        {
            DfirRoot function = DfirRoot.Create();
            Frame frame = Frame.Create(function.BlockDiagram);
            var lockTunnel = CreateLockTunnel(frame);
            FunctionalNode createLockingCell = ConnectCreateLockingCellToInputTerminal(lockTunnel.InputTerminals[0]);
            ConnectConstantToInputTerminal(createLockingCell.InputTerminals[0], PFTypes.Int32, false);

            RunSemanticAnalysisUpToSetVariableTypes(function);

            VariableReference borrowOutputVariable = lockTunnel.OutputTerminals[0].GetTrueVariable();
            Assert.IsTrue(borrowOutputVariable.Type.IsMutableReferenceType());
            Lifetime lifetime = borrowOutputVariable.Lifetime;
            Assert.IsTrue(lifetime.IsBounded);
            Assert.IsFalse(lifetime.DoesOutlastDiagram(frame.Diagram));
        }

        [TestMethod]
        public void LockTunnel_SetVariableTypes_OutputLifetimeHasCorrectInterruptedVariables()
        {
            DfirRoot function = DfirRoot.Create();
            Frame frame = Frame.Create(function.BlockDiagram);
            var lockTunnel = CreateLockTunnel(frame);
            FunctionalNode createLockingCell = ConnectCreateLockingCellToInputTerminal(lockTunnel.InputTerminals[0]);
            ConnectConstantToInputTerminal(createLockingCell.InputTerminals[0], PFTypes.Int32, false);
            var lifetimeAssociation = new LifetimeVariableAssociation();

            RunSemanticAnalysisUpToSetVariableTypes(function, null, null, lifetimeAssociation);

            VariableReference lockOutputVariable = lockTunnel.OutputTerminals[0].GetTrueVariable(),
                lockInputVariable = lockTunnel.InputTerminals[0].GetTrueVariable();
            Lifetime lifetime = lockOutputVariable.Lifetime;
            IEnumerable<VariableReference> interruptedVariables = lifetimeAssociation.GetVariablesInterruptedByLifetime(lifetime);
            Assert.AreEqual(1, interruptedVariables.Count());
            Assert.AreEqual(lockInputVariable, interruptedVariables.First());
        }

        [TestMethod]
        public void LockTunnelWithNonCellInput_ValidateVariableUsages_TypeConflictErrorReported()
        {
            DfirRoot function = DfirRoot.Create();
            Frame frame = Frame.Create(function.BlockDiagram);
            var lockTunnel = CreateLockTunnel(frame);
            ConnectConstantToInputTerminal(lockTunnel.InputTerminals[0], PFTypes.Int32, false);

            RunSemanticAnalysisUpToValidation(function);

            AssertTerminalHasTypeConflictMessage(lockTunnel.InputTerminals[0]);
        }

        private static LockTunnel CreateLockTunnel(Structure structure)
        {
            var lockTunnel = new LockTunnel(structure);
            var terminateLifetimeDfir = new TerminateLifetimeTunnel(structure);
            lockTunnel.TerminateLifetimeTunnel = terminateLifetimeDfir;
            terminateLifetimeDfir.BeginLifetimeTunnel = lockTunnel;
            return lockTunnel;
        }

        private static FunctionalNode ConnectCreateLockingCellToInputTerminal(Terminal inputTerminal)
        {
            FunctionalNode createLockingCell = new FunctionalNode(inputTerminal.ParentDiagram, Signatures.CreateLockingCellType);
            Wire wire = Wire.Create(inputTerminal.ParentDiagram, createLockingCell.OutputTerminals[0], inputTerminal);
            return createLockingCell;
        }
    }
}
