using NationalInstruments.Dfir;
using Rebar.Compiler;

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

        protected void RunSemanticAnalysisUpToValidation(DfirRoot dfirRoot)
        {
            var cancellationToken = new NationalInstruments.Compiler.CompileCancellationToken();
            RunSemanticAnalysisUpToSetVariableTypes(dfirRoot, cancellationToken);
            new ValidateVariableUsagesTransform().Execute(dfirRoot, cancellationToken);
        }
    }
}
