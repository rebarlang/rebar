using NationalInstruments.SourceModel;

namespace RustyWires.SourceModel
{
    /// <summary>
    /// Common interface for <see cref="Tunnel"/>s that terminate a lifetime on exiting a <see cref="Structure"/>, and are
    /// associated with an <see cref="IBeginLifetimeTunnel"/>.
    /// </summary>
    public interface ITerminateLifetimeTunnel : IViewElement
    {
        IBeginLifetimeTunnel BeginLifetimeTunnel { get; }
    }
}
