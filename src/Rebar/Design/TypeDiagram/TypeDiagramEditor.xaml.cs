using System.Collections.ObjectModel;
using NationalInstruments.Core;
using NationalInstruments.Design;
using NationalInstruments.Shell;
using NationalInstruments.SourceModel;

namespace Rebar.Design.TypeDiagram
{
    /// <summary>
    /// Interaction logic for TypeDiagramEditor.xaml
    /// </summary>
    public partial class TypeDiagramEditor : DocumentEditControl
    {
        /// <summary>
        /// UniqueId for document edit control.
        /// </summary>
        public const string UniqueId = "Rebar.TypeDiagramEditor";

        public TypeDiagramEditor()
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

        /// <inheritdoc />
        protected override void Initialize()
        {
            base.Initialize();
            // ((TypeDiagramDocument)Document).AddCommandBindings(this);
        }

        /// <inheritdoc />
        public override void CreateCommandContentForDocument(ICommandPresentationContext context, Document document)
        {
            base.CreateCommandContentForDocument(context, document);
            using (context.AddDocumentToolBarContent())
            {
            }
        }
    }
}
