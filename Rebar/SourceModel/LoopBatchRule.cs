using System.Linq;
using NationalInstruments;
using NationalInstruments.Core;
using NationalInstruments.SourceModel;
using NationalInstruments.VI.SourceModel;

namespace Rebar.SourceModel
{
    /// <summary>
    /// <see cref="BatchRule"/> for updating <see cref="Loop"/> and its border nodes.
    /// </summary>
    /// <remarks>Adapted from NationalInstruments.VI.SourceModel.LoopBatchRule.</remarks>
    internal class LoopBatchRule : BatchRule
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
                HandleLoopBoundsChanges(context.MergeItems<BoundsChangeMerger<Loop>>());
            }
            ProcessTransactionItems = false;
        }

        private void HandleBorderNodeBoundsChanges(BoundsChangeMerger<BorderNode> boundsChanges)
        {
            foreach (var change in boundsChanges)
            {
                ViewElementOverlapHelper.PreventBorderNodeOverlap(change.TargetElement.Structure);
            }
            CoreBatchRule.HandleBorderNodeChanges<Loop>(boundsChanges);
        }

        private void HandleLoopBoundsChanges(BoundsChangeMerger<Loop> boundsChanges)
        {
            // Don't move things on structure move.
            foreach (var change in boundsChanges.Where(c => c.IsResize))
            {
                SMRect oldBounds = change.OldBounds;
                SMRect newBounds = change.NewBounds;
                var element = change.TargetElement;
                float leftDiff = newBounds.Left - oldBounds.Left;
                float topDiff = newBounds.Top - oldBounds.Top;
                foreach (BorderNode node in element.BorderNodes)
                {
                    if (BorderNode.GetBorderNodeDockingAxis(node.Docking) == BorderNodeDockingAxis.Horizontal)
                    {
                        node.Left -= leftDiff;
                    }
                    else
                    {
                        node.Top -= topDiff;
                    }
                }
                ViewElementOverlapHelper.PreventBorderNodeOverlap(element, ViewElementOverlapHelper.PreventBorderNodeOverlap);
                element.BorderNodes.ForEach(bn => bn.EnsureDocking());
            }
        }
    }
}
