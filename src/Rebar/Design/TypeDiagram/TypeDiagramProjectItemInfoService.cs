using NationalInstruments.ProjectExplorer;
using NationalInstruments.SourceModel.Envoys;
using Rebar.SourceModel.TypeDiagram;

namespace Rebar.Design.TypeDiagram
{
    /// <summary>
    /// Envoy Service Factory
    /// </summary>
    [ExportEnvoyServiceFactory(typeof(IProjectItemInfo))]
    [ProvidedInterface(typeof(IProjectItemProvideContextMenuItems))]
    [BindsToModelDefinitionType(TypeDiagramDefinition.TypeDiagramDefinitionType)]
    public class TypeDiagramProjectItemInfoServiceFactory : EnvoyServiceFactory
    {
        /// <inheritdoc />
        protected override EnvoyService CreateService()
        {
            return new TypeDiagramProjectItemInfoService();
        }
    }

    /// <summary>
    /// Service responsible for the display information about the type diagram in the project.
    /// </summary>
    public class TypeDiagramProjectItemInfoService : ProjectItemInfoSourceFileReferenceDefaultService
    {
        // TODO: override Icon property (see implementation in VIProjectItemInfoService
    }
}
