using NationalInstruments.Core;
using NationalInstruments.Design;
using NationalInstruments.SourceModel;

namespace RustyWires.Design
{
    public abstract class RustyWiresFlatSequenceBorderNodeViewModel : BorderNodeViewModel
    {
        protected RustyWiresFlatSequenceBorderNodeViewModel(BorderNode element) : base(element)
        {
        }

        /// <inheritoc />
        public override NineGridData ForegroundImageData => new ViewModelImageData(this) { ImageUri = ForegroundUri };
    }
}
