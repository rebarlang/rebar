using NationalInstruments.Shell;
using NationalInstruments.SourceModel.Envoys;
using Rebar.SourceModel.TypeDiagram;

namespace Rebar.Design.TypeDiagram
{
    /// <summary>
    /// Type diagram document type
    /// </summary>
    [ExportDocumentAndFileType(
        nameType: typeof(TypeDiagramDocumentType),
        modelDefinitionType: TypeDiagramDefinition.TypeDiagramDefinitionType,
        smallImage: "Resources/SketchSmallImgae.xml",
        createNewSmallImage: CreateNewSmallIcon,
        createNewLargeImage: CreateNewLargeIcon,
        paletteImage: PaletteImage,
        relativeImportance: RelativeImportance,
        fileExtension: ".td",
        autoCreatesProject: true,
        commandLineArguments: OpenArgumentFormat + ", " + "Foo",
        defaultFileNameType: typeof(TypeDiagramDocumentType))]
    public class TypeDiagramDocumentType : SourceFileDocumentType
    {
        /// <summary>
        /// Our command line
        /// </summary>
        public const string OpenArgument = "openTypeDiagram";

        /// <summary>
        /// Our command line
        /// </summary>
        public const string OpenArgumentFormat = OpenArgument + " {0}";

        /// <summary>
        /// Constant for create new large icon.
        /// </summary>
        public const string CreateNewLargeIcon = "SketchCreateNewLargeIcon.png";

        /// <summary>
        /// Constant for create new small icon.
        /// </summary>
        public const string CreateNewSmallIcon = "SketchCreateNewSmallIcon.png";

        /// <summary>
        /// Constant for palette icon.
        /// </summary>
        public const string PaletteImage = "SketchPaletteImage.png";

        /// <summary>
        /// Constant for relative importance.
        /// </summary>
        public const double RelativeImportance = 0.5;

        /// <inheritdoc />
        public override Document CreateDocument(Envoy envoy)
        {
            return Host.CreateInstance<TypeDiagramDocument>();
        }
    }
}
