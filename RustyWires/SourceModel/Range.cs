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
    /// <summary>
    /// Function that takes integer lower (inclusive) and upper (exclusive) bounds and returns an integer iterator.
    /// </summary>
    public class Range : RustyWiresSimpleNode
    {
        private const string ElementName = "Range";

        protected Range()
        {
            FixedTerminals.Add(new NodeTerminal(Direction.Input, PFTypes.Int32, "lower limit"));
            FixedTerminals.Add(new NodeTerminal(Direction.Input, PFTypes.Int32, "upper limit"));
            FixedTerminals.Add(new NodeTerminal(Direction.Output, PFTypes.Int32.CreateIterator(), "iterator"));
        }

        [XmlParserFactoryMethod(ElementName, RustyWiresFunction.ParsableNamespaceName)]
        public static Range CreateRange(IElementCreateInfo elementCreateInfo)
        {
            var range = new Range();
            range.Init(elementCreateInfo);
            return range;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, RustyWiresFunction.ParsableNamespaceName);

        /// <inheritdoc />
        protected override void SetIconViewGeometry()
        {
            Bounds = new SMRect(Left, Top, StockDiagramGeometries.GridSize * 4, StockDiagramGeometries.GridSize * 4);
            var terminals = Terminals.OfType<NodeTerminal>().ToArray();
            terminals[0].Hotspot = new SMPoint(0, StockDiagramGeometries.GridSize * 1);
            terminals[1].Hotspot = new SMPoint(0, StockDiagramGeometries.GridSize * 3);
            terminals[2].Hotspot = new SMPoint(StockDiagramGeometries.GridSize * 4, StockDiagramGeometries.GridSize * 1);
        }

        /// <inheritdoc />
        public override void AcceptVisitor(IElementVisitor visitor)
        {
            var rustyWiresVisitor = visitor as IRustyWiresFunctionVisitor;
            if (rustyWiresVisitor != null)
            {
                rustyWiresVisitor.VisitRange(this);
            }
            else
            {
                base.AcceptVisitor(visitor);
            }
        }
    }
}
