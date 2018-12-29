using NationalInstruments.SourceModel;

namespace RustyWires.Design
{
    public class RustyWiresFlatSequenceSimpleBorderNodeViewModel : RustyWiresFlatSequenceBorderNodeViewModel
    {
        public RustyWiresFlatSequenceSimpleBorderNodeViewModel(BorderNode element, string foregroundUri) : base(element)
        {
            ForegroundUri = new ResourceUri(this, foregroundUri);
        }

        protected override ResourceUri ForegroundUri { get; }
    }
}
