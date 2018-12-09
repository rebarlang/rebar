using NationalInstruments.SourceModel;
using NationalInstruments.VI.SourceModel;

namespace RustyWires.SourceModel
{
    /// <summary>
    /// <see cref="BatchRule"/> that ensures pairs of <see cref="Tunnel"/>s stay horizontally aligned with each other.
    /// </summary>
    public sealed class PairedTunnelBatchRule : BatchRule
    {
        /// <inheritdoc />
        public override ModelBatchRuleExecuteLevels InitializeForTransaction(IRuleInitializeContext context)
        {
            return context.IsRootElementUnsetOrMatches<BlockDiagram>()
                ? ModelBatchRuleExecuteLevels.Intermediate
                : ModelBatchRuleExecuteLevels.None;
        }

        /// <inheritdoc />
        protected override void Execute(TransactionItem item, IRuleExecuteContext context)
        {
            var borrowTunnelBoundsChange = item.AsBoundsChange<BorrowTunnel>();
            if (borrowTunnelBoundsChange.IsValid)
            {
                BorrowTunnel borrowTunnel = borrowTunnelBoundsChange.TargetElement;
                borrowTunnel.TerminateScopeTunnel.Top = borrowTunnel.Top;
            }

            var lockTunnelBoundsChange = item.AsBoundsChange<LockTunnel>();
            if (lockTunnelBoundsChange.IsValid)
            {
                LockTunnel lockTunnel = lockTunnelBoundsChange.TargetElement;
                lockTunnel.TerminateScopeTunnel.Top = lockTunnel.Top;
            }
        }
    }
}
