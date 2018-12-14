using System.Collections.Generic;
using System.Linq;
using NationalInstruments.Composition;
using NationalInstruments.Controls.Shell;
using NationalInstruments.Core;
using NationalInstruments.Design;
using NationalInstruments.Restricted;
using NationalInstruments.Shell;
using NationalInstruments.SourceModel;
using NationalInstruments.VI.Design;
using RustyWires.Common;
using RustyWires.SourceModel;

namespace RustyWires.Design
{
    public class BorrowTunnelViewModel : BorderNodeViewModel
    {
        public static readonly ICommandEx BorrowModeGroupCommand = new ShellRelayCommand()
        {
            UIType = UITypeForCommand.RadioGroup,
            LabelTitle = "Borrow Mode",
            UniqueId = "NI.RWDiagramNodeCommands:BorrowMode"
        };

        /// <summary>
        /// Command for setting a borrow tunnel to borrow immutably.
        /// </summary>
        public static readonly ICommandEx BorrowImmutableCommand = new ShellSelectionRelayCommand(HandleExecuteBorrowImmutableCommand, HandleCanExecuteBorrowImmutableCommand)
        {
            UniqueId = "NI.RWDiagramNodeCommands:BorrowTunnelImmutable",
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
            UniqueId = "NI.RWDiagramNodeCommands:BorrowTunnelMutable",
            UIType = UITypeForCommand.RadioButton,
            LabelTitle = "Mutable",
            SmallImageSource = VIDiagramNodeCommands.LoadVIResource("Designer/Resources/BlockDiagram/placeholder_16x16.PNG"),
            LargeImageSource = VIDiagramNodeCommands.LoadVIResource("Designer/Resources/BlockDiagram/placeholder_32x32.PNG"),
        };

        public BorrowTunnelViewModel(BorrowTunnel element) : base(element)
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
            selection.CheckAllBorrowModesMatch<BorrowTunnel>(parameter, BorrowMode.Immutable);
            return true;
        }

        private static void HandleExecuteBorrowImmutableCommand(ICommandParameter parameter, IEnumerable<IViewModel> selection, ICompositionHost host, DocumentEditSite site)
        {
            selection.GetBorrowTunnels<BorrowTunnel>().SetBorrowTunnelsMode(BorrowMode.Immutable);
        }

        private static bool HandleCanExecuteBorrowMutableCommand(ICommandParameter parameter, IEnumerable<IViewModel> selection, ICompositionHost host, DocumentEditSite site)
        {
            selection.CheckAllBorrowModesMatch<BorrowTunnel>(parameter, BorrowMode.Mutable);
            return true;
        }

        private static void HandleExecuteBorrowMutableCommand(ICommandParameter parameter, IEnumerable<IViewModel> selection, ICompositionHost host, DocumentEditSite site)
        {
            selection.GetBorrowTunnels<BorrowTunnel>().SetBorrowTunnelsMode(BorrowMode.Mutable);
        }
    }

    public static class FlatSequenceTunnelViewModelHelpers
    {
        /// <summary>
        /// Add Shift Register to a loop command
        /// </summary>
        public static readonly ICommandEx StructureAddBorrowTunnelCommand = new ShellSelectionRelayCommand(HandleAddFlatSequenceBorrowTunnel, HandleCanAddFlatSequenceBorrowTunnel)
        {
            UniqueId = "NI.RWDiagramNodeCommands:AddBorrowTunnelCommand",
            LabelTitle = "Add Borrow Tunnel",
            UIType = UITypeForCommand.Button,
            PopupMenuParent = MenuPathCommands.RootMenu,
            Weight = 0.1
        };

        private static bool HandleCanAddFlatSequenceBorrowTunnel(ICommandParameter parameter, IEnumerable<IViewModel> selection, ICompositionHost host, DocumentEditSite site)
        {
            return selection.OfType<RustyWiresFlatSequenceEditor>().Any();
        }

        private static void HandleAddFlatSequenceBorrowTunnel(object parameter, IEnumerable<IViewModel> selection, ICompositionHost host, DocumentEditSite site)
        {
            var structureViewModels = selection.OfType<RustyWiresFlatSequenceEditor>().WhereNotNull();
            if (structureViewModels.Any())
            {
                using (var transaction = structureViewModels.First().TransactionManager.BeginTransaction("Add Borrow Tunnels", TransactionPurpose.User))
                {
                    foreach (var structureViewModel in structureViewModels)
                    {
                        SMRect leftRect, rightRect;
                        BorderNodeViewModelHelpers.FindBorderNodePositions(structureViewModel, out leftRect, out rightRect);
                        Structure model = (Structure)structureViewModel.Model;

                        BorrowTunnel borrowTunnel = model.MakeTunnel<BorrowTunnel>(model.Diagram, model.NestedDiagrams.First());
                        FlatSequenceTerminateLifetimeTunnel flatSequenceTerminateLifetimeTunnel = model.MakeTunnel<FlatSequenceTerminateLifetimeTunnel>(model.Diagram, model.NestedDiagrams.First());
                        borrowTunnel.TerminateLifetimeTunnel = flatSequenceTerminateLifetimeTunnel;
                        flatSequenceTerminateLifetimeTunnel.BeginLifetimeTunnel = borrowTunnel;
                        // Set both as rules were not consistently picking right one to adjust to other.
                        borrowTunnel.Top = leftRect.Y;
                        borrowTunnel.Left = leftRect.X;
                        flatSequenceTerminateLifetimeTunnel.Top = borrowTunnel.Top;
                        flatSequenceTerminateLifetimeTunnel.Left = rightRect.X;
                    }
                    transaction.Commit();
                }
            }
        }

        /// <summary>
        /// Add Lock Tunnel to a Frame command
        /// </summary>
        public static readonly ICommandEx StructureAddLockTunnelCommand = new ShellSelectionRelayCommand(HandleAddLockTunnel, HandleCanAddLockTunnel)
        {
            UniqueId = "NI.RWDiagramNodeCommands:AddLockTunnelCommand",
            LabelTitle = "Add Lock Tunnel",
            UIType = UITypeForCommand.Button,
            PopupMenuParent = MenuPathCommands.RootMenu,
            Weight = 0.1
        };

        private static bool HandleCanAddLockTunnel(ICommandParameter parameter, IEnumerable<IViewModel> selection, ICompositionHost host, DocumentEditSite site)
        {
            return selection.OfType<StructureViewModel>().Any();
        }

        private static void HandleAddLockTunnel(object parameter, IEnumerable<IViewModel> selection, ICompositionHost host, DocumentEditSite site)
        {
            var structureViewModels = selection.OfType<StructureViewModel>().WhereNotNull();
            if (structureViewModels.Any())
            {
                using (var transaction = structureViewModels.First().TransactionManager.BeginTransaction("Add Lock Tunnels", TransactionPurpose.User))
                {
                    foreach (var structureViewModel in structureViewModels)
                    {
                        SMRect leftRect, rightRect;
                        BorderNodeViewModelHelpers.FindBorderNodePositions(structureViewModel, out leftRect, out rightRect);
                        Structure model = (Structure)structureViewModel.Model;

                        LockTunnel lockTunnel = model.MakeTunnel<LockTunnel>(model.Diagram, model.NestedDiagrams.First());
                        FlatSequenceTerminateLifetimeTunnel flatSequenceTerminateLifetimeTunnel = model.MakeTunnel<FlatSequenceTerminateLifetimeTunnel>(model.Diagram, model.NestedDiagrams.First());
                        lockTunnel.TerminateLifetimeTunnel = flatSequenceTerminateLifetimeTunnel;
                        flatSequenceTerminateLifetimeTunnel.BeginLifetimeTunnel = lockTunnel;
                        // Set both as rules were not consistently picking right one to adjust to other.
                        lockTunnel.Top = leftRect.Y;
                        lockTunnel.Left = leftRect.X;
                        flatSequenceTerminateLifetimeTunnel.Top = lockTunnel.Top;
                        flatSequenceTerminateLifetimeTunnel.Left = rightRect.X;
                    }
                    transaction.Commit();
                }
            }
        }

        /// <summary>
        /// Add Unwrap Option Tunnel to a FlatSequenceStructure command
        /// </summary>
        public static readonly ICommandEx StructureAddUnwrapOptionTunnelCommand = new ShellSelectionRelayCommand(HandleAddUnwrapOptionTunnel, HandleCanAddUnwrapOptionTunnel)
        {
            UniqueId = "NI.RWDiagramNodeCommands:AddUnwrapOptionTunnelCommand",
            LabelTitle = "Add Unwrap Option Tunnel",
            UIType = UITypeForCommand.Button,
            PopupMenuParent = MenuPathCommands.RootMenu,
            Weight = 0.1
        };

        private static bool HandleCanAddUnwrapOptionTunnel(ICommandParameter parameter, IEnumerable<IViewModel> selection, ICompositionHost host, DocumentEditSite site)
        {
            return selection.OfType<RustyWiresFlatSequenceEditor>().Any();
        }

        private static void HandleAddUnwrapOptionTunnel(object parameter, IEnumerable<IViewModel> selection, ICompositionHost host, DocumentEditSite site)
        {
            var flatSequenceStructureViewModels = selection.OfType<StructureViewModel>().WhereNotNull();
            if (flatSequenceStructureViewModels.Any())
            {
                using (var transaction = flatSequenceStructureViewModels.First().TransactionManager.BeginTransaction("Add Unwrap Option Tunnels", TransactionPurpose.User))
                {
                    foreach (var structureViewModel in flatSequenceStructureViewModels)
                    {
                        Structure model = (Structure)structureViewModel.Model;
                        SMRect leftRect, rightRect;
                        BorderNodeViewModelHelpers.FindBorderNodePositions(structureViewModel, out leftRect, out rightRect);
                        UnwrapOptionTunnel unwrapOptionTunnel = model.MakeTunnel<UnwrapOptionTunnel>(model.Diagram, model.NestedDiagrams.First());
                        unwrapOptionTunnel.Top = leftRect.Y;
                        unwrapOptionTunnel.Left = leftRect.X;
                    }
                    transaction.Commit();
                }
            }
        }
    }
}
