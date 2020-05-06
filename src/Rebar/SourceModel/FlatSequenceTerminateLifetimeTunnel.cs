using System.Xml.Linq;
using NationalInstruments.Core;
using NationalInstruments.DynamicProperties;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;
using NationalInstruments.VI.SourceModel;

namespace Rebar.SourceModel
{
    public class FlatSequenceTerminateLifetimeTunnel : FlatSequenceTunnel, ITerminateLifetimeTunnel
    {
        private const string ElementName = "FlatSequenceTerminateLifetimeTunnel";

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static FlatSequenceTerminateLifetimeTunnel CreateFlatSequenceTerminateLifetimeTunnel(IElementCreateInfo elementCreateInfo)
        {
            var flatSequenceTerminateLifetimeTunnel = new FlatSequenceTerminateLifetimeTunnel();
            flatSequenceTerminateLifetimeTunnel.Initialize(elementCreateInfo);
            return flatSequenceTerminateLifetimeTunnel;
        }

        public static readonly PropertySymbol BeginLifetimeTunnelPropertySymbol =
            ExposeIdReferenceProperty<FlatSequenceTerminateLifetimeTunnel>(
                "BeginLifetimeTunnel",
                flatSequenceTerminateLifetimeTunnel => flatSequenceTerminateLifetimeTunnel.BeginLifetimeTunnel,
                (flatSequenceTerminateLifetimeTunnel, beginLifetimeTunnel) => flatSequenceTerminateLifetimeTunnel.BeginLifetimeTunnel = (IBeginLifetimeTunnel)beginLifetimeTunnel);

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        public override BorderNodeRelationship Relationship => BorderNodeRelationship.AncestorToDescendant;

        public override BorderNodeMultiplicity Multiplicity => BorderNodeMultiplicity.OneToOne;

        public IBeginLifetimeTunnel BeginLifetimeTunnel { get; set; }

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
            // BeginLifetimeTunnel may be null during post-parse fixups.
            if (BeginLifetimeTunnel != null)
            {
                BeginLifetimeTunnel.Top = Top;
            }
            base.EnsureViewDirectional(hints, oldBoundsMinusNewbounds);
        }
    }
}
