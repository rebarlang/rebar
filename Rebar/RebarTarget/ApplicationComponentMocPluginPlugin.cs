using System.Collections.Generic;
using System.ComponentModel.Composition;
using NationalInstruments.Compiler;
using NationalInstruments.Dfir;
using NationalInstruments.MocCommon.Components.Compiler;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Envoys;
using Rebar.Compiler;

namespace Rebar.RebarTarget
{
    [Export(typeof(IComponentMocPluginPlugin))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class ApplicationComponentMocPluginPlugin : ComponentMocPluginPluginBase
    {
        public static readonly DfirRootRuntimeType ApplicationRuntimeType = new DfirRootRuntimeType(ApplicationComponentSubtype.Identifier);

        public override string ComponentSubtypeIdentifier => ApplicationComponentSubtype.Identifier;

        public override DfirRootRuntimeType RuntimeType => ApplicationRuntimeType;

        public override IComponentIRBuilder CreateIRBuilder()
        {
            return new ApplicationIRBuilder();
        }

        public override MocReflector CreateMocReflector(ICompilableModel source, ReflectionCancellationToken reflectionCancellationToken, Envoy buildSpecSource, SpecAndQName specAndQName)
        {
            return new ApplicationComponentMocReflector(
                source,
                reflectionCancellationToken,
                ScheduledActivityManager,
                AdditionalErrorTexts,
                buildSpecSource,
                specAndQName);
        }

        public override IEnumerable<IDfirTransformBase> GetSemanticTransforms(BuildSpec buildSpec)
        {
            yield return new RebarSupportedTargetTransform();
            yield return new ReflectErrorsTransform(CompilePhase.SemanticAnalysis);
        }
    }
}
