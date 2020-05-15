using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using NationalInstruments;
using NationalInstruments.DataTypes;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;
using Rebar.Common;

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

        private VariantMatchStructure VariantMatchStructure => (VariantMatchStructure)Owner;

        /// <inheritdoc />
        public override IEnumerable<Terminal> VisibleTerminals
        {
            get
            {
                yield return PrimaryOuterTerminal;
                yield return GetTerminalInDiagram(VariantMatchStructure.SelectedDiagram);
            }
        }

        /// <inheritdoc />
        public override Terminal CreateTerminal(string identifier)
        {
            return new VariantMatchStructureSelectorTerminal(identifier);
        }
    }

    internal class VariantMatchStructureSelectorTerminal : BorderNodeTerminal
    {
        public VariantMatchStructureSelectorTerminal()
        {
        }

        public VariantMatchStructureSelectorTerminal(string terminalIdentifier)
        {
            TerminalIdentifier = terminalIdentifier;
        }

        private VariantMatchStructureSelector VariantMatchStructureSelector => (VariantMatchStructureSelector)Owner;

        /// <inheritdoc />
        public override bool Visible
        {
            get
            {
                if (Direction == NationalInstruments.CommonModel.Direction.Input)
                {
                    return true;
                }

                var inputTerminal = VariantMatchStructureSelector.InputTerminals.First();
                NIType inputType = inputTerminal.DataType;
                if (inputType.IsUnion())
                {
                    int index = VariantMatchStructureSelector.OutputTerminals.IndexOf(this);
                    NIType fieldType = inputType.GetFields().ElementAt(index).GetDataType();
                    if (fieldType.IsUnit())
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
