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
    public class OptionTypeTests : CompilerTestBase
    {
        [TestMethod]
        public void NoneConstructorWithoutUsage_ValidateVariableUsage_ErrorReported()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode none = new FunctionalNode(function.BlockDiagram, Signatures.NoneConstructorType);

            RunSemanticAnalysisUpToValidation(function);

            Assert.IsTrue(none.OutputTerminals[0].GetDfirMessages().Any(message => message.Descriptor == Messages.TypeNotDetermined.Descriptor));
        }

        [TestMethod]
        public void NoneConstructorWithUsageThatDoesNotDetermineType_ValidateVariableUsage_ErrorReported()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode none = new FunctionalNode(function.BlockDiagram, Signatures.NoneConstructorType);
            FunctionalNode immutablePassthrough = new FunctionalNode(function.BlockDiagram, Signatures.ImmutablePassthroughType);
            Wire.Create(function.BlockDiagram, none.OutputTerminals[0], immutablePassthrough.InputTerminals[0]);

            RunSemanticAnalysisUpToValidation(function);

            Assert.IsTrue(none.OutputTerminals[0].GetDfirMessages().Any(message => message.Descriptor == Messages.TypeNotDetermined.Descriptor));
        }

        [TestMethod]
        public void NoneConstructorLinkedToSomeConstructorWithUndeterminedType_ValidateVariableUsage_ErrorReportedOnNoneButNotSome()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode none = new FunctionalNode(function.BlockDiagram, Signatures.NoneConstructorType);
            FunctionalNode some = new FunctionalNode(function.BlockDiagram, Signatures.SomeConstructorType);
            FunctionalNode select = new FunctionalNode(function.BlockDiagram, Signatures.SelectReferenceType);
            Wire.Create(function.BlockDiagram, none.OutputTerminals[0], select.InputTerminals[1]);
            Wire.Create(function.BlockDiagram, some.OutputTerminals[0], select.InputTerminals[2]);

            RunSemanticAnalysisUpToValidation(function);

            Assert.IsTrue(none.OutputTerminals[0].GetDfirMessages().Any(message => message.Descriptor == Messages.TypeNotDetermined.Descriptor));
            Assert.IsFalse(some.OutputTerminals[0].GetDfirMessages().Any(message => message.Descriptor == Messages.TypeNotDetermined.Descriptor));
        }

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
            Tunnel outputTunnel = CreateOutputTunnel(frame);
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
            Tunnel outputTunnel = CreateOutputTunnel(frame);
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
            Tunnel outputTunnel = CreateOutputTunnel(frame);
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
