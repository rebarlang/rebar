using NationalInstruments.Core;
using NationalInstruments.DynamicProperties;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;
using Rebar.Common;

namespace Rebar.SourceModel
{
    /// <summary>
    /// <see cref="SimpleTunnel"/> for borrowing references on entering a <see cref="Loop"/>.
    /// </summary>
    public class LoopBorrowTunnel : SimpleTunnel, IBeginLifetimeTunnel, IBorrowTunnel
    {
        public static readonly PropertySymbol TerminateLifetimeTunnelPropertySymbol =
            ExposeIdReferenceProperty<LoopBorrowTunnel>(
                "TerminateLifetimeTunnel",
                loopBorrowTunnel => loopBorrowTunnel.TerminateLifetimeTunnel,
                (loopBorrowTunnel, terminateLifetimeTunnel) => loopBorrowTunnel.TerminateLifetimeTunnel = (LoopTerminateLifetimeTunnel)terminateLifetimeTunnel);

        public static readonly PropertySymbol BorrowModePropertySymbol =
            ExposeStaticProperty<LoopBorrowTunnel>(
                "BorrowMode",
                borrowTunnel => borrowTunnel.BorrowMode,
                (borrowTunnel, value) => borrowTunnel.BorrowMode = (BorrowMode)value,
                PropertySerializers.CreateEnumSerializer<BorrowMode>(),
                BorrowMode.Immutable
            );

        private BorrowMode _borrowMode;

        public LoopBorrowTunnel()
        {
            Docking = BorderNodeDocking.Left;
            _borrowMode = BorrowMode.Immutable;
        }

        public override BorderNodeRelationship Relationship => BorderNodeRelationship.AncestorToDescendant;

        public override BorderNodeMultiplicity Multiplicity => BorderNodeMultiplicity.OneToOne;

        public ITerminateLifetimeTunnel TerminateLifetimeTunnel { get; set; }

        public BorrowMode BorrowMode
        {
            get { return _borrowMode; }
            set
            {
                if (_borrowMode != value)
                {
                    TransactionRecruiter.EnlistPropertyItem(
                        this,
                        "BorrowMode",
                        _borrowMode,
                        value,
                        (mode, reason) => _borrowMode = mode,
                        TransactionHints.Semantic);
                    _borrowMode = value;
                }
            }
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
            Docking = BorderNodeDocking.Left;
            base.EnsureViewDirectional(hints, oldBoundsMinusNewbounds);
        }
    }
}
