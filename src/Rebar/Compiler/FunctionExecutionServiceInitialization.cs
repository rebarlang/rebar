using Foundation;
using NationalInstruments.Compiler;
using NationalInstruments.SourceModel.Envoys;

namespace Rebar.Compiler
{
    /// <summary>
    /// Factory / registration class for the <see cref="FunctionExecutionService"/> envoy service
    /// </summary>
    [Preserve(AllMembers = true)]
    [ExportEnvoyServiceFactory(typeof(IFunctionExecutionService))]
    [ProvidedInterface(typeof(ILockDocument))]
    [ProvidedInterface(typeof(IExecutionService))]
    [BindsToModelDefinitionType(SourceModel.Function.FunctionDefinitionType)]
    [BindOnTargeted]
    public sealed class DevSystemFunctionExecutionServiceInitialization : EnvoyServiceFactory
    {
        /// <inheritdoc />
        protected override EnvoyService CreateService()
        {
            return new FunctionExecutionService();
        }
    }
}
