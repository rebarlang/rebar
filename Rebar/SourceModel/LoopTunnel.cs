using System.Xml.Linq;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;

namespace Rebar.SourceModel
{
    /// <summary>
    /// Simple value-moving <see cref="Tunnel"/> for <see cref="Loop"/>.
    /// </summary>
    /// <remarks>Since the number of iterations the <see cref="Loop"/> will run is not generally known at compile-time,
    /// this tunnel can only pass shallow-copyable types.</remarks>
    public class LoopTunnel : SimpleTunnel
    {
        private const string ElementName = "LoopTunnel";

        [XmlParserFactoryMethod(ElementName, Function.ParsableNamespaceName)]
        public static LoopTunnel CreateLoopTunnel(IElementCreateInfo elementCreateInfo)
        {
            var loopTunnel = new LoopTunnel();
            loopTunnel.Init(elementCreateInfo);
            return loopTunnel;
        }

        /// <inheritdoc />
        public override XName XmlElementName => XName.Get(ElementName, Function.ParsableNamespaceName);

        /// <inheritdoc />
        public override BorderNodeMultiplicity Multiplicity => BorderNodeMultiplicity.OneToOne;
    }
}
