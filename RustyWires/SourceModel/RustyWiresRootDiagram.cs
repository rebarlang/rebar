using System.Xml.Linq;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;

namespace RustyWires.SourceModel
{
    public class RustyWiresRootDiagram : RootDiagram
    {
        private const string ElementName = "RustyWiresRootDiagram";

        [XmlParserFactoryMethod(ElementName, RustyWiresFunction.ParsableNamespaceName)]
        public static RustyWiresRootDiagram CreateRustyWiresRootDiagram(IElementCreateInfo elementCreateInfo)
        {
            var diagram = new RustyWiresRootDiagram();
            diagram.Init(elementCreateInfo);
            return diagram;
        }

        public RustyWiresRootDiagram()
        {
        }

        public override XName XmlElementName => XName.Get(ElementName, RustyWiresFunction.ParsableNamespaceName);
    }
}
