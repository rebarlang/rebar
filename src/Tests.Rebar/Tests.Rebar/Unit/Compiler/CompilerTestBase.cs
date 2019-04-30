using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Compiler;
using Rebar.RebarTarget;

namespace Tests.Rebar.Unit.Compiler
{
    public class CompilerTestBase
    {
        protected void RunSemanticAnalysisUpToCreateNodeFacades(DfirRoot dfirRoot, NationalInstruments.Compiler.CompileCancellationToken cancellationToken = null)
        {
            ExecutionOrderSortingVisitor.SortDiagrams(dfirRoot);
            cancellationToken = cancellationToken ?? new NationalInstruments.Compiler.CompileCancellationToken();
            new CreateNodeFacadesTransform().Execute(dfirRoot, cancellationToken);
        }

        protected void RunSemanticAnalysisUpToSetVariableTypes(DfirRoot dfirRoot, NationalInstruments.Compiler.CompileCancellationToken cancellationToken = null)
        {
            cancellationToken = cancellationToken ?? new NationalInstruments.Compiler.CompileCancellationToken();
            RunSemanticAnalysisUpToCreateNodeFacades(dfirRoot, cancellationToken);
            new SetVariableTypesAndLifetimesTransform().Execute(dfirRoot, cancellationToken);
        }

        protected void RunSemanticAnalysisUpToValidation(DfirRoot dfirRoot, NationalInstruments.Compiler.CompileCancellationToken cancellationToken = null)
        {
            cancellationToken = cancellationToken ?? new NationalInstruments.Compiler.CompileCancellationToken();
            RunSemanticAnalysisUpToSetVariableTypes(dfirRoot, cancellationToken);
            new ValidateVariableUsagesTransform().Execute(dfirRoot, cancellationToken);
        }

        protected void RunSemanticAnalysisUpToCodeGeneration(DfirRoot dfirRoot)
        {
            var cancellationToken = new NationalInstruments.Compiler.CompileCancellationToken();
            RunSemanticAnalysisUpToValidation(dfirRoot, cancellationToken);

            new AutoBorrowTransform().Execute(dfirRoot, cancellationToken);
            FunctionCompileHandler.CompileFunction(dfirRoot, cancellationToken);
        }

        protected void ConnectConstantToInputTerminal(Terminal inputTerminal, NIType variableType, bool mutable)
        {
            Constant constant = Constant.Create(inputTerminal.ParentDiagram, variableType.CreateDefaultValue(), variableType);
            Wire wire = Wire.Create(inputTerminal.ParentDiagram, constant.OutputTerminal, inputTerminal);
            wire.SetWireBeginsMutableVariable(mutable);
        }
    }
}
