using NationalInstruments.ProjectExplorer;
using NationalInstruments.SourceModel.Envoys;
using Rebar.SourceModel;

namespace Rebar.Design
{
    /// <summary>
    /// Envoy Service Factory
    /// </summary>
    [ExportEnvoyServiceFactory(typeof(IProjectItemInfo))]
    [ProvidedInterface(typeof(IProjectItemProvideContextMenuItems))]
    [BindsToModelDefinitionType(Function.FunctionDefinitionType)]
    public class FunctionProjectItemInfoServiceFactory : EnvoyServiceFactory
    {
        /// <inheritdoc />
        protected override EnvoyService CreateService()
        {
            return new FunctionProjectItemInfoService();
        }
    }

    /// <summary>
    /// Service responsible for the display information about VI in the project.
    /// </summary>
    public class FunctionProjectItemInfoService : ProjectItemInfoSourceFileReferenceDefaultService
    {
        // TODO: override Icon property (see implementation in VIProjectItemInfoService
    }
}
