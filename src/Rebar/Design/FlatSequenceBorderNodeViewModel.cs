using NationalInstruments.Core;
using NationalInstruments.Design;
using NationalInstruments.SourceModel;

namespace Rebar.Design
{
    public abstract class FlatSequenceBorderNodeViewModel : BorderNodeViewModel
    {
        protected FlatSequenceBorderNodeViewModel(BorderNode element) : base(element)
        {
        }

        /// <inheritoc />
        public override NineGridData ForegroundImageData => new ViewModelImageData(this) { ImageUri = ForegroundUri };
    }
}
