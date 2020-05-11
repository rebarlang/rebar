using System.Xml.Linq;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;

namespace Rebar.SourceModel
{
    public class OptionPatternStructureSelector : MatchStructureSelectorBase
    {
        private const string ElementName = "OptionPatternStructureSelector";

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static OptionPatternStructureSelector CreateOptionPatternStructureSelector(IElementCreateInfo elementCreateInfo)
        {
            var optionPatternStructureSelector = new OptionPatternStructureSelector();
            optionPatternStructureSelector.Initialize(elementCreateInfo);
            return optionPatternStructureSelector;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);
    }
}
