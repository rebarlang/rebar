using System.Xml.Linq;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;

namespace Rebar.SourceModel
{
    public sealed class VariantMatchStructureTunnel : MatchStructureTunnelBase
    {
        private const string ElementName = "VariantMatchStructureTunnel";

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static VariantMatchStructureTunnel CreateVariantMatchStructureTunnel(IElementCreateInfo elementCreateInfo)
        {
            var variantMatchStructureTunnel = new VariantMatchStructureTunnel();
            variantMatchStructureTunnel.Initialize(elementCreateInfo);
            return variantMatchStructureTunnel;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);
    }
}
