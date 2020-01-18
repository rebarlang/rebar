using System.ComponentModel.Composition;
using NationalInstruments;
using NationalInstruments.Composition;
using NationalInstruments.Core;
using NationalInstruments.FeatureToggles;
using NationalInstruments.MocCommon.SourceModel;
using NationalInstruments.SourceModel;

namespace Rebar.Design
{
    [ExportPaletteLoader(DiagramPaletteIdentifier)]
    [PartCreationPolicy(CreationPolicy.Shared)]
    [PartMetadata(ExportIdentifier.ExportIdentifierKey, ProductLevel.Elemental)]
    public class FunctionDiagramPaletteLoader : ResourcePaletteLoader
    {
        public const string DiagramPaletteIdentifier = "FunctionDiagram";

        /// <inheritdoc />
        protected override string ResourcePath => "Rebar.Resources.DiagramPalette.xml";
    }

    /// <summary>
    /// Defines the Rebar diagram palette type
    /// </summary>
    [ExportPaletteType(
        FunctionDiagramPaletteLoader.DiagramPaletteIdentifier,
        FunctionDiagramPaletteLoader.DiagramPaletteIdentifier + PaletteTypeInfo.PaletteTypeIdentifierSuffix,
        "Rebar diagram palette",
        userCreatable: false)]
    [BindsToKeyword(PaletteConstants.NativeTargetKeyword)]
    public class FunctionDocumentDiagramPaletteType : PaletteType
    {
    }

    [ExportPaletteLoader(FunctionDiagramPaletteLoader.DiagramPaletteIdentifier)]
    [PartCreationPolicy(CreationPolicy.Shared)]
    [PartMetadata(ExportIdentifier.ExportIdentifierKey, ProductLevel.Elemental)]
    public class OptionDiagramPaletteLoader : ResourcePaletteLoader
    {
        /// <inheritdoc />
        protected override string ResourcePath => "Rebar.Resources.OptionPalette.xml";
    }

    [ExportPaletteLoader(FunctionDiagramPaletteLoader.DiagramPaletteIdentifier)]
    [PartCreationPolicy(CreationPolicy.Shared)]
    [PartMetadata(ExportIdentifier.ExportIdentifierKey, ProductLevel.Elemental)]
    [PartMetadata(FeatureToggleSupport.RequiredFeatureToggleKey, RebarFeatureToggles.CellDataType)]
    public class CellDiagramPaletteLoader : ResourcePaletteLoader
    {
        /// <inheritdoc />
        protected override string ResourcePath => "Rebar.Resources.CellPalette.xml";
    }

    [ExportPaletteLoader(FunctionDiagramPaletteLoader.DiagramPaletteIdentifier)]
    [PartCreationPolicy(CreationPolicy.Shared)]
    [PartMetadata(ExportIdentifier.ExportIdentifierKey, ProductLevel.Elemental)]
    [PartMetadata(FeatureToggleSupport.RequiredFeatureToggleKey, RebarFeatureToggles.OutputNode)]
    public class OutputDiagramPaletteLoader : ResourcePaletteLoader
    {
        /// <inheritdoc />
        protected override string ResourcePath => "Rebar.Resources.OutputPalette.xml";
    }

    [ExportPaletteLoader(FunctionDiagramPaletteLoader.DiagramPaletteIdentifier)]
    [PartCreationPolicy(CreationPolicy.Shared)]
    [PartMetadata(ExportIdentifier.ExportIdentifierKey, ProductLevel.Elemental)]
    [PartMetadata(FeatureToggleSupport.RequiredFeatureToggleKey, RebarFeatureToggles.VectorAndSliceTypes)]
    public class VectorDiagramPaletteLoader : ResourcePaletteLoader
    {
        /// <inheritdoc />
        protected override string ResourcePath => "Rebar.Resources.VectorPalette.xml";
    }

    [ExportPaletteLoader(FunctionDiagramPaletteLoader.DiagramPaletteIdentifier)]
    [PartCreationPolicy(CreationPolicy.Shared)]
    [PartMetadata(ExportIdentifier.ExportIdentifierKey, ProductLevel.Elemental)]
    [PartMetadata(FeatureToggleSupport.RequiredFeatureToggleKey, RebarFeatureToggles.StringDataType)]
    public class StringDiagramPaletteLoader : ResourcePaletteLoader
    {
        /// <inheritdoc />
        protected override string ResourcePath => "Rebar.Resources.StringPalette.xml";
    }

    [ExportPaletteLoader(FunctionDiagramPaletteLoader.DiagramPaletteIdentifier)]
    [PartCreationPolicy(CreationPolicy.Shared)]
    [PartMetadata(ExportIdentifier.ExportIdentifierKey, ProductLevel.Elemental)]
    [PartMetadata(FeatureToggleSupport.RequiredFeatureToggleKey, RebarFeatureToggles.FileHandleDataType)]
    public class FileDiagramPaletteLoader : ResourcePaletteLoader
    {
        /// <inheritdoc />
        protected override string ResourcePath => "Rebar.Resources.FilePalette.xml";
    }

    [ExportPaletteLoader(FunctionDiagramPaletteLoader.DiagramPaletteIdentifier)]
    [PartCreationPolicy(CreationPolicy.Shared)]
    [PartMetadata(ExportIdentifier.ExportIdentifierKey, ProductLevel.Elemental)]
    [PartMetadata(FeatureToggleSupport.RequiredFeatureToggleKey, RebarFeatureToggles.OptionPatternStructure)]
    public class OptionPatternStructureDiagramPaletteLoader : ResourcePaletteLoader
    {
        /// <inheritdoc />
        protected override string ResourcePath => "Rebar.Resources.OptionPatternStructurePalette.xml";
    }

    [ExportPaletteLoader(FunctionDiagramPaletteLoader.DiagramPaletteIdentifier)]
    [PartCreationPolicy(CreationPolicy.Shared)]
    [PartMetadata(ExportIdentifier.ExportIdentifierKey, ProductLevel.Elemental)]
    [PartMetadata(FeatureToggleSupport.RequiredFeatureToggleKey, RebarFeatureToggles.NotifierType)]
    public class NotifierDiagramPaletteLoader : ResourcePaletteLoader
    {
        /// <inheritdoc />
        protected override string ResourcePath => "Rebar.Resources.NotifierPalette.xml";
    }
}
