using NationalInstruments.Core;
using NationalInstruments.Design;
using NationalInstruments.VI.Design;
using RustyWires.SourceModel;

namespace RustyWires.Design
{
    public class RustyWiresFlatSequenceEditor : FlatSequenceEditor
    {
        public RustyWiresFlatSequenceEditor(RustyWiresFlatSequence flatSequence)
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
                    context.Add(FlatSequenceTunnelViewModelHelpers.StructureAddLockTunnelCommand.SetWeight(0.6));
                    context.Add(FlatSequenceTunnelViewModelHelpers.StructureAddUnwrapOptionTunnelCommand.SetWeight(0.6));
                }
            }
        }
    }
}
