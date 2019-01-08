using System.Windows;
using NationalInstruments.Core;
using NationalInstruments.Design;
using RustyWires.SourceModel;

namespace RustyWires.Design
{
    /// <summary>
    /// View model for <see cref="Loop"/>.
    /// </summary>
    public class LoopViewModel : StructureViewModel
    {
        public LoopViewModel(Loop loop) : base(loop)
        {
        }

        /// <inheritdoc />
        /// <remarks>Copied from WhileLoopEditor. Since Loop can be arbitrarily sized, this tells its view how to
        /// slice up the associated NineGrid into nine parts that are applied to the total view.</remarks>
        public override NineGridData ForegroundImageData
        {
            get
            {
                var data = base.ForegroundImageData;
                data.Margin = new Thickness(0, -5, 0, -5);
                data.Slicing = new Thickness(34, 20, 34, 20);
                data.HorizontalAlignment = HorizontalAlignment.Stretch;
                data.VerticalAlignment = VerticalAlignment.Stretch;
                return data;
            }
        }

        /// <inheritdoc />
        public override void CreateCommandContent(ICommandPresentationContext context)
        {
            base.CreateCommandContent(context);
            using (context.AddConfigurationPaneContent())
            {
                using (context.AddGroup(ConfigurationPaneCommands.BehaviorGroupCommand))
                {
                    context.Add(LoopTunnelViewModelHelpers.LoopAddBorrowTunnelCommand.SetWeight(0.6));
                    context.Add(LoopTunnelViewModelHelpers.LoopAddIterateTunnelCommand.SetWeight(0.6));
                }
            }
        }
    }
}
