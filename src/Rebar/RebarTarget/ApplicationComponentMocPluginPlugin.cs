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
    [Export(typeof(ComponentMocPluginPlugin))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class ApplicationComponentMocPluginPlugin : ComponentMocPluginPlugin
    {
        public static readonly DfirRootRuntimeType ApplicationRuntimeType = new DfirRootRuntimeType(ApplicationComponentSubtype.Identifier);

        public override string ComponentSubtypeIdentifier => ApplicationComponentSubtype.Identifier;

        public override DfirRootRuntimeType RuntimeType => ApplicationRuntimeType;

        public override ComponentIRBuilder CreateIRBuilder()
        {
            return new ApplicationIRBuilder();
        }

        public override MocReflector CreateMocReflector(
            ICompilableModel source,
            ReflectionCancellationToken reflectionCancellationToken,
            Envoy buildSpecSource,
            CompileSpecification compileSpecification)
        {
            return new ApplicationComponentMocReflector(
                source,
                reflectionCancellationToken,
                ScheduledActivityManager,
                AdditionalErrorTexts,
                buildSpecSource,
                compileSpecification);
        }

        /// <inheritdoc />
        public override IEnumerable<IDfirTransformBase> GetSemanticTransforms()
        {
            yield return new ReflectErrorsTransform(CompilePhase.SemanticAnalysis);
        }
    }
}
