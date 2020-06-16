using System.Xml.Linq;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;

namespace Rebar.SourceModel
{
    public class OptionPatternStructureDiagram : MatchStructureDiagramBase
    {
        private const string ElementName = "OptionPatternStructure.Diagram";

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static OptionPatternStructureDiagram CreateOptionPatternStructureDiagram(IElementCreateInfo elementCreateInfo)
        {
            var optionPatternStructureDiagram = new OptionPatternStructureDiagram();
            optionPatternStructureDiagram.Initialize(elementCreateInfo);
            return optionPatternStructureDiagram;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);
    }
}
