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
    public class ImmutablePassthroughNode : RustyWiresSimpleNode
    {
        private const string ElementName = "ImmutablePassthroughNode";

        protected ImmutablePassthroughNode()
        {
            var immutableReferenceType = PFTypes.Void.CreateImmutableReference();
            FixedTerminals.Add(new NodeTerminal(Direction.Input, immutableReferenceType, "reference in"));
            FixedTerminals.Add(new NodeTerminal(Direction.Output, immutableReferenceType, "reference out"));
        }

        [XmlParserFactoryMethod(ElementName, RustyWiresFunction.ParsableNamespaceName)]
        public static ImmutablePassthroughNode CreateImmutablePassthroughNode(IElementCreateInfo elementCreateInfo)
        {
            var immutablePassthroughNode = new ImmutablePassthroughNode();
            immutablePassthroughNode.Init(elementCreateInfo);
            return immutablePassthroughNode;
        }

        public override XName XmlElementName => XName.Get(ElementName, RustyWiresFunction.ParsableNamespaceName);

        protected override void SetIconViewGeometry()
        {
            Bounds = new SMRect(Left, Top, StockDiagramGeometries.GridSize * 4, StockDiagramGeometries.GridSize * 2);
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
                rustyWiresVisitor.VisitImmutablePassthroughNode(this);
            }
            else
            {
                base.AcceptVisitor(visitor);
            }
        }
    }
}
