using System.Linq;
using System.Xml.Linq;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;
using RustyWires.Compiler;

namespace RustyWires.SourceModel
{
    public class DropNode : RustyWiresSimpleNode
    {
        private const string ElementName = "DropNode";

        protected DropNode()
        {
            FixedTerminals.Add(new NodeTerminal(Direction.Input, PFTypes.Void, "value"));
        }

        [XmlParserFactoryMethod(ElementName, RustyWiresFunction.ParsableNamespaceName)]
        public static DropNode CreateDropNode(IElementCreateInfo elementCreateInfo)
        {
            var dropNode = new DropNode();
            dropNode.Init(elementCreateInfo);
            dropNode.SetIconViewGeometry();
            return dropNode;
        }

        public override XName XmlElementName => XName.Get(ElementName, RustyWiresFunction.ParsableNamespaceName);

        protected override void SetIconViewGeometry()
        {
            Bounds = new SMRect(Left, Top, StockDiagramGeometries.GridSize * 4, StockDiagramGeometries.GridSize * 4);
            var terminals = FixedTerminals.OfType<NodeTerminal>().ToArray();
            terminals[0].Hotspot = new SMPoint(0, StockDiagramGeometries.GridSize * 1);
        }

        /// <inheritdoc />
        public override void AcceptVisitor(IElementVisitor visitor)
        {
            var rustyWiresVisitor = visitor as IRustyWiresFunctionVisitor;
            if (rustyWiresVisitor != null)
            {
                rustyWiresVisitor.VisitDropNode(this);
            }
            else
            {
                base.AcceptVisitor(visitor);
            }
        }
    }
}
