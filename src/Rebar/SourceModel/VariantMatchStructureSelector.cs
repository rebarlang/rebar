using System.Xml.Linq;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;

namespace Rebar.SourceModel
{
    public sealed class VariantMatchStructureSelector : MatchStructureSelectorBase
    {
        private const string ElementName = "VariantMatchStructureSelector";

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static VariantMatchStructureSelector CreateVariantMatchStructureSelector(IElementCreateInfo elementCreateInfo)
        {
            var variantMatchStructureSelector = new VariantMatchStructureSelector();
            variantMatchStructureSelector.Initialize(elementCreateInfo);
            return variantMatchStructureSelector;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);
    }
}
