using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;
using NationalInstruments.DataTypes;
using NationalInstruments.DynamicProperties;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;
using Rebar.Compiler;

namespace Rebar.SourceModel
{
    /// <summary>
    /// Growable node that builds a tuple from one or more inputs.
    /// </summary>
    public class BuildTuple : VerticalGrowNode, IViewVerticalGrowNode
    {
        private const int DefaultChunkCount = 2;

        public const string ElementName = "BuildTuple";

        public static readonly PropertySymbol VerticalChunkCountPropertySymbol = ExposeVerticalChunkCountProperty<BuildTuple>(DefaultChunkCount);

        public static readonly PropertySymbol NodeTerminalsPropertySymbol =
            ExposeReadOnlyVariableNodeTerminalsProperty<BuildTuple>(PropertySerializers.NodeTerminalsConnectedVariableReferenceSerializer);

        public BuildTuple()
        {
            Width = StockDiagramGeometries.GridSize * 8;
            TupleOutputTerminal = new NodeTerminal(
                Direction.Output,
                PFTypes.Void,
                "tuple",
                TerminalHotspots.CreateOutputTerminalHotspot(TerminalSize.Small, Width, 0));
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static BuildTuple CreateBuildTuple(IElementCreateInfo elementCreateInfo)
        {
            var buildTuple = new BuildTuple();
            buildTuple.Init(elementCreateInfo);
            return buildTuple;
        }

        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        public NodeTerminal TupleOutputTerminal { get; private set; }

        /// <inheritdoc />
        protected override void Init(IElementCreateInfo info)
        {
            base.Init(info);

            AddComponent(TupleOutputTerminal);

            // Set the initial chunk count
            this.SetVerticalChunkCount(DefaultChunkCount, GrowNodeResizeDirection.Bottom);
            this.RecalculateNodeHeight();
        }

        /// <inheritdoc />
        public override void AcceptVisitor(IElementVisitor visitor)
        {
            var functionVisitor = visitor as IFunctionVisitor;
            if (functionVisitor != null)
            {
                functionVisitor.VisitBuildTuple(this);
            }
            else
            {
                base.AcceptVisitor(visitor);
            }
        }

        /// <inheritdoc/>
        public override IDocumentation CreateDocumentationForTerminal(Terminal terminal)
        {
            if (terminal != TupleOutputTerminal)
            {
                string terminalName = ResourceBasedDocumentation.GetResourceBaseName(this) + "_element";
                var documentation = new Documentation(ResourceBasedDocumentation.RetrieveForTerminal(Host, this, terminalName));
                int index = terminal.Index - 1;
                documentation.Name += " " + index.ToString(CultureInfo.InvariantCulture);
                return documentation;
            }
            return base.CreateDocumentationForTerminal(terminal);
        }

        #region VerticalGrowNode overrides

        public override IList<WireableTerminal> CreateTerminalsForVerticalChunk(int chunkIndex)
        {
            return new List<WireableTerminal> { new NodeTerminal(Direction.Input, PFTypes.Void, "element", TerminalHotspots.Input1) };
        }

        public override int FixedTerminalCount => 1;

        public override int MinimumVerticalChunkCount => 1;

        public override int GetNumberOfTerminalsInVerticalChunk(int chunkIndex) => 1;

        #endregion

        #region IViewVerticalGrowNode implementation

        public float TopMargin => Template == ViewElementTemplate.List ? StockDiagramGeometries.ListViewHeaderHeight : 0;

        public float BottomMargin => Template == ViewElementTemplate.List ? ListViewFooterHeight : 0;

        public float TerminalHeight => Template == ViewElementTemplate.List ? StockDiagramGeometries.LargeTerminalHeight : StockDiagramGeometries.StandardTerminalHeight;

        public float TerminalHotspotVerticalOffset => TerminalHotspots.HotspotVerticalOffsetForTerminalSize(TerminalSize.Small);

        public float GetVerticalChunkHeight(int chunkIndex) => TerminalHeight;

        public float OffsetForVerticalChunk(int chunkIndex) => TopMargin + (1 + chunkIndex) * this.GetFixedSizeVerticalChunkHeight();

        public float NodeHeightForVerticalChunkCount(int chunkCount) => OffsetForVerticalChunk(chunkCount) + BottomMargin;

        #endregion
    }
}
