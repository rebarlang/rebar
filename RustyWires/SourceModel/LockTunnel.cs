using System.Xml.Linq;
using NationalInstruments.Core;
using NationalInstruments.DynamicProperties;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;
using NationalInstruments.VI.SourceModel;

namespace RustyWires.SourceModel
{
    public class LockTunnel : FlatSequenceTunnel, IBeginLifetimeTunnel
    {
        private const string ElementName = "LockTunnel";

        [XmlParserFactoryMethod(ElementName, RustyWiresFunction.ParsableNamespaceName)]
        public static LockTunnel CreateLockTunnel(IElementCreateInfo elementCreateInfo)
        {
            var lockTunnel = new LockTunnel();
            lockTunnel.Init(elementCreateInfo);
            return lockTunnel;
        }

        public static readonly PropertySymbol TerminateLifetimeTunnelPropertySymbol =
            ExposeIdReferenceProperty<LockTunnel>(
                "TerminateLifetimeTunnel",
                lockTunnel => lockTunnel.TerminateLifetimeTunnel,
                (lockTunnel, terminateLifetimeTunnel) => lockTunnel.TerminateLifetimeTunnel = (FlatSequenceTerminateLifetimeTunnel)terminateLifetimeTunnel);

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, RustyWiresFunction.ParsableNamespaceName);

        public LockTunnel()
        {
            Docking = BorderNodeDocking.Left;
        }

        public override BorderNodeRelationship Relationship => BorderNodeRelationship.AncestorToDescendant;

        public override BorderNodeMultiplicity Multiplicity => BorderNodeMultiplicity.OneToOne;

        public ITerminateLifetimeTunnel TerminateLifetimeTunnel { get; set; }

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
