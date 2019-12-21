using System.Collections.Generic;
using System.Linq;
using NationalInstruments.Composition;
using NationalInstruments.Controls.Shell;
using NationalInstruments.Core;
using NationalInstruments.Design;
using NationalInstruments.Shell;
using NationalInstruments.SourceModel;
using NationalInstruments.VI.Design;
using Rebar.Common;
using Rebar.SourceModel;

namespace Rebar.Design
{
    public class BorrowNodeViewModel : BasicNodeViewModel
    {
        public static readonly ICommandEx BorrowModeGroupCommand = new ShellRelayCommand()
        {
            UIType = UITypeForCommand.RadioGroup,
            LabelTitle = "Borrow Mode",
            UniqueId = "NI.RebarDiagramNodeCommands:BorrowNodeBorrowMode"
        };

        /// <summary>
        /// Command for setting a borrow tunnel to borrow immutably.
        /// </summary>
        public static readonly ICommandEx BorrowImmutableCommand = new ShellSelectionRelayCommand(HandleExecuteBorrowImmutableCommand, HandleCanExecuteBorrowImmutableCommand)
        {
            UniqueId = "NI.RebarDiagramNodeCommands:BorrowTunnelImmutable",
            UIType = UITypeForCommand.RadioButton,
            LabelTitle = "Immutable",
            SmallImageSource = VIDiagramNodeCommands.LoadVIResource("Designer/Resources/BlockDiagram/placeholder_16x16.PNG"),
            LargeImageSource = VIDiagramNodeCommands.LoadVIResource("Designer/Resources/BlockDiagram/placeholder_32x32.PNG"),
        };

        /// <summary>
        /// Command for setting a borrow tunnel to borrow mutably.
        /// </summary>
        public static readonly ICommandEx BorrowMutableCommand = new ShellSelectionRelayCommand(HandleExecuteBorrowMutableCommand, HandleCanExecuteBorrowMutableCommand)
        {
            UniqueId = "NI.RebarDiagramNodeCommands:BorrowTunnelMutable",
            UIType = UITypeForCommand.RadioButton,
            LabelTitle = "Mutable",
            SmallImageSource = VIDiagramNodeCommands.LoadVIResource("Designer/Resources/BlockDiagram/placeholder_16x16.PNG"),
            LargeImageSource = VIDiagramNodeCommands.LoadVIResource("Designer/Resources/BlockDiagram/placeholder_32x32.PNG"),
        };

        public BorrowNodeViewModel(ImmutableBorrowNode borrowNode)
            : base(borrowNode, "Borrow", @"Resources\Diagram\Nodes\ImmutableBorrowNode.png")
        {
        }

        /// <inheritdoc />
        public override void CreateCommandContent(ICommandPresentationContext context)
        {
            base.CreateCommandContent(context);
            using (context.AddConfigurationPaneContent())
            {
                using (context.AddGroup(BorrowModeGroupCommand))
                {
                    context.Add(BorrowImmutableCommand);
                    context.Add(BorrowMutableCommand);
                }
            }
        }

        private static bool HandleCanExecuteBorrowImmutableCommand(ICommandParameter parameter, IEnumerable<IViewModel> selection, ICompositionHost host, DocumentEditSite site)
        {
            selection.CheckAllBorrowModesMatch<ImmutableBorrowNode>(parameter, BorrowMode.Immutable);
            return true;
        }

        private static void HandleExecuteBorrowImmutableCommand(ICommandParameter parameter, IEnumerable<IViewModel> selection, ICompositionHost host, DocumentEditSite site)
        {
            selection.GetBorrowTunnels<ImmutableBorrowNode>().SetBorrowTunnelsMode(BorrowMode.Immutable);
        }

        private static bool HandleCanExecuteBorrowMutableCommand(ICommandParameter parameter, IEnumerable<IViewModel> selection, ICompositionHost host, DocumentEditSite site)
        {
            selection.CheckAllBorrowModesMatch<ImmutableBorrowNode>(parameter, BorrowMode.Mutable);
            return true;
        }

        private static void HandleExecuteBorrowMutableCommand(ICommandParameter parameter, IEnumerable<IViewModel> selection, ICompositionHost host, DocumentEditSite site)
        {
            selection.GetBorrowTunnels<ImmutableBorrowNode>().SetBorrowTunnelsMode(BorrowMode.Mutable);
        }
    }
}
