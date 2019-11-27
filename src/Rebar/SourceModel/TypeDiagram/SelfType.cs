using System.Linq;
using System.Xml.Linq;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;

namespace Rebar.SourceModel.TypeDiagram
{
    /// <summary>
    /// Node on the <see cref="TypeDiagramDefinition"/> that represents the type being defined.
    /// </summary>
    public class SelfType : SimpleNode
    {
        private const string ElementName = "SelfType";

        private SelfType()
        {
            FixedTerminals.Add(new NodeTerminal(Direction.Input, PFTypes.Void, "type"));
        }

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static SelfType CreateSelfTypeNode(IElementCreateInfo elementCreateInfo)
        {
            var selfType = new SelfType();
            selfType.Init(elementCreateInfo);
            selfType.SetIconViewGeometry();
            return selfType;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        /// <inheritdoc />
        protected override void SetIconViewGeometry()
        {
            Bounds = new SMRect(Left, Top, StockDiagramGeometries.GridSize * 8, StockDiagramGeometries.GridSize * 4);
            var terminals = FixedTerminals.OfType<NodeTerminal>().ToArray();
            terminals[0].Hotspot = new SMPoint(0, StockDiagramGeometries.GridSize * 1);
        }

        /// <inheritdoc />
        public override bool CanDelete => false;
    }
}
