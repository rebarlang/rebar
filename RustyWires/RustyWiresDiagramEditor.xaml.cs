using System.Collections.ObjectModel;
using NationalInstruments.Core;
using NationalInstruments.Design;
using NationalInstruments.MocCommon.Design;
using NationalInstruments.Shell;
using NationalInstruments.SourceModel;

namespace RustyWires
{
    public class RustyWiresDiagramEditorInfo : DocumentEditControlInfo<RustyWiresDiagramEditor>
    {
        public RustyWiresDiagramEditorInfo(string uniqueId, RustyWiresDocument document)
            : base(uniqueId, document, document.Function.Diagram, "editor", RustyWiresDiagramPaletteLoader.RustyWiresDiagramPaletteIdentifier, string.Empty, string.Empty)
        {
            // ClipboardDataFormat = SketchUtilities.SketchClipboardDataFormat;
        }
    }

    /// <summary>
    /// Interaction logic for RustyWiresDiagramEditor.xaml
    /// </summary>
    public partial class RustyWiresDiagramEditor : DocumentEditControl
    {
        /// <summary>
        /// UniqueId for document edit control.
        /// </summary>
        public const string UniqueId = "RustyWires.RustyWiresDiagramEditor";

        public RustyWiresDiagramEditor()
        {
            InitializeComponent();
            DesignerSurfaceProperties.SetCanShowGridlines(_diagram, true);
            DesignerSurfaceProperties.SetCanSnapToGrid(_diagram, true);
            DesignerSurfaceProperties.SetSnapToGrid(_diagram, true);
            DesignerSurfaceProperties.SetShowGridlines(_diagram, true);
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
