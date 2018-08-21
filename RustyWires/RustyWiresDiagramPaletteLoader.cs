using System.ComponentModel.Composition;
using NationalInstruments;
using NationalInstruments.Composition;
using NationalInstruments.Core;
using NationalInstruments.MocCommon.SourceModel;
using NationalInstruments.SourceModel;

namespace RustyWires
{
    [ExportPaletteLoader(RustyWiresDiagramPaletteIdentifier)]
    [PartCreationPolicy(CreationPolicy.Shared)]
    [PartMetadata(ExportIdentifier.ExportIdentifierKey, ProductLevel.Elemental)]
    public class RustyWiresDiagramPaletteLoader : ResourcePaletteLoader
    {
        public const string RustyWiresDiagramPaletteIdentifier = "RustyWiresDiagram";

        /// <inheritdoc />
        protected override string ResourcePath => "RustyWires.Resources.DiagramPalette.xml";
    }

    /// <summary>
    /// Defines the sketch diagram palette type
    /// </summary>
    [ExportPaletteType(
        RustyWiresDiagramPaletteLoader.RustyWiresDiagramPaletteIdentifier,
        RustyWiresDiagramPaletteLoader.RustyWiresDiagramPaletteIdentifier + PaletteTypeInfo.PaletteTypeIdentifierSuffix,
        "RustyWires diagram palette",
        userCreatable: false)]
    [BindsToKeyword(PaletteConstants.NativeTargetKeyword)]
    public class SketchDocumentDiagramPaletteType : PaletteType
    {
    }
}
