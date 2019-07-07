using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.Compiler;
using NationalInstruments.Compiler.SemanticAnalysis;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler;
using Rebar.Compiler.Nodes;
using Rebar.RebarTarget;

namespace Tests.Rebar.Unit.Compiler
{
    public class CompilerTestBase
    {
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
            new AutoBorrowTransform().Execute(dfirRoot, cancellationToken);
            new InsertTerminateLifetimeTransform(lifetimeVariableAssociation).Execute(dfirRoot, cancellationToken);
            new InsertDropTransform(lifetimeVariableAssociation).Execute(dfirRoot, cancellationToken);
        }

        internal global::Rebar.RebarTarget.BytecodeInterpreter.Function RunSemanticAnalysisUpToCodeGeneration(DfirRoot dfirRoot)
        {
            var cancellationToken = new CompileCancellationToken();
            RunCompilationUpToAutomaticNodeInsertion(dfirRoot, cancellationToken);
            return FunctionCompileHandler.CompileFunctionForBytecodeInterpreter(dfirRoot, cancellationToken);
        }

        internal LLVMSharp.Module RunSemanticAnalysisUpToLLVMCodeGeneration(DfirRoot dfirRoot, string compiledFunctionName)
        {
            var cancellationToken = new CompileCancellationToken();
            RunCompilationUpToAutomaticNodeInsertion(dfirRoot, cancellationToken);
            return FunctionCompileHandler.CompileFunctionForLLVM(dfirRoot, cancellationToken, compiledFunctionName);
        }

        protected NIType DefineGenericOutputFunctionSignature()
        {
            NIFunctionBuilder functionBuilder = PFTypes.Factory.DefineFunction("genericOutput");
            NIType typeParameter = Signatures.AddGenericDataTypeParameter(functionBuilder, "TData");
            Signatures.AddOutputParameter(functionBuilder, typeParameter, "out");
            return functionBuilder.CreateType();
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
            return structure.CreateTunnel(Direction.Input, TunnelMode.LastValue, PFTypes.Void, PFTypes.Void);
        }

        protected Tunnel CreateOutputTunnel(Structure structure)
        {
            return structure.CreateTunnel(Direction.Output, TunnelMode.LastValue, PFTypes.Void, PFTypes.Void);
        }

        internal LoopConditionTunnel CreateLoopConditionTunnel(global::Rebar.Compiler.Nodes.Loop loop)
        {
            var loopConditionTunnel = new LoopConditionTunnel(loop);
            var terminateLifetimeDfir = new TerminateLifetimeTunnel(loop);
            loopConditionTunnel.TerminateLifetimeTunnel = terminateLifetimeDfir;
            terminateLifetimeDfir.BeginLifetimeTunnel = loopConditionTunnel;
            return loopConditionTunnel;
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
            Assert.IsTrue(terminal.GetDfirMessages().Any(message => message.Descriptor == Messages.TypeDoesNotHaveRequiredTrait.Descriptor));
        }
    }
}
