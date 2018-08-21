using NationalInstruments.ProjectExplorer;
using NationalInstruments.SourceModel.Envoys;
using RustyWires.SourceModel;

namespace RustyWires.Design
{
    /// <summary>
    /// Envoy Service Factory
    /// </summary>
    [ExportEnvoyServiceFactory(typeof(IProjectItemInfo))]
    [ProvidedInterface(typeof(IProjectItemProvideContextMenuItems))]
    [BindsToModelDefinitionType(RustyWiresFunction.RustyWiresFunctionDefinitionType)]
    public class RustyWiresProjectItemInfoServiceFactory : EnvoyServiceFactory
    {
        /// <inheritdoc />
        protected override EnvoyService CreateService()
        {
            return new RustyWiresProjectItemInfoService();
        }
    }

    /// <summary>
    /// Service responsible for the display information about VI in the project.
    /// </summary>
    public class RustyWiresProjectItemInfoService : ProjectItemInfoSourceFileReferenceDefaultService
    {
        // TODO: override Icon property (see implementation in VIProjectItemInfoService
    }
}
