using NationalInstruments.Core;
using NationalInstruments.SourceModel;
using NationalInstruments.VI.SourceModel;

namespace RustyWires.SourceModel
{
    public class LockTunnel : FlatSequenceTunnel
    {
        public LockTunnel()
        {
            Docking = BorderNodeDocking.Left;
        }

        public override BorderNodeRelationship Relationship => BorderNodeRelationship.AncestorToDescendant;

        public override BorderNodeMultiplicity Multiplicity => BorderNodeMultiplicity.OneToOne;

        public FlatSequenceTerminateLifetimeTunnel TerminateScopeTunnel { get; set; }

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
            Docking = BorderNodeDocking.Left;
            base.EnsureViewDirectional(hints, oldBoundsMinusNewbounds);
        }
    }
}
