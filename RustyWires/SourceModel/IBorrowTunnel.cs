using RustyWires.Common;

namespace RustyWires.SourceModel
{
    /// <summary>
    /// Common interface for <see cref="Tunnel"/>s that allow borrowing a value on entering a structure.
    /// </summary>
    public interface IBorrowTunnel
    {
        /// <summary>
        /// The <see cref="BorrowMode"/> of the tunnel.
        /// </summary>
        BorrowMode BorrowMode { get; set; }
    }
}
