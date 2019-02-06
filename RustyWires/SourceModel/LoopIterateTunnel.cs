using System.Xml.Linq;
using NationalInstruments.Core;
using NationalInstruments.DynamicProperties;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;

namespace RustyWires.SourceModel
{
    /// <summary>
    /// <see cref="SimpleTunnel"/> for using an Iterator inside a <see cref="Loop"/>.
    /// </summary>
    public class LoopIterateTunnel : SimpleTunnel, IBeginLifetimeTunnel
    {
        private const string ElementName = "LoopIterateTunnel";

        public static readonly PropertySymbol TerminateLifetimeTunnelPropertySymbol =
            ExposeIdReferenceProperty<LoopIterateTunnel>(
                "TerminateLifetimeTunnel",
                loopIterateTunnel => loopIterateTunnel.TerminateLifetimeTunnel,
                (loopIterateTunnel, terminateLifetimeTunnel) => loopIterateTunnel.TerminateLifetimeTunnel = (LoopTerminateLifetimeTunnel)terminateLifetimeTunnel);

        [XmlParserFactoryMethod(ElementName, RustyWiresFunction.ParsableNamespaceName)]
        public static LoopIterateTunnel CreateLoopIterateTunnel(IElementCreateInfo elementCreateInfo)
        {
            var loopIterateTunnel = new LoopIterateTunnel();
            loopIterateTunnel.Init(elementCreateInfo);
            return loopIterateTunnel;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, RustyWiresFunction.ParsableNamespaceName);

        public LoopIterateTunnel()
        {
            Docking = BorderNodeDocking.Left;
        }

        /// <inheritdoc />
        public override BorderNodeRelationship Relationship => BorderNodeRelationship.AncestorToDescendant;

        /// <inheritdoc />
        public override BorderNodeMultiplicity Multiplicity => BorderNodeMultiplicity.OneToOne;

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
