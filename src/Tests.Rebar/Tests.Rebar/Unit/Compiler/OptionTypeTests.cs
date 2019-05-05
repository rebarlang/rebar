using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public class OptionTypeTests : CompilerTestBase
    {
        [TestMethod]
        public void UnwrapOptionTunnelWithOptionTypeWired_SetVariableTypes_OutputTerminalIsInnerType()
        {
            DfirRoot function = DfirRoot.Create();
            Frame frame = Frame.Create(function.BlockDiagram);
            UnwrapOptionTunnel unwrapOption = CreateUnwrapOptionTunnel(frame);
            FunctionalNode someConstructor = ConnectSomeConstructorToInputTerminal(unwrapOption.InputTerminals[0]);
            ConnectConstantToInputTerminal(someConstructor.InputTerminals[0], PFTypes.Int32, false);

            RunSemanticAnalysisUpToSetVariableTypes(function);

            VariableReference unwrapVariable = unwrapOption.OutputTerminals[0].GetTrueVariable();
            Assert.IsTrue(unwrapVariable.Type.IsInt32());
        }

        [TestMethod]
        public void UnwrapOptionTunnelWithNonOptionTypeWired_ValidateVariableUsages_TypeConflictErrorCreated()
        {
            DfirRoot function = DfirRoot.Create();
            Frame frame = Frame.Create(function.BlockDiagram);
            UnwrapOptionTunnel unwrapOption = CreateUnwrapOptionTunnel(frame);
            ConnectConstantToInputTerminal(unwrapOption.InputTerminals[0], PFTypes.Int32, false);

            RunSemanticAnalysisUpToValidation(function);

            AssertTerminalHasTypeConflictMessage(unwrapOption.InputTerminals[0]);
        }

        [TestMethod]
        public void OutputTunnelWithInnerDiagramReferenceTypeInputOnFrameWithUnwrapOptionTunnel_ValidateVariableUsages_ErrorReported()
        {
            DfirRoot function = DfirRoot.Create();
            Frame frame = Frame.Create(function.BlockDiagram);
            UnwrapOptionTunnel unwrapOption = CreateUnwrapOptionTunnel(frame);
            Tunnel outputTunnel = frame.CreateTunnel(Direction.Output, TunnelMode.LastValue, PFTypes.Void, PFTypes.Void);
            FunctionalNode someConstructor = ConnectSomeConstructorToInputTerminal(outputTunnel.InputTerminals[0]);
            ExplicitBorrowNode borrow = ConnectExplicitBorrowToInputTerminals(someConstructor.InputTerminals[0]);
            ConnectConstantToInputTerminal(borrow.InputTerminals[0], PFTypes.Int32, false);

            RunSemanticAnalysisUpToValidation(function);

            Assert.IsTrue(outputTunnel.InputTerminals[0].GetDfirMessages().Any(message => message.Descriptor == Messages.WiredReferenceDoesNotLiveLongEnough.Descriptor));
        }

        [TestMethod]
        public void OutputTunnelOnFrameWithUnwrapOptionTunnelWithNonOptionInput_SetVariableTypes_OutputTerminalIsOptionType()
        {
            DfirRoot function = DfirRoot.Create();
            Frame frame = Frame.Create(function.BlockDiagram);
            UnwrapOptionTunnel unwrapOption = CreateUnwrapOptionTunnel(frame);
            Tunnel outputTunnel = frame.CreateTunnel(Direction.Output, TunnelMode.LastValue, PFTypes.Void, PFTypes.Void);
            ConnectConstantToInputTerminal(outputTunnel.InputTerminals[0], PFTypes.Int32, false);

            RunSemanticAnalysisUpToSetVariableTypes(function);

            VariableReference outputVariable = outputTunnel.OutputTerminals[0].GetTrueVariable();
            NIType innerType;
            Assert.IsTrue(outputVariable.Type.TryDestructureOptionType(out innerType));
            Assert.IsTrue(innerType.IsInt32());
        }

        [TestMethod]
        public void OutputTunnelOnFrameWithUnwrapOptionTunnelWithOptionInput_SetVariableTypes_OutputTerminalTypeIsNotRewrapped()
        {
            DfirRoot function = DfirRoot.Create();
            Frame frame = Frame.Create(function.BlockDiagram);
            UnwrapOptionTunnel unwrapOption = CreateUnwrapOptionTunnel(frame);
            Tunnel outputTunnel = frame.CreateTunnel(Direction.Output, TunnelMode.LastValue, PFTypes.Void, PFTypes.Void);
            FunctionalNode someConstructor = ConnectSomeConstructorToInputTerminal(outputTunnel.InputTerminals[0]);
            ConnectConstantToInputTerminal(someConstructor.InputTerminals[0], PFTypes.Int32, false);

            RunSemanticAnalysisUpToSetVariableTypes(function);

            VariableReference outputVariable = outputTunnel.OutputTerminals[0].GetTrueVariable();
            NIType innerType;
            Assert.IsTrue(outputVariable.Type.TryDestructureOptionType(out innerType));
            Assert.IsTrue(innerType.IsInt32());
        }

        private static UnwrapOptionTunnel CreateUnwrapOptionTunnel(Frame frame)
        {
            return new UnwrapOptionTunnel(frame);
        }

        internal static FunctionalNode ConnectSomeConstructorToInputTerminal(Terminal inputTerminal)
        {
            FunctionalNode someConstructor = new FunctionalNode(inputTerminal.ParentDiagram, Signatures.SomeConstructorType);
            Wire wire = Wire.Create(inputTerminal.ParentDiagram, someConstructor.OutputTerminals[0], inputTerminal);
            return someConstructor;
        }
    }
}
