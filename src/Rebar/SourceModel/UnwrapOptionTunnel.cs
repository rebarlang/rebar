using System.Xml.Linq;
using NationalInstruments.Core;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;
using NationalInstruments.VI.SourceModel;

namespace Rebar.SourceModel
{
    /// <summary>
    /// <see cref="Tunnel"/> for the <see cref="FlatSequence"/> that allows unwrapping Option types, and forces its
    /// attached sequence frame and subsequent frames to run conditionally.
    /// </summary>
    public class UnwrapOptionTunnel : FlatSequenceTunnel
    {
        private const string ElementName = "UnwrapOptionTunnel";

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static UnwrapOptionTunnel CreateUnwrapOptionTunnel(IElementCreateInfo elementCreateInfo)
        {
            var unwrapOptionTunnel = new UnwrapOptionTunnel();
            unwrapOptionTunnel.Initialize(elementCreateInfo);
            return unwrapOptionTunnel;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        public UnwrapOptionTunnel()
        {
            Docking = BorderNodeDocking.Left;
        }

        /// <inheritdoc />
        public override BorderNodeRelationship Relationship => BorderNodeRelationship.AncestorToDescendant;

        /// <inheritdoc />
        public override BorderNodeMultiplicity Multiplicity => BorderNodeMultiplicity.OneToOne;

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
