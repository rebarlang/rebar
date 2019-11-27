using NationalInstruments.Compiler;
using NationalInstruments.Core;
using NationalInstruments.SourceModel.Envoys;

namespace Rebar.RebarTarget
{
    /// <summary>
    /// Envoy service factory for <see cref="TypeDiagramCompileHandlerFactory"/>. Binds to Rebar targets as an envoy service.
    /// </summary>
    [ExportEnvoyServiceFactory(typeof(ITargetCompileHandlerFactory))]
    [BindsToKeyword(TargetDefinition.TargetDefinitionString)]
    public class TypeDiagramCompileHandlerFactoryFactory : EnvoyServiceFactory
    {
        /// <inheritdoc />
        protected override EnvoyService CreateService()
        {
            return new TypeDiagramCompileHandlerFactory();
        }
    }
}
