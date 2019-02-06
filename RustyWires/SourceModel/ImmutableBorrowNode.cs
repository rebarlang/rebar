using System.Linq;
using System.Xml.Linq;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;
using RustyWires.Common;
using RustyWires.Compiler;

namespace RustyWires.SourceModel
{
    public class ImmutableBorrowNode : RustyWiresSimpleNode
    {
        private const string ElementName = "ImmutableBorrowNode";

        protected ImmutableBorrowNode()
        {
            var immutableReferenceType = PFTypes.Void.CreateImmutableReference();
            FixedTerminals.Add(new NodeTerminal(Direction.Input, immutableReferenceType, "value in"));
            FixedTerminals.Add(new NodeTerminal(Direction.Output, immutableReferenceType, "reference out"));
        }

        [XmlParserFactoryMethod(ElementName, RustyWiresFunction.ParsableNamespaceName)]
        public static ImmutableBorrowNode CreateImmutablePassthroughNode(IElementCreateInfo elementCreateInfo)
        {
            var immutableBorrowNode = new ImmutableBorrowNode();
            immutableBorrowNode.Init(elementCreateInfo);
            return immutableBorrowNode;
        }

        public override XName XmlElementName => XName.Get(ElementName, RustyWiresFunction.ParsableNamespaceName);

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
            var rustyWiresVisitor = visitor as IRustyWiresFunctionVisitor;
            if (rustyWiresVisitor != null)
            {
                rustyWiresVisitor.VisitImmutableBorrowNode(this);
            }
            else
            {
                base.AcceptVisitor(visitor);
            }
        }
    }
}
