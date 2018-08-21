using System.Linq;
using System.Xml.Linq;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;
using RustyWires.Compiler;

namespace RustyWires.SourceModel
{
    public class CreateMutableCopyNode : RustyWiresSimpleNode
    {
        private const string ElementName = "CreateMutableCopyNode";

        protected CreateMutableCopyNode()
        {
            var immutableReferenceType = PFTypes.Void.CreateImmutableReference();
            FixedTerminals.Add(new NodeTerminal(Direction.Input, immutableReferenceType, "reference in"));
            FixedTerminals.Add(new NodeTerminal(Direction.Output, immutableReferenceType, "reference out"));
            FixedTerminals.Add(new NodeTerminal(Direction.Output, PFTypes.Void.CreateMutableValue(), "mutable copy"));
        }

        [XmlParserFactoryMethod(ElementName, RustyWiresFunction.ParsableNamespaceName)]
        public static CreateMutableCopyNode CreateCreateMutableCopyNode(IElementCreateInfo elementCreateInfo)
        {
            var createMutableCopyNode = new CreateMutableCopyNode();
            createMutableCopyNode.Init(elementCreateInfo);
            return createMutableCopyNode;
        }

        public override XName XmlElementName => XName.Get(ElementName, RustyWiresFunction.ParsableNamespaceName);

        protected override void SetIconViewGeometry()
        {
            Bounds = new SMRect(Left, Top, StockDiagramGeometries.GridSize * 4, StockDiagramGeometries.GridSize * 4);
            var terminals = Terminals.OfType<NodeTerminal>().ToArray();
            terminals[0].Hotspot = new SMPoint(0, StockDiagramGeometries.GridSize * 1);
            terminals[1].Hotspot = new SMPoint(StockDiagramGeometries.GridSize * 4, StockDiagramGeometries.GridSize * 1);
            terminals[2].Hotspot = new SMPoint(StockDiagramGeometries.GridSize * 4, StockDiagramGeometries.GridSize * 3);
        }

        /// <inheritdoc />
        public override void AcceptVisitor(IElementVisitor visitor)
        {
            var rustyWiresVisitor = visitor as IRustyWiresFunctionVisitor;
            if (rustyWiresVisitor != null)
            {
                rustyWiresVisitor.VisitCreateMutableCopyNode(this);
            }
            else
            {
                base.AcceptVisitor(visitor);
            }
        }
    }
}
