using System.ComponentModel.Composition;
using NationalInstruments.Compiler;
using NationalInstruments.Core;
using NationalInstruments.FeatureToggles;
using NationalInstruments.SourceModel.Envoys;

namespace Rebar.RebarTarget
{
    /// <summary>
    /// A factory for creating <see cref="ApplicationCompileHandler"/>s. Binds to the Rebar target as an envoy service.
    /// </summary>
    [ExportEnvoyServiceFactory(typeof(ITargetCompileHandlerFactory))]
    [PartMetadata(FeatureToggleSupport.RequiredFeatureToggleKey, RebarFeatureToggles.RebarTarget)]
    [BindsToKeyword(TargetDefinition.TargetDefinitionString)]
    public class ApplicationCompileHandlerFactoryFactory : EnvoyServiceFactory
    {
        /// <inheritdoc />
        protected override EnvoyService CreateService()
        {
            return new ApplicationCompileHandlerFactory();
        }
    }
}
