using NationalInstruments.SourceModel;

namespace Rebar.Design
{
    public class FlatSequenceSimpleBorderNodeViewModel : FlatSequenceBorderNodeViewModel
    {
        public FlatSequenceSimpleBorderNodeViewModel(BorderNode element, string foregroundUri) : base(element)
        {
            ForegroundUri = new ResourceUri(this, foregroundUri);
        }

        protected override ResourceUri ForegroundUri { get; }
    }
}
