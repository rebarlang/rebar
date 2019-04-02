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
    /// Node that constructs a mutable Some(x) value from an input value x; the output type is Option&lt;T&gt; 
    /// when the input type is T.
    /// </summary>
    public class SomeConstructorNode : SimpleNode
    {
        private const string ElementName = "SomeConstructor";

        protected SomeConstructorNode()
        {
            FixedTerminals.Add(new NodeTerminal(Direction.Input, PFTypes.Void, "value in"));
            FixedTerminals.Add(new NodeTerminal(Direction.Output, PFTypes.Void.CreateOption(), "Some value out"));
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static SomeConstructorNode CreateSomeConstructorNode(IElementCreateInfo elementCreateInfo)
        {
            var someConstructor = new SomeConstructorNode();
            someConstructor.Init(elementCreateInfo);
            return someConstructor;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

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
            var functionVisitor = visitor as IFunctionVisitor;
            if (functionVisitor != null)
            {
                functionVisitor.VisitSomeConstructorNode(this);
            }
            else
            {
                base.AcceptVisitor(visitor);
            }
        }
    }
}
