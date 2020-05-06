using System;
using System.Collections.Generic;
using NationalInstruments.Compiler;
using NationalInstruments.Composition;
using NationalInstruments.Core;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.SourceModel;

namespace Rebar.Compiler
{
    internal class FunctionMocPlugin : MocPlugin
    {
        private readonly ICompositionHost _host;

        public FunctionMocPlugin(ICompositionHost host, CompilerService compilerService) : base(host, compilerService)
        {
            _host = host;
        }

        public override MocTransformManager GenerateMocTransformManagerAndSourceDfir(DfirCreationArguments creationArguments)
        {
            var dfirBuilder = new FunctionDfirBuilder();
            var function = (Function)creationArguments.SourceModel;
            var dfirRoot = dfirBuilder.CreatedDfirRoot;
            FunctionMocReflector reflector = new FunctionMocReflector(
                function,
                creationArguments.ReflectionCancellationToken,
                _host.GetSharedExportedValue<ScheduledActivityManager>(),
                AdditionalErrorTexts,
                creationArguments.BuildSpecSource,
                creationArguments.CompileSpecification,
                dfirBuilder.DfirModelMap);
            creationArguments.PrebuildTransform(dfirRoot, reflector, this);

            dfirBuilder.VisitFunction(function);
            ExecutionOrderSortingVisitor.SortDiagrams(dfirRoot);
            return GenerateMocTransformManager(
                creationArguments.CompileSpecification,
                dfirRoot,
                new CompileCancellationToken());
        }

        public override MocTransformManager GenerateMocTransformManager(
            CompileSpecification compileSpecification,
            DfirRoot sourceDfir,
            CompileCancellationToken cancellationToken)
        {
            TerminalTypeUnificationResults unificationResults = new TerminalTypeUnificationResults();
            LifetimeVariableAssociation lifetimeVariableAssociation = new LifetimeVariableAssociation();
            List<IDfirTransformBase> semanticAnalysisTransforms = new List<IDfirTransformBase>()
            {
                new CreateNodeFacadesTransform(),
                new MergeVariablesAcrossWiresTransform(lifetimeVariableAssociation, unificationResults),
                new FinalizeAutoBorrowsTransform(),
                new MarkConsumedVariablesTransform(lifetimeVariableAssociation),
                new ValidateVariableUsagesTransform(unificationResults),
                new ReflectVariablesToTerminalsTransform(),
            };

            if (RebarFeatureToggles.IsRebarTargetEnabled)
            {
                semanticAnalysisTransforms.Add(new RebarSupportedTargetTransform(SemanticAnalysisTargetInfo));
            }
            semanticAnalysisTransforms.Add(new StandardTypeReflectionTransform());
            ReflectErrorsTransform.AddErrorReflection(semanticAnalysisTransforms, CompilePhase.SemanticAnalysis);
            if (!RebarFeatureToggles.IsRebarTargetEnabled)
            {
                semanticAnalysisTransforms.Add(new EmptyTargetDfirTransform());
            }

            var nodeInsertionTypeUnificationResultFactory = new NodeInsertionTypeUnificationResultFactory();
            List<IDfirTransformBase> toTargetDfirTransforms = new List<IDfirTransformBase>()
            {
                new AutoBorrowTransform(lifetimeVariableAssociation),
                new InsertTerminateLifetimeTransform(lifetimeVariableAssociation, nodeInsertionTypeUnificationResultFactory),
                new InsertDropTransform(lifetimeVariableAssociation, nodeInsertionTypeUnificationResultFactory),
            };

            return new StandardMocTransformManager(
                compileSpecification,
                sourceDfir,
                semanticAnalysisTransforms,
                toTargetDfirTransforms, 
                _host.GetSharedExportedValue<ScheduledActivityManager>());
        }

        public override DfirRootRuntimeType GetRuntimeType(IReadOnlySymbolTable symbolTable) => FunctionRuntimeType;

        private class EmptyTargetDfirTransform : IDfirTransform
        {
            public void Execute(DfirRoot dfirRoot, CompileCancellationToken cancellationToken)
            {
                // Just remove everything
                dfirRoot.BlockDiagram.DisconnectAndRemoveNodes(dfirRoot.BlockDiagram.Nodes);
            }
        }

        public static DfirRootRuntimeType FunctionRuntimeType { get; } = new DfirRootRuntimeType("RebarFunction");
    }
}
