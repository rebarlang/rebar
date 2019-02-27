using NationalInstruments.Compiler;
using NationalInstruments.Core;
using NationalInstruments.MocCommon.Components.Compiler;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Envoys;

namespace Rebar.RebarTarget
{
    /// <summary>
    /// A class to reflect messages from the compile process of a Rebar Application component into the source model for the component.
    /// Each MoC has its own way to display compiler information. All reflectors reflect errors from the compile process.
    /// 
    /// In particular, this MocReflector reflects errors from the component's <see cref="DfirRoot"/> to the <see cref="SparseEnvoyManager"/>'s referencing <see cref="Envoy"/>.
    /// </summary>
    internal class ApplicationComponentMocReflector : ComponentMocReflector
    {
        /// <summary>
        /// Base constructor of <see cref="ApplicationComponentMocReflector"/>
        /// </summary>
        /// <param name="source">An interface allowing the reflector to talk to the source model</param>
        /// <param name="reflectionCancellationToken">A token to poll for whether to do reflection or not</param>
        /// <param name="scheduledActivityManager">The activity used to flip over into the UI thread as needed to do reflections</param>
        /// <param name="additionalErrorTexts">Supplies third-party message descriptions for this MoC</param>
        /// <param name="buildSpecSource">Source of the BuildSpec for which to reflect messages</param>
        /// <param name="specAndQName">The <see cref="SpecAndQName"/> for which to reflect messages</param>
        internal ApplicationComponentMocReflector(IReflectableModel source, ReflectionCancellationToken reflectionCancellationToken, IScheduledActivityManager scheduledActivityManager, IMessageDescriptorTranslator additionalErrorTexts, Envoy buildSpecSource, SpecAndQName specAndQName)
            : base(source, reflectionCancellationToken, scheduledActivityManager, additionalErrorTexts, buildSpecSource, specAndQName)
        {
        }

        /// <inheritdoc/>
        protected ApplicationComponentMocReflector(ApplicationComponentMocReflector reflector)
            : base(reflector)
        {
        }

        /// <inheritdoc/>
        protected override MocReflector CopyMocReflector()
        {
            return new ApplicationComponentMocReflector(this);
        }
    }
}
