using System.Xml.Linq;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;
using NationalInstruments.VI.SourceModel;

namespace Rebar.SourceModel
{
    public class OptionPatternStructureDiagram : StackedStructureDiagram
    {
        private const string ElementName = "OptionPatternStructure.Diagram";

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static OptionPatternStructureDiagram CreateOptionPatternStructureDiagram(IElementCreateInfo elementCreateInfo)
        {
            var optionPatternStructureDiagram = new OptionPatternStructureDiagram();
            optionPatternStructureDiagram.Initialize(elementCreateInfo);
            return optionPatternStructureDiagram;
        }

        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        /// <inheritdoc />
        /// <remarks>This is necessary because the ancestor class NestedDiagram returns true for this.</remarks>
        public override bool DoNotGenerateThisElement(ElementGenerationOptions options) => false;
    }
}
