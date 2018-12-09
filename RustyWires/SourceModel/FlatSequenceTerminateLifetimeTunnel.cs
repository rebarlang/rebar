using NationalInstruments.Core;
using NationalInstruments.SourceModel;
using NationalInstruments.VI.SourceModel;

namespace RustyWires.SourceModel
{
    public class FlatSequenceTerminateLifetimeTunnel : FlatSequenceTunnel
    {
        public override BorderNodeRelationship Relationship => BorderNodeRelationship.AncestorToDescendant;

        public override BorderNodeMultiplicity Multiplicity => BorderNodeMultiplicity.OneToOne;

        public FlatSequenceTunnel BeginLifetimeTunnel { get; set; }

        public FlatSequenceTerminateLifetimeTunnel()
        {
            Docking = BorderNodeDocking.Right;
        }

        public override void EnsureView(EnsureViewHints hints)
        {
            EnsureViewWork(hints, new RectDifference());
        }

        public override void EnsureViewDirectional(EnsureViewHints hints, RectDifference oldBoundsMinusNewBounds)
        {
            EnsureViewWork(hints, oldBoundsMinusNewBounds);
        }

        private void EnsureViewWork(EnsureViewHints hints, RectDifference oldBoundsMinusNewbounds)
        {
            Docking = BorderNodeDocking.Right;
            BeginLifetimeTunnel.Top = Top;
            base.EnsureViewDirectional(hints, oldBoundsMinusNewbounds);
        }
    }
}
