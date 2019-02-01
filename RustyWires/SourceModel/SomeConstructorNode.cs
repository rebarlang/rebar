using System.Linq;
using System.Xml.Linq;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;
using RustyWires.Compiler;

namespace RustyWires.SourceModel
{
    /// <summary>
    /// Node that constructs a mutable Some(x) value from an input value x; the output type is Option&lt;T&gt; 
    /// when the input type is T.
    /// </summary>
    public class SomeConstructorNode : RustyWiresSimpleNode
    {
        private const string ElementName = "SomeConstructor";

        protected SomeConstructorNode()
        {
            FixedTerminals.Add(new NodeTerminal(Direction.Input, PFTypes.Void, "value in"));
            FixedTerminals.Add(new NodeTerminal(Direction.Output, PFTypes.Void.CreateOption(), "Some value out"));
        }

        [XmlParserFactoryMethod(ElementName, RustyWiresFunction.ParsableNamespaceName)]
        public static SomeConstructorNode CreateSomeConstructorNode(IElementCreateInfo elementCreateInfo)
        {
            var someConstructor = new SomeConstructorNode();
            someConstructor.Init(elementCreateInfo);
            return someConstructor;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, RustyWiresFunction.ParsableNamespaceName);

        /// <inheritdoc />
        protected override void SetIconViewGeometry()
        {
            const int width = StockDiagramGeometries.GridSize * 8;
            Bounds = new SMRect(Left, Top, width, StockDiagramGeometries.GridSize * 4);
            var terminals = FixedTerminals.OfType<NodeTerminal>().ToArray();
            terminals[0].Hotspot = new SMPoint(0, StockDiagramGeometries.GridSize * 1);
            terminals[1].Hotspot = new SMPoint(width, StockDiagramGeometries.GridSize * 1);
        }

        /// <inheritdoc />
        public override void AcceptVisitor(IElementVisitor visitor)
        {
            var rustyWiresVisitor = visitor as IRustyWiresFunctionVisitor;
            if (rustyWiresVisitor != null)
            {
                rustyWiresVisitor.VisitSomeConstructorNode(this);
            }
            else
            {
                base.AcceptVisitor(visitor);
            }
        }
    }
}
