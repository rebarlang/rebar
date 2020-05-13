using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.CommonModel;
using NationalInstruments.Compiler;
using NationalInstruments.Compiler.SemanticAnalysis;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using NationalInstruments.ExecutionFramework;
using Rebar.Common;
using Rebar.Compiler;
using Rebar.Compiler.Nodes;
using Rebar.RebarTarget;
using Rebar.RebarTarget.LLVM;
using Loop = Rebar.Compiler.Nodes.Loop;

namespace Tests.Rebar.Unit.Compiler
{
    public class CompilerTestBase
    {
        protected CompilableDefinitionName CreateTestCompilableDefinitionName(string definitionName)
        {
            return new CompilableDefinitionName(new QualifiedName(definitionName), "component");
        }

        protected void RunSemanticAnalysisUpToCreateNodeFacades(DfirRoot dfirRoot, CompileCancellationToken cancellationToken = null)
        {
            ExecutionOrderSortingVisitor.SortDiagrams(dfirRoot);
            cancellationToken = cancellationToken ?? new CompileCancellationToken();
            new CreateNodeFacadesTransform().Execute(dfirRoot, cancellationToken);
        }

        internal void RunSemanticAnalysisUpToSetVariableTypes(
            DfirRoot dfirRoot, 
            CompileCancellationToken cancellationToken = null,
            TerminalTypeUnificationResults unificationResults = null,
            LifetimeVariableAssociation lifetimeVariableAssociation = null)
        {
            cancellationToken = cancellationToken ?? new CompileCancellationToken();
            unificationResults = unificationResults ?? new TerminalTypeUnificationResults();
            lifetimeVariableAssociation = lifetimeVariableAssociation ?? new LifetimeVariableAssociation();
            RunSemanticAnalysisUpToCreateNodeFacades(dfirRoot, cancellationToken);
            new MergeVariablesAcrossWiresTransform(lifetimeVariableAssociation, unificationResults).Execute(dfirRoot, cancellationToken);
            new FinalizeAutoBorrowsTransform().Execute(dfirRoot, cancellationToken);
            new MarkConsumedVariablesTransform(lifetimeVariableAssociation).Execute(dfirRoot, cancellationToken);
        }

        internal void RunSemanticAnalysisUpToValidation(
            DfirRoot dfirRoot, 
            CompileCancellationToken cancellationToken = null,
            LifetimeVariableAssociation lifetimeVariableAssociation = null)
        {
            cancellationToken = cancellationToken ?? new CompileCancellationToken();
            var unificationResults = new TerminalTypeUnificationResults();
            RunSemanticAnalysisUpToSetVariableTypes(dfirRoot, cancellationToken, unificationResults, lifetimeVariableAssociation);
            new ValidateVariableUsagesTransform(unificationResults).Execute(dfirRoot, cancellationToken);
        }

        protected void RunCompilationUpToAutomaticNodeInsertion(DfirRoot dfirRoot, CompileCancellationToken cancellationToken = null)
        {
            cancellationToken = cancellationToken ?? new CompileCancellationToken();
            var lifetimeVariableAssociation = new LifetimeVariableAssociation();
            RunSemanticAnalysisUpToValidation(dfirRoot, cancellationToken, lifetimeVariableAssociation);

            if (DfirMessageHelper.CalculateIsBroken(dfirRoot))
            {
                var messageBuilder = new StringBuilder("Compilation failed because DfirRoot has semantic errors:\n");
                foreach (DfirNodeMessagePair messagePair in DfirMessageHelper.ListAllNodeUserMessages(dfirRoot, false))
                {
                    messageBuilder.AppendLine($"{messagePair.Node}: {messagePair.Message.Descriptor}");
                }
                Assert.Fail(messageBuilder.ToString());
            }

            new AutoBorrowTransform(lifetimeVariableAssociation).Execute(dfirRoot, cancellationToken);
            var nodeInsertionTypeUnificationResultFactory = new NodeInsertionTypeUnificationResultFactory();
            new InsertTerminateLifetimeTransform(lifetimeVariableAssociation, nodeInsertionTypeUnificationResultFactory)
                .Execute(dfirRoot, cancellationToken);
            new InsertDropTransform(lifetimeVariableAssociation, nodeInsertionTypeUnificationResultFactory)
                .Execute(dfirRoot, cancellationToken);
        }

        protected void RunCompilationUpToAsyncNodeDecomposition(DfirRoot dfirRoot, CompileCancellationToken cancellationToken = null)
        {
            cancellationToken = cancellationToken ?? new CompileCancellationToken();
            RunCompilationUpToAutomaticNodeInsertion(dfirRoot, cancellationToken);
            var nodeInsertionTypeUnificationResultFactory = new NodeInsertionTypeUnificationResultFactory();
            var emptyDictionary = new Dictionary<CompilableDefinitionName, bool>();
            new AsyncNodeDecompositionTransform(emptyDictionary, emptyDictionary, nodeInsertionTypeUnificationResultFactory)
                .Execute(dfirRoot, cancellationToken);
        }

        internal FunctionCompileResult RunSemanticAnalysisUpToLLVMCodeGeneration(
            DfirRoot dfirRoot,
            string compiledFunctionName,
            Dictionary<CompilableDefinitionName, bool> calleesIsYielding,
            Dictionary<CompilableDefinitionName, bool> calleesMayPanic)
        {
            var cancellationToken = new CompileCancellationToken();
            RunCompilationUpToAutomaticNodeInsertion(dfirRoot, cancellationToken);
            return FunctionCompileHandler.CompileFunctionForLLVM(dfirRoot, cancellationToken, calleesIsYielding, calleesMayPanic, compiledFunctionName);
        }

        protected NIType DefineGenericOutputFunctionSignature()
        {
            NIFunctionBuilder functionBuilder = NITypes.Factory.DefineFunction("genericOutput");
            NIType typeParameter = Signatures.AddGenericDataTypeParameter(functionBuilder, "TData");
            return functionBuilder.AddOutput(typeParameter, "out").CreateType();
        }

        protected Constant ConnectConstantToInputTerminal(Terminal inputTerminal, NIType variableType, bool mutable)
        {
            Constant constant = Constant.Create(inputTerminal.ParentDiagram, variableType.CreateDefaultValue(), variableType);
            Wire wire = Wire.Create(inputTerminal.ParentDiagram, constant.OutputTerminal, inputTerminal);
            wire.SetWireBeginsMutableVariable(mutable);
            return constant;
        }

        internal static ExplicitBorrowNode ConnectExplicitBorrowToInputTerminals(params Terminal[] inputTerminals)
        {
            return ConnectExplicitBorrowToInputTerminals(BorrowMode.Immutable, inputTerminals);
        }

        internal static ExplicitBorrowNode ConnectExplicitBorrowToInputTerminals(BorrowMode borrowMode, params Terminal[] inputTerminals)
        {
            Diagram parentDiagram = inputTerminals[0].ParentDiagram;
            ExplicitBorrowNode borrow = new ExplicitBorrowNode(parentDiagram, borrowMode, inputTerminals.Length, true, true);
            for (int i = 0; i < inputTerminals.Length; ++i)
            {
                Wire.Create(parentDiagram, borrow.OutputTerminals[i], inputTerminals[i]);
            }
            return borrow;
        }

        internal static BorrowTunnel CreateBorrowTunnel(Structure structure, BorrowMode borrowMode)
        {
            var borrowTunnel = new BorrowTunnel(structure, borrowMode);
            var terminateLifetimeDfir = new TerminateLifetimeTunnel(structure);
            borrowTunnel.TerminateLifetimeTunnel = terminateLifetimeDfir;
            terminateLifetimeDfir.BeginLifetimeTunnel = borrowTunnel;
            return borrowTunnel;
        }

        protected Tunnel CreateInputTunnel(Structure structure)
        {
            return structure.CreateTunnel(Direction.Input, TunnelMode.LastValue, NITypes.Void, NITypes.Void);
        }

        protected Tunnel CreateOutputTunnel(Structure structure)
        {
            return structure.CreateTunnel(Direction.Output, TunnelMode.LastValue, NITypes.Void, NITypes.Void);
        }

        internal UnwrapOptionTunnel CreateUnwrapOptionTunnel(Frame frame)
        {
            return new UnwrapOptionTunnel(frame);
        }

        internal LoopConditionTunnel CreateLoopConditionTunnel(Loop loop)
        {
            var loopConditionTunnel = new LoopConditionTunnel(loop);
            var terminateLifetimeDfir = new TerminateLifetimeTunnel(loop);
            loopConditionTunnel.TerminateLifetimeTunnel = terminateLifetimeDfir;
            terminateLifetimeDfir.BeginLifetimeTunnel = loopConditionTunnel;
            return loopConditionTunnel;
        }

        internal IterateTunnel CreateIterateTunnel(Loop loop)
        {
            var iterateTunnel = new IterateTunnel(loop);
            var terminateLifetimeDfir = new TerminateLifetimeTunnel(loop);
            iterateTunnel.TerminateLifetimeTunnel = terminateLifetimeDfir;
            terminateLifetimeDfir.BeginLifetimeTunnel = iterateTunnel;
            return iterateTunnel;
        }

        internal OptionPatternStructure CreateOptionPatternStructure(Diagram parentDiagram)
        {
            OptionPatternStructure patternStructure = new OptionPatternStructure(parentDiagram);
            patternStructure.CreateDiagram();
            return patternStructure;
        }

        internal FunctionalNode ConnectSomeConstructorToInputTerminal(Terminal inputTerminal, bool mutableOutput = false)
        {
            var someConstructor = new FunctionalNode(inputTerminal.ParentDiagram, Signatures.SomeConstructorType);
            Wire.Create(inputTerminal.ParentDiagram, someConstructor.OutputTerminals[0], inputTerminal)
                .SetWireBeginsMutableVariable(mutableOutput);
            return someConstructor;
        }

        protected void AssertVariablesReferenceSame(VariableReference expected, VariableReference actual)
        {
            Assert.IsTrue(actual.ReferencesSame(expected));
        }

        protected void AssertTerminalHasRequiredTerminalUnconnectedMessage(Terminal terminal)
        {
            Assert.IsTrue(terminal.GetDfirMessages().Any(message => message.Descriptor == AllModelsOfComputationErrorMessages.RequiredTerminalUnconnected));
        }

        protected void AssertTerminalHasTypeConflictMessage(Terminal terminal)
        {
            Assert.IsTrue(terminal.GetDfirMessages().Any(message => message.Descriptor == AllModelsOfComputationErrorMessages.TypeConflict));
        }

        protected void AssertTerminalDoesNotHaveTypeConflictMessage(Terminal terminal)
        {
            Assert.IsFalse(terminal.GetDfirMessages().Any(message => message.Descriptor == AllModelsOfComputationErrorMessages.TypeConflict));
        }

        protected void AssertTerminalHasMissingTraitMessage(Terminal terminal)
        {
            Assert.IsTrue(terminal.GetDfirMessages().Any(message => message.Descriptor == Messages.TypeDoesNotHaveRequiredTraitDescriptor));
        }
    }
}
