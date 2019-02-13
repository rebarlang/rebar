namespace Rebar.Compiler.Nodes
{
    /// <summary>
    /// Interface for input <see cref="BorderNode"/>s that are associated with a
    /// <see cref="TerminateLifetimeTunnel"/>.
    /// </summary>
    internal interface IBeginLifetimeTunnel
    {
        /// <summary>
        /// The <see cref="TerminateLifetimeTunnel"/> that terminates the variable begun by this tunnel.
        /// </summary>
        TerminateLifetimeTunnel TerminateLifetimeTunnel { get; set; }
    }
}
