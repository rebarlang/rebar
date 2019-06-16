using NationalInstruments.Core;
using NationalInstruments.Design;
using Rebar.SourceModel;

namespace Rebar.Design
{
    public class FlatSequenceEditor : NationalInstruments.VI.Design.FlatSequenceEditor
    {
        public FlatSequenceEditor(FlatSequence flatSequence)
            : base(flatSequence)
        {
        }

        /// <inheritdoc />
        public override void CreateCommandContent(ICommandPresentationContext context)
        {
            base.CreateCommandContent(context);
            using (context.AddConfigurationPaneContent())
            {
                using (context.AddGroup(ConfigurationPaneCommands.BehaviorGroupCommand))
                {
                    context.Add(FlatSequenceTunnelViewModelHelpers.StructureAddBorrowTunnelCommand.SetWeight(0.6));
                    if (RebarFeatureToggles.IsCellDataTypeEnabled)
                    {
                        context.Add(FlatSequenceTunnelViewModelHelpers.StructureAddLockTunnelCommand.SetWeight(0.6));
                    }
                    context.Add(FlatSequenceTunnelViewModelHelpers.StructureAddUnwrapOptionTunnelCommand.SetWeight(0.6));
                }
            }
        }
    }
}
