using System.Collections.Generic;
using System.Xml.Linq;
using NationalInstruments.CommonModel;
using NationalInstruments.DataTypes;
using NationalInstruments.DynamicProperties;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;
using Rebar.Common;

namespace Rebar.SourceModel.TypeDiagram
{
    /// <summary>
    /// Node on the <see cref="TypeDiagramDefinition"/> that represents the type being defined.
    /// </summary>
    public class SelfType : VerticalGrowNode, IViewVerticalGrowNode
    {
        private const string ElementName = "SelfType";

        private const int DefaultChunkCount = 1;

        public static readonly PropertySymbol VerticalChunkCountPropertySymbol = ExposeVerticalChunkCountProperty<SelfType>(DefaultChunkCount);

        public static readonly PropertySymbol NodeTerminalsPropertySymbol =
            ExposeReadOnlyVariableNodeTerminalsProperty<SelfType>(PropertySerializers.NodeTerminalsConnectedVariableReferenceSerializer);

        public static readonly PropertySymbol ModePropertySymbol =
            ExposeStaticProperty<SelfType>(
                nameof(Mode),
                selfType => selfType.Mode,
                (selfType, value) => selfType.Mode = (SelfTypeMode)value,
                PropertySerializers.CreateEnumSerializer<SelfTypeMode>(),
                SelfTypeMode.Struct
            );

        private SelfType()
        {
            Width = StockDiagramGeometries.GridSize * 8;
            Mode = SelfTypeMode.Struct;
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static SelfType CreateSelfType(IElementCreateInfo elementCreateInfo)
        {
            var selfType = new SelfType();
            selfType.Initialize(elementCreateInfo);
            return selfType;
        }

        public SelfTypeMode Mode { get; private set; }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        /// <inheritdoc />
        protected override void Initialize(IElementCreateInfo info)
        {
            base.Initialize(info);

            // Set the initial chunk count
            this.SetVerticalChunkCount(DefaultChunkCount, GrowNodeResizeDirection.Bottom);
            this.RecalculateNodeHeight();
        }

        /// <inheritdoc />
        public override bool CanDelete => false;

        #region VerticalGrowNode overrides

        /// <inheritdoc />
        public override IList<WireableTerminal> CreateTerminalsForVerticalChunk(int chunkIndex)
        {
            return new List<WireableTerminal> { new NodeTerminal(Direction.Input, NITypes.Void, "element", TerminalHotspots.Input1) };
        }

        /// <inheritdoc />
        public override int FixedTerminalCount => 0;

        /// <inheritdoc />
        public override int MinimumVerticalChunkCount => 1;

        /// <inheritdoc />
        public override int GetNumberOfTerminalsInVerticalChunk(int chunkIndex) => 1;

        #endregion

        #region IViewVerticalGrowNode implementation

        /// <inheritdoc />
        public override float TopMargin => Template == ViewElementTemplate.List ? StockDiagramGeometries.ListViewHeaderHeight : 0;

        /// <inheritdoc />
        public override float BottomMargin => Template == ViewElementTemplate.List ? ListViewFooterHeight : 0;

        /// <inheritdoc />
        public override float TerminalHeight => Template == ViewElementTemplate.List ? StockDiagramGeometries.LargeTerminalHeight : StockDiagramGeometries.StandardTerminalHeight;

        /// <inheritdoc />
        public override float TerminalHotspotVerticalOffset => TerminalHotspots.HotspotVerticalOffsetForTerminalSize(TerminalSize.Small);

        /// <inheritdoc />
        public override float GetVerticalChunkHeight(int chunkIndex) => TerminalHeight;

        /// <inheritdoc />
        public override float OffsetForVerticalChunk(int chunkIndex) => TopMargin + chunkIndex * this.GetFixedSizeVerticalChunkHeight();

        /// <inheritdoc />
        public override float NodeHeightForVerticalChunkCount(int chunkCount) => OffsetForVerticalChunk(chunkCount) + BottomMargin;

        #endregion
    }
}
