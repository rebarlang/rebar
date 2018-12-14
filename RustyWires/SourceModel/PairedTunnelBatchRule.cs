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
            var beginLifetimeTunnelBoundsChange = item.AsBoundsChange<IBeginLifetimeTunnel>();
            if (beginLifetimeTunnelBoundsChange.IsValid)
            {
                IBeginLifetimeTunnel beginLifetimeTunnel = beginLifetimeTunnelBoundsChange.TargetElement;
                beginLifetimeTunnel.TerminateLifetimeTunnel.Top = beginLifetimeTunnel.Top;
            }
        }
    }
}
