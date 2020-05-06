using System.Xml.Linq;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;

namespace Rebar.SourceModel
{
    public class RootDiagram : NationalInstruments.SourceModel.RootDiagram
    {
        private const string ElementName = "RootDiagram";

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static RootDiagram CreateRootDiagram(IElementCreateInfo elementCreateInfo)
        {
            var diagram = new RootDiagram();
            diagram.Initialize(elementCreateInfo);
            return diagram;
        }

        public RootDiagram()
        {
        }

        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);
    }
}
