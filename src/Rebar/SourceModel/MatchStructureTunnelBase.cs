using System.Collections.Generic;
using NationalInstruments.SourceModel;
using NationalInstruments.VI.SourceModel;

namespace Rebar.SourceModel
{
    public abstract class MatchStructureTunnelBase : StackedStructureTunnel
    {
        /// <inheritdoc />
        public override BorderNodeMultiplicity Multiplicity => BorderNodeMultiplicity.OneToMany;

        /// <inheritdoc />
        public override IEnumerable<Terminal> VisibleTerminals
        {
            get
            {
                yield return OuterTerminal;
                Terminal innerTerminal = GetPrimaryTerminal(((MatchStructureBase)Structure).SelectedDiagram);
                yield return innerTerminal;
            }
        }
    }
}
