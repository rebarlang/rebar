using NationalInstruments.SourceModel;
using NationalInstruments.VI.SourceModel;

namespace Rebar.SourceModel
{
    public abstract class MatchStructureTunnelBase : StackedStructureTunnel
    {
        /// <inheritdoc />
        public override BorderNodeMultiplicity Multiplicity => BorderNodeMultiplicity.OneToMany;
    }
}
