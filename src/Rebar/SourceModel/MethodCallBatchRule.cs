using System.Collections.Generic;
using System.Linq;
using NationalInstruments.DataTypes;
using NationalInstruments.MocCommon.SourceModel;
using NationalInstruments.SourceModel;
using NationalInstruments.VI.SourceModel;

namespace Rebar.SourceModel
{
    /// <summary>
    /// <see cref="BatchRule"/> that removes error-type terminals from <see cref="MethodCall"/>s on insertion.
    /// </summary>
    internal class MethodCallBatchRule : BatchRule
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
                HandleBorderNodeBoundsChanges(context.MergeItems<OwnerChangeMerger<MocCommonMethodCall, Element>>());
            }
            ProcessTransactionItems = false;
        }

        private void HandleBorderNodeBoundsChanges(OwnerChangeMerger<MocCommonMethodCall, Element> methodCallChanges)
        {
            foreach (var change in methodCallChanges)
            {
                if (change.ChangeType == OwnerComponentChangeType.Insert)
                {
                    MocCommonMethodCall methodCall = change.Component;
                    List<Terminal> errorTerminals = methodCall.Terminals.Where(t => t.DataType.IsError()).ToList();
                    errorTerminals.ForEach(t => t.Delete());
                }
            }
        }
    }
}
