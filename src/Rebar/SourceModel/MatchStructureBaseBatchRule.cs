using NationalInstruments.SourceModel;
using NationalInstruments.VI.SourceModel;

namespace Rebar.SourceModel
{
    internal class MatchStructureBaseBatchRule : BatchRule
    {
        /// <inheritdoc/>
        public override ModelBatchRuleExecuteLevels InitializeForTransaction(IRuleInitializeContext context)
        {
            return context.IsRootElementUnsetOrMatches<BlockDiagram>()
                ? ModelBatchRuleExecuteLevels.Intermediate
                : ModelBatchRuleExecuteLevels.None;
        }

        /// <inheritdoc/>
        protected override void OnBeginExecuteTransactionItems(IRuleExecuteContext context)
        {
            if (context.ShouldRunIntermediateRules)
            {
                HandleBorderNodeBoundsChanges(context.MergeItems<BoundsChangeMerger<BorderNode>>());
            }
            ProcessTransactionItems = false;
        }

        private void HandleBorderNodeBoundsChanges(BoundsChangeMerger<BorderNode> boundsChanges)
        {
            foreach (var change in boundsChanges)
            {
                ViewElementOverlapHelper.PreventBorderNodeOverlap(change.TargetElement.Structure);
            }
            CoreBatchRule.HandleBorderNodeChanges<MatchStructureBase>(boundsChanges);
        }
    }
}
