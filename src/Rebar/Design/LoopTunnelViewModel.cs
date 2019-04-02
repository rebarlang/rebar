using NationalInstruments.Core;
using NationalInstruments.Design;
using NationalInstruments.SourceModel;

namespace Rebar.Design
{
    /// <summary>
    /// View model for <see cref="Loop"/> border nodes.
    /// </summary>
    public class LoopBorderNodeViewModel : BorderNodeViewModel
    {
        public LoopBorderNodeViewModel(Tunnel loopTunnel, string foregroundUri) : base(loopTunnel)
        {
            ForegroundUri = new ResourceUri(this, foregroundUri);
        }

        /// <inheritoc />
        protected override ResourceUri ForegroundUri { get; }

        /// <inheritoc />
        public override NineGridData ForegroundImageData => new ViewModelImageData(this) { ImageUri = ForegroundUri };
    }
}
