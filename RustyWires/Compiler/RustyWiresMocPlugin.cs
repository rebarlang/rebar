using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Foundation;
using NationalInstruments.Compiler;
using NationalInstruments.Compiler.SemanticAnalysis;
using NationalInstruments.Composition;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Envoys;
using RustyWires.SourceModel;
using Diagram = NationalInstruments.SourceModel.Diagram;
using Structure = NationalInstruments.SourceModel.Structure;

namespace RustyWires.Compiler
{
    /// <summary>
    /// Factory / registration class for the <see cref="RustyWiresCompilerService"/> envoy service.
    /// </summary>
    [Preserve(AllMembers = true)]
    [ExportEnvoyServiceFactory(typeof(CompilerService))]
    [BindsToKeyword(RustyWiresFunction.RustyWiresMocIdentifier)]
    [PartMetadata(ExportIdentifier.ExportIdentifierKey, "{1253CAD1-5874-4BB6-8090-7C3841BB5E21}")]
    [BindOnTargeted]
    public class RustyWiresCompilerServiceInitialization : EnvoyServiceFactory
    {
        /// <summary>
        ///  Called to create the envoy service
        /// </summary>
        /// <returns>the created envoy service</returns>
        protected override EnvoyService CreateService()
        {
            EnvoyService service = Host.CreateInstance<RustyWiresCompilerService>();
            return service;
        }
    }

    public class RustyWiresCompilerService : CompilerService, IPartImportsSatisfiedNotification
    {
        private RustyWiresMocPlugin _mocPlugin;

        protected override MocPlugin MocPlugin => _mocPlugin;
         
        public void OnImportsSatisfied()
        {
            _mocPlugin = new RustyWiresMocPlugin(Host, this);
        }
    }

    public class RustyWiresMocPlugin : MocPlugin
    {
        private readonly ICompositionHost _host;

        public RustyWiresMocPlugin(ICompositionHost host, CompilerService compilerService) : base(host, compilerService)
        {
            _host = host;
        }

        public override MocTransformManager GenerateMocTransformManagerAndSourceDfir(DfirCreationArguments creationArguments)
        {
            var dfirBuilder = new RustyWiresDfirBuilder();
            var rustyWiresFunction = (RustyWiresFunction)creationArguments.SourceModel;
            dfirBuilder.VisitRustyWiresFunction(rustyWiresFunction);
            var dfirRoot = dfirBuilder.CreatedDfirRoot;
            RustyWiresMocReflector reflector = new RustyWiresMocReflector(
                rustyWiresFunction,
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
            List<IDfirTransformBase> semanticAnalysisTransforms = new List<IDfirTransformBase>()
            {
                new DetermineVariablesTransform(),
                new SetVariableTypesAndLifetimesTransform(),
                new ValidateVariableUsagesTransform(),
                new ReflectVariablesToTerminalsTransform(),
                // new ExplicitBorrowTransform(),
                // new PropagateTypePermissivenessTransform(),
                new StandardTypeReflectionTransform(),
            };
            ReflectErrorsTransform.AddErrorReflection(semanticAnalysisTransforms, CompilePhase.SemanticAnalysis);
            semanticAnalysisTransforms.Add(new EmptyTargetDfirTransform());

            return new StandardMocTransformManager(
                specAndQName,
                sourceDfir,
                semanticAnalysisTransforms,
                Enumerable.Empty<IDfirTransformBase>(), 
                _host.GetSharedExportedValue<ScheduledActivityManager>());
        }

        public override DfirRootRuntimeType GetRuntimeType(IReadOnlySymbolTable symbolTable) => DfirRootRuntimeType.FunctionType;

        private class EmptyTargetDfirTransform : IDfirTransform
        {
            public void Execute(DfirRoot dfirRoot, CompileCancellationToken cancellationToken)
            {
                // Just remove everything
                dfirRoot.BlockDiagram.DisconnectAndRemoveNodes(dfirRoot.BlockDiagram.Nodes);
            }
        }
    }

    internal class RustyWiresMocReflector : MocReflector, IReflectTypes
    {
        private readonly DfirModelMap _map;

        public RustyWiresMocReflector(
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

        public RustyWiresMocReflector(MocReflector reflectorToCopy) : base(reflectorToCopy)
        {
            _map = ((RustyWiresMocReflector)reflectorToCopy)._map;
        }

        protected override MocReflector CopyMocReflector()
        {
            return new RustyWiresMocReflector(this);
        }

        public void ReflectTypes(DfirRoot dfirGraph)
        {
            IReflectableModel source;
            if (TryGetSource(out source))
            {
                var rustyWiresFunction = (RustyWiresFunction)source;
                VisitDiagram(rustyWiresFunction.Diagram);
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
        }

        private void VisitConnectable(Connectable connectable)
        {
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
    }
}
