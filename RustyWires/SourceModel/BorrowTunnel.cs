using NationalInstruments.Core;
using NationalInstruments.DynamicProperties;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;
using NationalInstruments.VI.SourceModel;
using RustyWires.Common;

namespace RustyWires.SourceModel
{
    public class BorrowTunnel : FlatSequenceTunnel
    {
        public static readonly PropertySymbol NodeTerminalsPropertySymbol =
            ExposeStaticProperty<BorrowTunnel>(
                "BorrowMode",
                borrowTunnel => borrowTunnel.BorrowMode,
                (borrowTunnel, value) => borrowTunnel.BorrowMode = (BorrowMode)value,
                PropertySerializers.CreateEnumSerializer<BorrowMode>(),
                BorrowMode.Immutable
            );

        private BorrowMode _borrowMode;

        public BorrowTunnel()
        {
            Docking = BorderNodeDocking.Left;
            _borrowMode = BorrowMode.Immutable;
        }

        public override BorderNodeRelationship Relationship => BorderNodeRelationship.AncestorToDescendant;

        // TODO: this will not be the case for BorrowTunnels on case structures
        public override BorderNodeMultiplicity Multiplicity => BorderNodeMultiplicity.OneToOne;

        public UnborrowTunnel UnborrowTunnel { get; set; }

        public BorrowMode BorrowMode
        {
            get { return _borrowMode;}
            set
            {
                if (_borrowMode != value)
                {
                    TransactionRecruiter.EnlistPropertyItem(
                        this, 
                        "BorrowMode", 
                        _borrowMode, 
                        value, 
                        (mode, reason) => this._borrowMode = mode, 
                        TransactionHints.Semantic);
                    _borrowMode = value;
                }
            }
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
            Docking = BorderNodeDocking.Left;
            base.EnsureViewDirectional(hints, oldBoundsMinusNewbounds);
        }
    }
}
