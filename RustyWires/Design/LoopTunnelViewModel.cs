using NationalInstruments.Design;
using NationalInstruments.SourceModel;

namespace RustyWires.Design
{
    /// <summary>
    /// View model for <see cref="LoopTunnel"/>.
    /// </summary>
    public class LoopTunnelViewModel : BorderNodeViewModel
    {
        public LoopTunnelViewModel(Tunnel loopTunnel) : base(loopTunnel)
        {
        }
    }
}
