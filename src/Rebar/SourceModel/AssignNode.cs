using System.Linq;
using System.Xml.Linq;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;
using Rebar.Common;
using Rebar.Compiler;

namespace Rebar.SourceModel
{
    /// <summary>
    /// Node that consumes one variable and assigns its value into another, whose old 0value is dropped.
    /// </summary>
    public class AssignNode : SimpleNode
    {
        private const string ElementName = "AssignNode";

        protected AssignNode()
        {
            FixedTerminals.Add(new NodeTerminal(Direction.Input, PFTypes.Void.CreateMutableReference(), "assignee in"));
            FixedTerminals.Add(new NodeTerminal(Direction.Input, PFTypes.Void, "value in"));
            FixedTerminals.Add(new NodeTerminal(Direction.Output, PFTypes.Void.CreateMutableReference(), "assignee out"));
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static AssignNode CreateAssignNode(IElementCreateInfo elementCreateInfo)
        {
            var assignNode = new AssignNode();
            assignNode.Init(elementCreateInfo);
            return assignNode;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

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
            var functionVisitor = visitor as IFunctionVisitor;
            if (functionVisitor != null)
            {
                functionVisitor.VisitAssignNode(this);
            }
            else
            {
                base.AcceptVisitor(visitor);
            }
        }
    }
}
