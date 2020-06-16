using NationalInstruments.Core;
using NationalInstruments.Design;
using NationalInstruments.SourceModel;
using Rebar.SourceModel;

namespace Rebar.Design
{
    /// <summary>
    /// View model class for <see cref="VariantMatchStructureSelector"/>.
    /// </summary>
    internal sealed class VariantMatchStructureSelectorViewModel : BorderNodeViewModel
    {
        public VariantMatchStructureSelectorViewModel(VariantMatchStructureSelector selector, string foregroundUri)
            : base(selector)
        {
            ForegroundUri = new ResourceUri(this, foregroundUri);
        }

        /// <inheritoc />
        protected override ResourceUri ForegroundUri { get; }

        /// <inheritoc />
        public override NineGridData ForegroundImageData => new ViewModelImageData(this) { ImageUri = ForegroundUri };
    }
}
