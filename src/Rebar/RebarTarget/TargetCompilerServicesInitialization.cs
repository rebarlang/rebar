using NationalInstruments.Compiler;
using NationalInstruments.Core;
using NationalInstruments.SourceModel.Envoys;

namespace Rebar.RebarTarget
{
    /// <summary>
    /// Rebar target compiler service factory. A factory is used so the service is not reused and we can
    /// dispose it on Detach.
    /// </summary>
    [ExportEnvoyServiceFactory(typeof(ITargetCompilerServices))]
    [BindsToKeyword(TargetDefinition.TargetDefinitionString)]
    [TargetModel(TargetDefinition.TargetDefinitionString, typeof(TargetDefinition), typeof(ITargetCompilerServices))]
    public class TargetCompilerServicesInitialization : EnvoyServiceFactory
    {
        /// <summary>
        /// Creates the envoy service associated with simulation
        /// </summary>
        /// <returns>Envoy service</returns>
        protected override EnvoyService CreateService()
        {
            return Host.CreateInstance<TargetCompilerServices>();
        }
    }
}
