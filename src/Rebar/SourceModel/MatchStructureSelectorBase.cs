using NationalInstruments.CommonModel;
using NationalInstruments.Core;
using NationalInstruments.DynamicProperties;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;

namespace Rebar.SourceModel
{
    public abstract class MatchStructureSelectorBase : BorderNode
    {
        /// <summary>
        /// <see cref="PropertySymbol"/> for exposing <see cref="BorderNodeTerminal"/>s.
        /// </summary>
        public static readonly PropertySymbol BorderNodeTerminalsPropertySymbol =
            ExposeVariableBorderNodeTerminalsProperty<MatchStructureSelectorBase>(PropertySerializers.BorderNodeTerminalsAllVariableReferenceSerializer);

        public MatchStructureSelectorBase()
        {
            var outerTerminal = MakePrimaryOuterTerminal(null);
            outerTerminal.Direction = Direction.Input;

            Docking = BorderNodeDocking.Left;
        }

        /// <inheritdoc />
        public override BorderNodeMultiplicity Multiplicity => BorderNodeMultiplicity.OneToMany;

        /// <inheritdoc />
        public override bool CanDelete => false;

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
