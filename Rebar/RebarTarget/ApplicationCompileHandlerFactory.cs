using NationalInstruments.Compiler;
using NationalInstruments.Core;
using NationalInstruments.MocCommon.Components.Compiler;
using NationalInstruments.SourceModel.Envoys;

namespace Rebar.RebarTarget
{
    /// <summary>
    /// A factory for creating <see cref="ApplicationCompileHandler"/>s. Binds to the Rebar target as an envoy service.
    /// </summary>
    internal sealed class ApplicationCompileHandlerFactory : EnvoyService, ITargetCompileHandlerFactory
    {
        /// <inheritdoc />
        public TargetCompileHandler Create(DelegatingTargetCompiler parent, IScheduledActivityManager scheduledActivityManager)
        {
            return new ApplicationCompileHandler(
                parent,
                scheduledActivityManager,
                Host,
                new OwningComponentInformationRetriever(AssociatedEnvoy.ParentScope));
        }
    }
}
