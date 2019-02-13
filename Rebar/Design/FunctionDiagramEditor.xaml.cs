using System.Collections.ObjectModel;
using NationalInstruments.Core;
using NationalInstruments.Design;
using NationalInstruments.Shell;
using NationalInstruments.SourceModel;

namespace Rebar.Design
{
    public class FunctionDiagramEditorInfo : DocumentEditControlInfo<FunctionDiagramEditor>
    {
        public FunctionDiagramEditorInfo(string uniqueId, FunctionDocument document)
            : base(uniqueId, document, document.Function.Diagram, "editor", FunctionDiagramPaletteLoader.DiagramPaletteIdentifier, string.Empty, string.Empty)
        {
            // ClipboardDataFormat = SketchUtilities.SketchClipboardDataFormat;
        }
    }

    /// <summary>
    /// Interaction logic for FunctionDiagramEditor.xaml
    /// </summary>
    public partial class FunctionDiagramEditor : DocumentEditControl
    {
        /// <summary>
        /// UniqueId for document edit control.
        /// </summary>
        public const string UniqueId = "Rebar.FunctionDiagramEditor";

        public FunctionDiagramEditor()
        {
            InitializeComponent();
            DesignerSurfaceProperties.SetCanShowGridlines(_diagram, true);
            DesignerSurfaceProperties.SetCanSnapToGrid(_diagram, true);
            DesignerSurfaceProperties.SetSnapToGrid(_diagram, true);
            DesignerSurfaceProperties.SetShowGridlines(_diagram, false);
            DesignerSurfaceProperties.SetCanSnapToObjects(_diagram, true);
            DesignerSurfaceProperties.SetSnapToGridSize(this, new SMSize(StockDiagramGeometries.GridSize * 2, StockDiagramGeometries.GridSize * 2));
            DesignerSurfaceProperties.SetGridlineSpacing(this, new SMSize(2 * StockDiagramGeometries.GridSize, 2 * StockDiagramGeometries.GridSize));
        }

        public override DesignerEditControl Designer => _designer;

        /// <inheritdoc />
        protected override IDesignerToolViewModel CreateDefaultTool()
        {
            var tools = new Collection<IDesignerToolViewModel>
            {
                new PlacementViewModel(),
                new WiringToolViewModel(),
                new TextToolViewModel(),
                new SelectionToolViewModel(),
                // new ToolTipToolViewModel(),
                // new DebuggingToolViewModel()
            };
            return new AutoToolViewModel(tools);
        }
    }
}
