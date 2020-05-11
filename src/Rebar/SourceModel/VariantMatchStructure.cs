using System.Xml.Linq;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;

namespace Rebar.SourceModel
{
    public class VariantMatchStructure : MatchStructureBase
    {
        private const string ElementName = "VariantMatchStructure";

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static VariantMatchStructure CreateVariantMatchStructure(IElementCreateInfo elementCreateInfo)
        {
            var variantMatchStructure = new VariantMatchStructure();
            variantMatchStructure.Initialize(elementCreateInfo);
            return variantMatchStructure;
        }

        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        protected override RectangleSides GetSidesForBorderNode(BorderNode borderNode) => RectangleSides.All;

        public override BorderNode MakeDefaultBorderNode(Diagram startDiagram, Diagram endDiagram, Wire wire, StructureIntersection intersection)
        {
            return MakeDefaultTunnelCore<VariantMatchStructureTunnel>(startDiagram, endDiagram, wire);
        }
    }
}
