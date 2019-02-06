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
    /// Node that consumes one variable and assigns its value into another, whose old 0value is dropped.
    /// </summary>
    public class AssignNode : RustyWiresSimpleNode
    {
        private const string ElementName = "AssignNode";

        protected AssignNode()
        {
            FixedTerminals.Add(new NodeTerminal(Direction.Input, PFTypes.Void.CreateMutableReference(), "assignee in"));
            FixedTerminals.Add(new NodeTerminal(Direction.Input, PFTypes.Void, "value in"));
            FixedTerminals.Add(new NodeTerminal(Direction.Output, PFTypes.Void.CreateMutableReference(), "assignee out"));
        }

        [XmlParserFactoryMethod(ElementName, RustyWiresFunction.ParsableNamespaceName)]
        public static AssignNode CreateAssignNode(IElementCreateInfo elementCreateInfo)
        {
            var assignNode = new AssignNode();
            assignNode.Init(elementCreateInfo);
            return assignNode;
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
                rustyWiresVisitor.VisitAssignNode(this);
            }
            else
            {
                base.AcceptVisitor(visitor);
            }
        }
    }
}
