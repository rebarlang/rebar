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
    public class ExchangeValues : RustyWiresSimpleNode
    {
        private const string ElementName = "ExchangeValues";

        protected ExchangeValues()
        {
            NIType mutableReferenceType = PFTypes.Void.CreateMutableReference();
            FixedTerminals.Add(new NodeTerminal(Direction.Input, mutableReferenceType, "value in 1"));
            FixedTerminals.Add(new NodeTerminal(Direction.Input, mutableReferenceType, "value in 2"));
            FixedTerminals.Add(new NodeTerminal(Direction.Output, mutableReferenceType, "value out 1"));
            FixedTerminals.Add(new NodeTerminal(Direction.Output, mutableReferenceType, "value out 2"));
        }

        [XmlParserFactoryMethod(ElementName, RustyWiresFunction.ParsableNamespaceName)]
        public static ExchangeValues CreateExchangeValues(IElementCreateInfo elementCreateInfo)
        {
            var exchangeValues = new ExchangeValues();
            exchangeValues.Init(elementCreateInfo);
            return exchangeValues;
        }

        public override XName XmlElementName => XName.Get(ElementName, RustyWiresFunction.ParsableNamespaceName);

        protected override void SetIconViewGeometry()
        {
            Bounds = new SMRect(Left, Top, StockDiagramGeometries.GridSize * 4, StockDiagramGeometries.GridSize * 4);
            var terminals = FixedTerminals.OfType<NodeTerminal>().ToArray();
            terminals[0].Hotspot = new SMPoint(0, StockDiagramGeometries.GridSize * 1);
            terminals[1].Hotspot = new SMPoint(0, StockDiagramGeometries.GridSize * 3);
            terminals[2].Hotspot = new SMPoint(StockDiagramGeometries.GridSize * 4, StockDiagramGeometries.GridSize * 1);
            terminals[3].Hotspot = new SMPoint(StockDiagramGeometries.GridSize * 4, StockDiagramGeometries.GridSize * 3);
        }

        /// <inheritdoc />
        public override void AcceptVisitor(IElementVisitor visitor)
        {
            var rustyWiresVisitor = visitor as IRustyWiresFunctionVisitor;
            if (rustyWiresVisitor != null)
            {
                rustyWiresVisitor.VisitExchangeValuesNode(this);
            }
            else
            {
                base.AcceptVisitor(visitor);
            }
        }
    }
}
