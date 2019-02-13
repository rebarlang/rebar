using System.Xml.Linq;
using NationalInstruments.Core;
using NationalInstruments.DynamicProperties;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;

namespace Rebar.SourceModel
{
    public class LoopTerminateLifetimeTunnel : SimpleTunnel, ITerminateLifetimeTunnel
    {
        private const string ElementName = "LoopTerminateLifetimeTunnel";

        public static readonly PropertySymbol BeginLifetimeTunnelPropertySymbol =
            ExposeIdReferenceProperty<LoopTerminateLifetimeTunnel>(
                "BeginLifetimeTunnel",
                loopTerminateLifetimeTunnel => loopTerminateLifetimeTunnel.BeginLifetimeTunnel,
                (loopTerminateLifetimeTunnel, beginLifetimeTunnel) => loopTerminateLifetimeTunnel.BeginLifetimeTunnel = (IBeginLifetimeTunnel)beginLifetimeTunnel);

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static LoopTerminateLifetimeTunnel CreateLoopConditionTunnel(IElementCreateInfo elementCreateInfo)
        {
            var loopTerminateLifetimeTunnel = new LoopTerminateLifetimeTunnel();
            loopTerminateLifetimeTunnel.Init(elementCreateInfo);
            return loopTerminateLifetimeTunnel;
        }

        public LoopTerminateLifetimeTunnel()
        {
            // TODO: can't do this because it fails SimpleTunnel.CheckModel. Need to be SimpleTunnel because
            // I don't see any other way of getting what SimpleTunnel.OnOwnerChanedFromNewTransaction does.
            // RemoveWireableTerminal(PrimaryInnerTerminals.First());
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        /// <inheritdoc />
        public override BorderNodeMultiplicity Multiplicity => BorderNodeMultiplicity.OneToOne;

        public IBeginLifetimeTunnel BeginLifetimeTunnel { get; set; }

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
            Docking = BorderNodeDocking.Right;
            BeginLifetimeTunnel.Top = Top;
            base.EnsureViewDirectional(hints, oldBoundsMinusNewbounds);
        }
    }
}
