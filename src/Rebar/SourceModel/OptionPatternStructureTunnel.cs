using System.Xml.Linq;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;
using NationalInstruments.VI.SourceModel;

namespace Rebar.SourceModel
{
    public class OptionPatternStructureTunnel : StackedStructureTunnel
    {
        private const string ElementName = "OptionPatternStructureTunnel";

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static OptionPatternStructureTunnel CreateOptionPatternStructureTunnel(IElementCreateInfo elementCreateInfo)
        {
            var optionPatternStructureTunnel = new OptionPatternStructureTunnel();
            optionPatternStructureTunnel.Init(elementCreateInfo);
            return optionPatternStructureTunnel;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        /// <inheritdoc />
        public override BorderNodeMultiplicity Multiplicity => BorderNodeMultiplicity.OneToMany;
    }
}
