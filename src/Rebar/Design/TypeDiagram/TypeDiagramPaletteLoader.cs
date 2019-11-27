using System.ComponentModel.Composition;
using NationalInstruments;
using NationalInstruments.Composition;
using NationalInstruments.Core;
using NationalInstruments.MocCommon.SourceModel;
using NationalInstruments.SourceModel;

namespace Rebar.Design.TypeDiagram
{
    [ExportPaletteLoader(DiagramPaletteIdentifier)]
    [PartCreationPolicy(CreationPolicy.Shared)]
    [PartMetadata(ExportIdentifier.ExportIdentifierKey, ProductLevel.Elemental)]
    public class TypeDiagramPaletteLoader : ResourcePaletteLoader
    {
        public const string DiagramPaletteIdentifier = "TypeDiagram";

        /// <inheritdoc />
        protected override string ResourcePath => "Rebar.Resources.TypeDiagramPalette.xml";
    }

    /// <summary>
    /// Defines the type diagram palette type
    /// </summary>
    [ExportPaletteType(
        TypeDiagramPaletteLoader.DiagramPaletteIdentifier,
        TypeDiagramPaletteLoader.DiagramPaletteIdentifier + PaletteTypeInfo.PaletteTypeIdentifierSuffix,
        "Type diagram palette",
        userCreatable: false)]
    [BindsToKeyword(PaletteConstants.NativeTargetKeyword)]
    public class TypeDiagramDocumentDiagramPaletteType : PaletteType
    {
    }
}
