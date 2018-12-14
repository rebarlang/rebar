using System.Xml.Linq;
using NationalInstruments.Core;
using NationalInstruments.DynamicProperties;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;

namespace RustyWires.SourceModel
{
    /// <summary>
    /// Left-side tunnel for <see cref="Loop"/> that creates a mutable boolean condition variable.
    /// </summary>
    public class LoopConditionTunnel : SimpleTunnel, IBeginLifetimeTunnel
    {
        private const string ElementName = "LoopConditionTunnel";

        public static readonly PropertySymbol TerminateLifetimeTunnelPropertySymbol =
            ExposeIdReferenceProperty<LoopConditionTunnel>(
                "TerminateLifetimeTunnel",
                loopConditionTunnel => loopConditionTunnel.TerminateLifetimeTunnel,
                (loopConditionTunnel, terminateLifetimeTunnel) => loopConditionTunnel.TerminateLifetimeTunnel = (LoopTerminateLifetimeTunnel)terminateLifetimeTunnel);

        [XmlParserFactoryMethod(ElementName, RustyWiresFunction.ParsableNamespaceName)]
        public static LoopConditionTunnel CreateLoopConditionTunnel(IElementCreateInfo elementCreateInfo)
        {
            var loopConditionTunnel = new LoopConditionTunnel();
            loopConditionTunnel.Init(elementCreateInfo);
            return loopConditionTunnel;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, RustyWiresFunction.ParsableNamespaceName);

        /// <inheritdoc />
        public override BorderNodeMultiplicity Multiplicity => BorderNodeMultiplicity.OneToOne;

        /// <inheritdoc />
        public override bool CanDelete => false;

        /// <inheritdoc />
        public ITerminateLifetimeTunnel TerminateLifetimeTunnel { get; set; }

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
