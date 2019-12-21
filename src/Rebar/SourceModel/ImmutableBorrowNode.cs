using System.Linq;
using System.Xml.Linq;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.DynamicProperties;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;
using Rebar.Common;
using Rebar.Compiler;

namespace Rebar.SourceModel
{
    public class ImmutableBorrowNode : SimpleNode, IBorrowTunnel
    {
        private const string ElementName = "ImmutableBorrowNode";

        public static readonly PropertySymbol BorrowModePropertySymbol =
            ExposeStaticProperty<ImmutableBorrowNode>(
                "BorrowMode",
                borrowNode => borrowNode.BorrowMode,
                (borrowTunnel, value) => borrowTunnel.BorrowMode = (BorrowMode)value,
                PropertySerializers.CreateEnumSerializer<BorrowMode>(),
                BorrowMode.Immutable);

        private BorrowMode _borrowMode = BorrowMode.Immutable;

        protected ImmutableBorrowNode()
        {
            var immutableReferenceType = PFTypes.Void.CreateImmutableReference();
            FixedTerminals.Add(new NodeTerminal(Direction.Input, immutableReferenceType, "value in"));
            FixedTerminals.Add(new NodeTerminal(Direction.Output, immutableReferenceType, "reference out"));
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static ImmutableBorrowNode CreateImmutablePassthroughNode(IElementCreateInfo elementCreateInfo)
        {
            var immutableBorrowNode = new ImmutableBorrowNode();
            immutableBorrowNode.Init(elementCreateInfo);
            return immutableBorrowNode;
        }

        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        public BorrowMode BorrowMode
        {
            get { return _borrowMode; }
            set
            {
                if (_borrowMode != value)
                {
                    TransactionRecruiter.EnlistPropertyItem(
                        this,
                        "BorrowMode",
                        _borrowMode,
                        value,
                        (mode, reason) => _borrowMode = mode,
                        TransactionHints.Semantic);
                    _borrowMode = value;
                }
            }
        }

        protected override void SetIconViewGeometry()
        {
            Bounds = new SMRect(Left, Top, StockDiagramGeometries.GridSize * 4, StockDiagramGeometries.GridSize * 4);
            var terminals = FixedTerminals.OfType<NodeTerminal>().ToArray();
            terminals[0].Hotspot = new SMPoint(0, StockDiagramGeometries.GridSize * 1);
            terminals[1].Hotspot = new SMPoint(StockDiagramGeometries.GridSize * 4, StockDiagramGeometries.GridSize * 1);
        }

        /// <inheritdoc />
        public override void AcceptVisitor(IElementVisitor visitor)
        {
            var functionVisitor = visitor as IFunctionVisitor;
            if (functionVisitor != null)
            {
                functionVisitor.VisitImmutableBorrowNode(this);
            }
            else
            {
                base.AcceptVisitor(visitor);
            }
        }
    }
}
