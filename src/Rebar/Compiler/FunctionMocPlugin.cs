using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Foundation;
using NationalInstruments;
using NationalInstruments.Compiler;
using NationalInstruments.Composition;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Envoys;
using Rebar.Common;
using Rebar.Compiler.Nodes;
using Rebar.SourceModel;
using Diagram = NationalInstruments.SourceModel.Diagram;
using Structure = NationalInstruments.SourceModel.Structure;

namespace Rebar.Compiler
{
    /// <summary>
    /// Factory / registration class for the <see cref="FunctionCompilerService"/> envoy service.
    /// </summary>
    [Preserve(AllMembers = true)]
    [ExportEnvoyServiceFactory(typeof(CompilerService))]
    [BindsToKeyword(Function.FunctionMocIdentifier)]
    [PartMetadata(ExportIdentifier.ExportIdentifierKey, "{1253CAD1-5874-4BB6-8090-7C3841BB5E21}")]
    [BindOnTargeted]
    public class FunctionCompilerServiceInitialization : EnvoyServiceFactory
    {
        /// <summary>
        ///  Called to create the envoy service
        /// </summary>
        /// <returns>the created envoy service</returns>
        protected override EnvoyService CreateService()
        {
            EnvoyService service = Host.CreateInstance<FunctionCompilerService>();
            return service;
        }
    }

    public class FunctionCompilerService : CompilerService, IPartImportsSatisfiedNotification
    {
        private FunctionMocPlugin _mocPlugin;

        protected override MocPlugin MocPlugin => _mocPlugin;
         
        public void OnImportsSatisfied()
        {
            _mocPlugin = new FunctionMocPlugin(Host, this);
        }
    }

    public class FunctionMocPlugin : MocPlugin
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
            dfirBuilder.VisitFunction(function);
            var dfirRoot = dfirBuilder.CreatedDfirRoot;
            FunctionMocReflector reflector = new FunctionMocReflector(
                function,
                creationArguments.ReflectionCancellationToken,
                _host.GetSharedExportedValue<ScheduledActivityManager>(),
                AdditionalErrorTexts,
                creationArguments.BuildSpecSource,
                creationArguments.SpecAndQName,
                dfirBuilder.DfirModelMap);
            creationArguments.PrebuildTransform(dfirRoot, reflector, this);

            ExecutionOrderSortingVisitor.SortDiagrams(dfirRoot);
            return GenerateMocTransformManager(creationArguments.SpecAndQName, dfirRoot, new CompileCancellationToken());
        }

        public override MocTransformManager GenerateMocTransformManager(SpecAndQName specAndQName, DfirRoot sourceDfir,
            CompileCancellationToken cancellationToken)
        {
            TerminalTypeUnificationResults unificationResults = new TerminalTypeUnificationResults();
            LifetimeVariableAssociation lifetimeVariableAssocation = new LifetimeVariableAssociation();
            List<IDfirTransformBase> semanticAnalysisTransforms = new List<IDfirTransformBase>()
            {
                new CreateNodeFacadesTransform(),
                new MergeVariablesAcrossWiresTransform(lifetimeVariableAssocation, unificationResults),
                new FinalizeAutoBorrowsTransform(),
                new MarkConsumedVariablesTransform(lifetimeVariableAssocation),
                new ValidateVariableUsagesTransform(unificationResults),
                new ReflectVariablesToTerminalsTransform(),
            };

            if (RebarFeatureToggles.IsRebarTargetEnabled)
            {
                semanticAnalysisTransforms.Add(new RebarSupportedTargetTransform());
            }
            semanticAnalysisTransforms.Add(new StandardTypeReflectionTransform());
            ReflectErrorsTransform.AddErrorReflection(semanticAnalysisTransforms, CompilePhase.SemanticAnalysis);
            if (!RebarFeatureToggles.IsRebarTargetEnabled)
            {
                semanticAnalysisTransforms.Add(new EmptyTargetDfirTransform());
            }

            List<IDfirTransformBase> toTargetDfirTransforms = new List<IDfirTransformBase>()
            {
                new AutoBorrowTransform(),
                new InsertTerminateLifetimeTransform(lifetimeVariableAssocation)
            };

            return new StandardMocTransformManager(
                specAndQName,
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

    internal class FunctionMocReflector : MocReflector, IReflectTypes
    {
        private readonly DfirModelMap _map;

        public FunctionMocReflector(
            IReflectableModel source, 
            ReflectionCancellationToken reflectionCancellationToken, 
            IScheduledActivityManager scheduledActivityManager, 
            IMessageDescriptorTranslator additionalErrorTexts,
            Envoy buildSpecSource, 
            SpecAndQName specAndQName,
            DfirModelMap map) : 
            base(source, reflectionCancellationToken, scheduledActivityManager, additionalErrorTexts, buildSpecSource, specAndQName)
        {
            _map = map;
        }

        public FunctionMocReflector(MocReflector reflectorToCopy) : base(reflectorToCopy)
        {
            _map = ((FunctionMocReflector)reflectorToCopy)._map;
        }

        protected override MocReflector CopyMocReflector()
        {
            return new FunctionMocReflector(this);
        }

        public void ReflectTypes(DfirRoot dfirGraph)
        {
            IReflectableModel source;
            if (TryGetSource(out source))
            {
                var function = (Function)source;
                VisitDiagram(function.Diagram);
            }
        }

        private void VisitDiagram(Diagram diagram)
        {
            foreach (var node in diagram.Nodes)
            {
                VisitConnectable(node);
                var structure = node as Structure;
                if (structure != null)
                {
                    foreach (var borderNode in structure.BorderNodes)
                    {
                        VisitConnectable(borderNode);
                    }
                    foreach (var nestedDiagram in structure.NestedDiagrams)
                    {
                        VisitDiagram(nestedDiagram);
                    }
                }
            }
            foreach (var wire in diagram.Wires)
            {
                VisitWire(wire);
            }
        }

        private void VisitConnectable(Connectable connectable)
        {
            // Update terminals on a TerminateLifetime before reflecting types
            var terminateLifetime = connectable as TerminateLifetime;
            if (terminateLifetime != null)
            {
                VisitTerminateLifetime(terminateLifetime);
            }

            foreach (var nodeTerminal in connectable.Terminals)
            {
                NationalInstruments.Dfir.Terminal dfirTerminal = _map.GetDfirForTerminal(nodeTerminal);
                NIType typeToReflect = dfirTerminal.DataType;
                if (!nodeTerminal.DataType.Equals(typeToReflect))
                {
                    nodeTerminal.DataType = typeToReflect;
                }
            }
        }

        private void VisitWire(NationalInstruments.SourceModel.Wire wire)
        {
            var dfirWire = (NationalInstruments.Dfir.Wire)_map.GetDfirForModel(wire);
            bool isFirstVariableWire = dfirWire.GetIsFirstVariableWire();
            wire.SetIsFirstVariableWire(isFirstVariableWire);

            NationalInstruments.Dfir.Terminal dfirSourceTerminal;
            if (dfirWire.TryGetSourceTerminal(out dfirSourceTerminal))
            {
                wire.SetWireVariable(dfirSourceTerminal.GetFacadeVariable());
            }

            if (!isFirstVariableWire)
            {
                wire.SetWireBeginsMutableVariable(false);
            }
        }

        private void VisitTerminateLifetime(TerminateLifetime terminateLifetime)
        {
            TerminateLifetimeNode terminateLifetimeDfir = (TerminateLifetimeNode)_map.GetDfirForModel(terminateLifetime);
            if (terminateLifetimeDfir.RequiredInputCount != null && terminateLifetimeDfir.RequiredOutputCount != null)
            {
                terminateLifetime.UpdateTerminals(terminateLifetimeDfir.RequiredInputCount.Value, terminateLifetimeDfir.RequiredOutputCount.Value);
            }
            foreach (var pair in terminateLifetime.Terminals.Zip(terminateLifetimeDfir.Terminals))
            {
                if (!_map.ContainsTerminal(pair.Key))
                {
                    _map.AddMapping(pair.Key, pair.Value);
                }
            }
        }
    }
}
