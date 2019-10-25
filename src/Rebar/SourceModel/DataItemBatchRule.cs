using System.Linq;
using NationalInstruments.CommonModel;
using NationalInstruments.MocCommon.SourceModel;
using NationalInstruments.SourceModel;
using NationalInstruments.VI.SourceModel;

namespace Rebar.SourceModel
{
    internal class DataItemBatchRule : BatchRule
    {
        /// <inheritdoc/>
        public override ModelBatchRuleExecuteLevels InitializeForTransaction(IRuleInitializeContext context)
        {
            return context.IsRootElementUnsetOrMatches<BlockDiagram>()
                ? ModelBatchRuleExecuteLevels.Intermediate
                : ModelBatchRuleExecuteLevels.None;
        }

        protected override void Execute(TransactionItem item, IRuleExecuteContext context)
        {
            var dataItemDirection = item.AsPropertyChange<DataItem, ParameterCallDirection>(nameof(DataItem.CallDirection));
            if (dataItemDirection.IsValid)
            {
                DataItem dataItem = dataItemDirection.TargetElement;
                if (dataItemDirection.NewValue != ParameterCallDirection.None && dataItem.CallIndex == -1)
                {
                    int callIndex = 0;
                    var function = (Function)dataItemDirection.TargetElement.Definition;
                    while (function.DataItems.Any(d => d.CallDirection != ParameterCallDirection.None && d.CallIndex == callIndex))
                    {
                        ++callIndex;
                    }
                    dataItem.CallIndex = callIndex;
                }
                else if (dataItemDirection.NewValue == ParameterCallDirection.None)
                {
                    dataItem.CallIndex = -1;
                }
            }
        }
    }
}
