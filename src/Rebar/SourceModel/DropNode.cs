using System.Linq;
using System.Xml.Linq;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;
using Rebar.Compiler;

namespace Rebar.SourceModel
{
    public class DropNode : SimpleNode
    {
        private const string ElementName = "DropNode";

        protected DropNode()
        {
            FixedTerminals.Add(new NodeTerminal(Direction.Input, PFTypes.Void, "value"));
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static DropNode CreateDropNode(IElementCreateInfo elementCreateInfo)
        {
            var dropNode = new DropNode();
            dropNode.Init(elementCreateInfo);
            dropNode.SetIconViewGeometry();
            return dropNode;
        }

        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        protected override void SetIconViewGeometry()
        {
            Bounds = new SMRect(Left, Top, StockDiagramGeometries.GridSize * 4, StockDiagramGeometries.GridSize * 4);
            var terminals = FixedTerminals.OfType<NodeTerminal>().ToArray();
            terminals[0].Hotspot = new SMPoint(0, StockDiagramGeometries.GridSize * 1);
        }

        /// <inheritdoc />
        public override void AcceptVisitor(IElementVisitor visitor)
        {
            var functionVisitor = visitor as IFunctionVisitor;
            if (functionVisitor != null)
            {
                functionVisitor.VisitDropNode(this);
            }
            else
            {
                base.AcceptVisitor(visitor);
            }
        }
    }
}
