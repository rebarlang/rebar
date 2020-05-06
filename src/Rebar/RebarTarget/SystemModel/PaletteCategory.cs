using System.ComponentModel.Composition;
using System.Xml.Linq;
using NationalInstruments.FeatureToggles;
using NationalInstruments.SystemDiagram.SourceModel;

namespace Rebar.RebarTarget.SystemModel
{
    /// <summary>
    /// Exports the Rebar palette category.
    /// </summary>
    [ExportPaletteCategory(typeof(PaletteCategory))]
    [PartMetadata(FeatureToggleSupport.RequiredFeatureToggleKey, RebarFeatureToggles.RebarTarget)]
    [WithPaletteDisplayInfoMetadata(
        typeof(PaletteCategory),
        "RebarPaletteCategory_RebarPaletteDisplayName",
        PaletteIconPath = "pack://application:,,,/NationalInstruments.VireoTarget;component/SystemDesigner/VireoBrowser/SystemDiagram/Palette/Images/PaletteCategory_Web_40x40.xml",
        Weight = 0.9)]
    public class PaletteCategory : IPaletteCategoryExport
    {
        /// <summary>
        /// Unique ID used for the Rebar palette category
        /// </summary>
        public static readonly XName Name = XName.Get("RebarPalette", SystemModelNamespaceSchema.ParsableNamespaceName);
    }
}
