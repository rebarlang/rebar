using System.Xml.Linq;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;

namespace Rebar.SourceModel
{
    public class OptionPatternStructure : MatchStructureBase
    {
        private const string ElementName = "OptionPatternStructure";

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static OptionPatternStructure CreateOptionPatternStructure(IElementCreateInfo elementCreateInfo)
        {
            var optionPatternStructure = new OptionPatternStructure();
            optionPatternStructure.Initialize(elementCreateInfo);
            return optionPatternStructure;
        }

        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        /// <inheritdoc />
        protected override RectangleSides GetSidesForBorderNode(BorderNode borderNode) => borderNode is OptionPatternStructureSelector
            ? RectangleSides.Left
            : RectangleSides.All;

        /// <inheritdoc />
        public override BorderNode MakeDefaultBorderNode(Diagram startDiagram, Diagram endDiagram, Wire wire, StructureIntersection intersection)
        {
            return MakeDefaultTunnelCore<OptionPatternStructureTunnel>(startDiagram, endDiagram, wire);
        }
    }
}
