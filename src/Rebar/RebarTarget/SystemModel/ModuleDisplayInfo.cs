using NationalInstruments.SystemDiagram.SourceModel;

namespace Rebar.RebarTarget.SystemModel
{
    /// <summary>
    /// Icon and palette info for Rebar Module.
    /// </summary>
    [ExportDisplayInfo(typeof(ModuleDisplayInfo), typeof(ModuleCatalogItem),
        DisplayNameResourceKey = "RebarModuleDisplayName",
        PaletteIconPath = "pack://application:,,,/NationalInstruments.LabVIEW.VireoTarget;component/SystemDesigner/VireoBrowser/SystemDiagram/Palette/Images/Palette_WebApplications_40x40.xml",
        PartsViewIconPath = "pack://application:,,,/NationalInstruments.LabVIEW.VireoTarget;component/SystemDesigner/VireoBrowser/SystemDiagram/Palette/Images/PartsView_WebApplications_40x40.xml",
        IconViewIconPath = "pack://application:,,,/NationalInstruments.LabVIEW.VireoTarget;component/SystemDesigner/VireoBrowser/SystemDiagram/Palette/Images/IconView_WebApplications_160x160.xml",
        Weight = 0.2)]
    public class ModuleDisplayInfo : DisplayInfoExport<NotInPaletteCategory>
    {
    }
}
