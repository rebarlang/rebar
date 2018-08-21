using System.Xml.Linq;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;
using NationalInstruments.VI.SourceModel;

namespace RustyWires.SourceModel
{
    public class RustyWiresCaseStructure : CaseStructure
    {
        private const string ElementName = "RustyWiresCaseStructure";
        
        [XmlParserFactoryMethod(ElementName, RustyWiresFunction.ParsableNamespaceName)]
        public static RustyWiresCaseStructure CreateRustyWiresFlatSequence(IElementCreateInfo elementCreateInfo)
        {
            var rwCaseStructure = new RustyWiresCaseStructure();
            rwCaseStructure.Init(elementCreateInfo);
            return rwCaseStructure;
        }

        public override XName XmlElementName => XName.Get(ElementName, RustyWiresFunction.ParsableNamespaceName);
    }
}
