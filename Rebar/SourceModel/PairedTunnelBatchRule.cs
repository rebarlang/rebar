using NationalInstruments.SourceModel;
using NationalInstruments.VI.SourceModel;

namespace Rebar.SourceModel
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
            var removedBorderNode = item.AsComponentRemove<BorderNode, Structure>();
            if (removedBorderNode != null)
            {
                Element toDelete = null;
                var beginLifetimeTunnel = removedBorderNode as IBeginLifetimeTunnel;
                var terminateLifetimeTunnel = removedBorderNode as ITerminateLifetimeTunnel;
                if (beginLifetimeTunnel != null)
                {
                    toDelete = (Element)beginLifetimeTunnel.TerminateLifetimeTunnel;
                }
                else if (terminateLifetimeTunnel != null)
                {
                    toDelete = (Element)terminateLifetimeTunnel.BeginLifetimeTunnel;
                }
                toDelete?.Delete();
            }
        }
    }
}
