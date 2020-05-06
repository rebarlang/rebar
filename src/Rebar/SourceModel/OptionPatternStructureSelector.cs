using System.Xml.Linq;
using NationalInstruments.CommonModel;
using NationalInstruments.Core;
using NationalInstruments.DynamicProperties;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;

namespace Rebar.SourceModel
{
    public class OptionPatternStructureSelector : BorderNode
    {
        /// <summary>
        /// <see cref="PropertySymbol"/> for exposing <see cref="BorderNodeTerminal"/>s.
        /// </summary>
        public static readonly PropertySymbol BorderNodeTerminalsPropertySymbol =
            ExposeVariableBorderNodeTerminalsProperty<OptionPatternStructureSelector>(PropertySerializers.BorderNodeTerminalsAllVariableReferenceSerializer);

        private const string ElementName = "OptionPatternStructureSelector";

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static OptionPatternStructureSelector CreateOptionPatternStructureSelector(IElementCreateInfo elementCreateInfo)
        {
            var optionPatternStructureSelector = new OptionPatternStructureSelector();
            optionPatternStructureSelector.Initialize(elementCreateInfo);
            return optionPatternStructureSelector;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        public override BorderNodeMultiplicity Multiplicity => BorderNodeMultiplicity.OneToMany;

        public override bool CanDelete => false;

        public OptionPatternStructureSelector()
        {
            var outerTerminal = MakePrimaryOuterTerminal(null);
            outerTerminal.Direction = Direction.Input;
            var innerTerminal = MakePrimaryInnerTerminal(null);
            innerTerminal.Direction = Direction.Output;

            Docking = BorderNodeDocking.Left;
        }

        /// <inheritdoc />
        public override void EnsureView(EnsureViewHints hints)
        {
            EnsureViewWork(hints, new RectDifference());
        }

        /// <inheritdoc />
        public override void EnsureViewDirectional(EnsureViewHints hints, RectDifference oldBoundsMinusNewBounds)
        {
            EnsureViewWork(hints, oldBoundsMinusNewBounds);
        }

        private void EnsureViewWork(EnsureViewHints hints, RectDifference oldBoundsMinusNewbounds)
        {
            base.EnsureViewDirectional(hints, oldBoundsMinusNewbounds);
        }
    }
}
