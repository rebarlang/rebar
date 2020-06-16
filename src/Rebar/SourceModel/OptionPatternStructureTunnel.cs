using System.Xml.Linq;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;

namespace Rebar.SourceModel
{
    public class OptionPatternStructureTunnel : MatchStructureTunnelBase
    {
        private const string ElementName = "OptionPatternStructureTunnel";

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static OptionPatternStructureTunnel CreateOptionPatternStructureTunnel(IElementCreateInfo elementCreateInfo)
        {
            var optionPatternStructureTunnel = new OptionPatternStructureTunnel();
            optionPatternStructureTunnel.Initialize(elementCreateInfo);
            return optionPatternStructureTunnel;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);
    }
}
