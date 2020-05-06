using System.Xml.Linq;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;

namespace Rebar.SourceModel
{
    public class CaseStructure : NationalInstruments.VI.SourceModel.CaseStructure
    {
        private const string ElementName = "CaseStructure";
        
        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static CaseStructure CreateCaseStructure(IElementCreateInfo elementCreateInfo)
        {
            var caseStructure = new CaseStructure();
            caseStructure.Initialize(elementCreateInfo);
            return caseStructure;
        }

        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);
    }
}
