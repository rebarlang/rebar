using System.Xml.Linq;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;

namespace Rebar.SourceModel
{
    public sealed class VariantMatchStructureDiagram : MatchStructureDiagramBase
    {
        private const string ElementName = "VariantMatchStructure.Diagram";

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static VariantMatchStructureDiagram CreateVariantMatchStructureDiagram(IElementCreateInfo elementCreateInfo)
        {
            var variantMatchStructureDiagram = new VariantMatchStructureDiagram();
            variantMatchStructureDiagram.Initialize(elementCreateInfo);
            return variantMatchStructureDiagram;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);
    }
}
