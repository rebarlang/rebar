using NationalInstruments.SourceModel;

namespace Rebar.SourceModel
{
    /// <summary>
    /// Common interface for <see cref="Tunnel"/>s that represent the beginning of a lifetime inside a <see cref="Structure"/>,
    /// and are associated with an <see cref="ITerminateLifetimeTunnel"/>.
    /// </summary>
    public interface IBeginLifetimeTunnel : IViewElement
    {
        /// <summary>
        /// The <see cref="ITerminateLifetimeTunnel"/> associated with this tunnel.
        /// </summary>
        ITerminateLifetimeTunnel TerminateLifetimeTunnel { get; }
    }
}
