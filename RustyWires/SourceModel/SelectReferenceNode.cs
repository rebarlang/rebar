using System.Linq;
using System.Xml.Linq;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;
using RustyWires.Compiler;

namespace RustyWires.SourceModel
{
    public class SelectReferenceNode : RustyWiresSimpleNode
    {
        private const string ElementName = "SelectReferenceNode";

        protected SelectReferenceNode()
        {
            var immutableReferenceType = PFTypes.Void.CreateImmutableReference();
            var booleanImmutableReferenceType = PFTypes.Boolean.CreateImmutableReference();
            FixedTerminals.Add(new NodeTerminal(Direction.Input, immutableReferenceType, "reference in 1"));
            FixedTerminals.Add(new NodeTerminal(Direction.Input, immutableReferenceType, "reference in 2"));
            FixedTerminals.Add(new NodeTerminal(Direction.Input, booleanImmutableReferenceType, "selector in"));
            FixedTerminals.Add(new NodeTerminal(Direction.Output, immutableReferenceType, "reference out 1"));
            FixedTerminals.Add(new NodeTerminal(Direction.Output, immutableReferenceType, "reference out 2"));
            FixedTerminals.Add(new NodeTerminal(Direction.Output, booleanImmutableReferenceType, "selector out"));
            FixedTerminals.Add(new NodeTerminal(Direction.Output, immutableReferenceType, "selected reference out"));
        }

        [XmlParserFactoryMethod(ElementName, RustyWiresFunction.ParsableNamespaceName)]
        public static SelectReferenceNode CreateSelectReferenceNode(IElementCreateInfo elementCreateInfo)
        {
            var selectReferenceNode = new SelectReferenceNode();
            selectReferenceNode.Init(elementCreateInfo);
            return selectReferenceNode;
        }

        public override XName XmlElementName => XName.Get(ElementName, RustyWiresFunction.ParsableNamespaceName);

        protected override void SetIconViewGeometry()
        {
            Bounds = new SMRect(Left, Top, StockDiagramGeometries.GridSize * 8, StockDiagramGeometries.GridSize * 8);
            var terminals = FixedTerminals.OfType<NodeTerminal>().ToArray();
            terminals[0].Hotspot = new SMPoint(0, StockDiagramGeometries.GridSize * 1);
            terminals[1].Hotspot = new SMPoint(0, StockDiagramGeometries.GridSize * 3);
            terminals[2].Hotspot = new SMPoint(0, StockDiagramGeometries.GridSize * 5);
            terminals[3].Hotspot = new SMPoint(StockDiagramGeometries.GridSize * 8, StockDiagramGeometries.GridSize * 1);
            terminals[4].Hotspot = new SMPoint(StockDiagramGeometries.GridSize * 8, StockDiagramGeometries.GridSize * 3);
            terminals[5].Hotspot = new SMPoint(StockDiagramGeometries.GridSize * 8, StockDiagramGeometries.GridSize * 5);
            terminals[6].Hotspot = new SMPoint(StockDiagramGeometries.GridSize * 8, StockDiagramGeometries.GridSize * 7);
        }

        /// <inheritdoc />
        public override void AcceptVisitor(IElementVisitor visitor)
        {
            var rustyWiresVisitor = visitor as IRustyWiresFunctionVisitor;
            if (rustyWiresVisitor != null)
            {
                rustyWiresVisitor.VisitSelectReferenceNode(this);
            }
            else
            {
                base.AcceptVisitor(visitor);
            }
        }
    }
}
