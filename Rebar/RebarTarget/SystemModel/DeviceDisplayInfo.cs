using NationalInstruments.SystemDiagram.SourceModel;

namespace Rebar.RebarTarget.SystemModel
{
    /// <summary>
    /// Icon and palette info for Rebar Device.
    /// </summary>
    [ExportDisplayInfo(
        typeof(DeviceDisplayInfo),
        typeof(DeviceCatalogItem),
        DisplayNameResourceKey = "RebarTargetDisplayName",
        PaletteIconPath = "pack://application:,,,/NationalInstruments.LabVIEW.VireoTarget;component/SystemDesigner/VireoBrowser/SystemDiagram/Palette/Images/Palette_WebServer_40x40.xml",
        PartsViewIconPath = "pack://application:,,,/NationalInstruments.LabVIEW.VireoTarget;component/SystemDesigner/VireoBrowser/SystemDiagram/Palette/Images/PartsView_WebServer_24x24.xml",
        IconViewIconPath = "pack://application:,,,/NationalInstruments.LabVIEW.VireoTarget;component/SystemDesigner/VireoBrowser/SystemDiagram/Palette/Images/IconView_WebServer_160x160.xml",
        Weight = 0.5)]
    public class DeviceDisplayInfo : DisplayInfoExport<PaletteCategory>
    {
    }
}
