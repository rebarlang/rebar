using System.Linq;
using System.Xml.Linq;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;
using RustyWires.Compiler;

namespace RustyWires.SourceModel
{
    public class Freeze : RustyWiresSimpleNode
    {
        private const string ElementName = "Freeze";

        protected Freeze()
        {
            FixedTerminals.Add(new NodeTerminal(Direction.Input, PFTypes.Void, "mutable value in"));
            FixedTerminals.Add(new NodeTerminal(Direction.Output, PFTypes.Void, "immutable value out"));
        }

        [XmlParserFactoryMethod(ElementName, RustyWiresFunction.ParsableNamespaceName)]
        public static Freeze CreateFreeze(IElementCreateInfo elementCreateInfo)
        {
            var freeze = new Freeze();
            freeze.Init(elementCreateInfo);
            return freeze;
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
                rustyWiresVisitor.VisitFreezeNode(this);
            }
            else
            {
                base.AcceptVisitor(visitor);
            }
        }
    }
}
