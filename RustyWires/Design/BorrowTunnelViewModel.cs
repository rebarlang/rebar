using System;
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
            IEnumerable<BorrowTunnel> borrowTunnels = GetBorrowTunnels(selection);
            BorrowMode firstMode = borrowTunnels.First().BorrowMode;
            bool multipleModes = borrowTunnels.Any(bt => bt.BorrowMode != firstMode);
            ((ICheckableCommandParameter)parameter).IsChecked = firstMode == BorrowMode.Immutable && !multipleModes;
            return true;
        }

        private static void HandleExecuteBorrowImmutableCommand(ICommandParameter parameter, IEnumerable<IViewModel> selection, ICompositionHost host, DocumentEditSite site)
        {
            SetBorrowTunnelsMode(GetBorrowTunnels(selection), BorrowMode.Immutable);
        }

        private static bool HandleCanExecuteBorrowMutableCommand(ICommandParameter parameter, IEnumerable<IViewModel> selection, ICompositionHost host, DocumentEditSite site)
        {
            IEnumerable<BorrowTunnel> borrowTunnels = GetBorrowTunnels(selection);
            BorrowMode firstMode = borrowTunnels.First().BorrowMode;
            bool multipleModes = borrowTunnels.Any(bt => bt.BorrowMode != firstMode);
            ((ICheckableCommandParameter)parameter).IsChecked = firstMode == BorrowMode.Mutable && !multipleModes;
            return true;
        }

        private static void HandleExecuteBorrowMutableCommand(ICommandParameter parameter, IEnumerable<IViewModel> selection, ICompositionHost host, DocumentEditSite site)
        {
            SetBorrowTunnelsMode(GetBorrowTunnels(selection), BorrowMode.Mutable);
        }

        private static IEnumerable<BorrowTunnel> GetBorrowTunnels(IEnumerable<IViewModel> selection)
        {
            return selection.Select(viewModel => viewModel.Model).OfType<BorrowTunnel>();
        }

        private static void SetBorrowTunnelsMode(IEnumerable<BorrowTunnel> borrowTunnels, BorrowMode borrowMode)
        {
            if (borrowTunnels.Any())
            {
                using (IActiveTransaction transaction = borrowTunnels.First().TransactionManager.BeginTransaction("Set BorrowTunnel BorrowMode", TransactionPurpose.User))
                {
                    foreach (BorrowTunnel borrowTunnel in borrowTunnels)
                    {
                        borrowTunnel.BorrowMode = borrowMode;
                    }
                    transaction.Commit();
                }
            }
        }
    }

    public static class BorrowTunnelViewModelHelpers
    {
        /// <summary>
        /// Add Shift Register to a loop command
        /// </summary>
        public static readonly ICommandEx StructureAddBorrowTunnelCommand = new ShellSelectionRelayCommand(HandleAddBorrowTunnel, HandleCanAddBorrowTunnel)
        {
            UniqueId = "NI.RWDiagramNodeCommands:AddBorrowTunnelCommand",
            LabelTitle = "Add Borrow Tunnel",
            UIType = UITypeForCommand.Button,
            PopupMenuParent = MenuPathCommands.RootMenu,
            Weight = 0.1
        };

        private static bool HandleCanAddBorrowTunnel(ICommandParameter parameter, IEnumerable<IViewModel> selection, ICompositionHost host, DocumentEditSite site)
        {
            return selection.OfType<StructureViewModel>().Any();
        }

        private static void HandleAddBorrowTunnel(object parameter, IEnumerable<IViewModel> selection, ICompositionHost host, DocumentEditSite site)
        {
            var structureViewModels = selection.OfType<StructureViewModel>().WhereNotNull();
            if (structureViewModels.Any())
            {
                using (var transaction = structureViewModels.First().TransactionManager.BeginTransaction("Add Borrow Tunnels", TransactionPurpose.User))
                {
                    foreach (var structureViewModel in structureViewModels)
                    {
                        var view = structureViewModel.View;
                        var contextMenuInfo = parameter.QueryService<ContextMenuInfo>().FirstOrDefault();
                        double top = 0;
                        if (contextMenuInfo != null && !view.IsEmpty)
                        {
                            top = contextMenuInfo.ClickPosition.GetPosition(view).Y;
                        }

                        Structure model = (Structure)structureViewModel.Model;
                        top -= (top - model.OuterBorderThickness.Top) % StockDiagramGeometries.GridSize;
                        top = Math.Max(top, model.OuterBorderThickness.Top);
                        top = Math.Min(top, model.Height - model.OuterBorderThickness.Bottom - StockDiagramGeometries.StandardTunnelHeight);
                        SMRect leftRect = new SMRect(-StockDiagramGeometries.StandardTunnelOffsetForStructures, top, StockDiagramGeometries.StandardTunnelWidth,
                            StockDiagramGeometries.StandardTunnelHeight);
                        SMRect rightRect = new SMRect(model.Width - StockDiagramGeometries.StandardTunnelWidth + StockDiagramGeometries.StandardTunnelOffsetForStructures, top,
                            StockDiagramGeometries.StandardTerminalWidth, StockDiagramGeometries.StandardTunnelHeight);
                        while (
                            model.BorderNodes.Any(
                                node => node.Bounds.Overlaps(leftRect) || node.Bounds.Overlaps(rightRect)))
                        {
                            leftRect.Y += StockDiagramGeometries.GridSize;
                            rightRect.Y += StockDiagramGeometries.GridSize;
                        }
                        // If we ran out of room looking for a place to put Shift Register, we need to grow our Loop
                        if (leftRect.Bottom > model.Height - model.OuterBorderThickness.Bottom)
                        {
                            model.Height = leftRect.Bottom + StockDiagramGeometries.StandardTunnelHeight;
                        }

                        BorrowTunnel borrowTunnel = model.MakeTunnel<BorrowTunnel>(model.Diagram, model.NestedDiagrams.First());
                        UnborrowTunnel unborrowTunnel = model.MakeTunnel<UnborrowTunnel>(model.Diagram, model.NestedDiagrams.First());
                        borrowTunnel.UnborrowTunnel = unborrowTunnel;
                        unborrowTunnel.BorrowTunnel = borrowTunnel;
                        // Set both as rules were not consistently picking right one to adjust to other.
                        borrowTunnel.Top = leftRect.Y;
                        borrowTunnel.Left = leftRect.X;
                        unborrowTunnel.Top = borrowTunnel.Top;
                        unborrowTunnel.Left = rightRect.X;
                    }
                    transaction.Commit();
                }
            }
        }

        /// <summary>
        /// Add Lock Tunnel to a Frame command
        /// </summary>
        public static readonly ICommandEx StructureAddLockTunnelCommand = new ShellSelectionRelayCommand(HandleAddLockTunnel, HandleCanAddLockunnel)
        {
            UniqueId = "NI.RWDiagramNodeCommands:AddLockTunnelCommand",
            LabelTitle = "Add Lock Tunnel",
            UIType = UITypeForCommand.Button,
            PopupMenuParent = MenuPathCommands.RootMenu,
            Weight = 0.1
        };

        private static bool HandleCanAddLockunnel(ICommandParameter parameter, IEnumerable<IViewModel> selection, ICompositionHost host, DocumentEditSite site)
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
                        var view = structureViewModel.View;
                        var contextMenuInfo = parameter.QueryService<ContextMenuInfo>().FirstOrDefault();
                        double top = 0;
                        if (contextMenuInfo != null && !view.IsEmpty)
                        {
                            top = contextMenuInfo.ClickPosition.GetPosition(view).Y;
                        }

                        Structure model = (Structure)structureViewModel.Model;
                        top -= (top - model.OuterBorderThickness.Top) % StockDiagramGeometries.GridSize;
                        top = Math.Max(top, model.OuterBorderThickness.Top);
                        top = Math.Min(top, model.Height - model.OuterBorderThickness.Bottom - StockDiagramGeometries.StandardTunnelHeight);
                        SMRect leftRect = new SMRect(-StockDiagramGeometries.StandardTunnelOffsetForStructures, top, StockDiagramGeometries.StandardTunnelWidth,
                            StockDiagramGeometries.StandardTunnelHeight);
                        SMRect rightRect = new SMRect(model.Width - StockDiagramGeometries.StandardTunnelWidth + StockDiagramGeometries.StandardTunnelOffsetForStructures, top,
                            StockDiagramGeometries.StandardTerminalWidth, StockDiagramGeometries.StandardTunnelHeight);
                        while (
                            model.BorderNodes.Any(
                                node => node.Bounds.Overlaps(leftRect) || node.Bounds.Overlaps(rightRect)))
                        {
                            leftRect.Y += StockDiagramGeometries.GridSize;
                            rightRect.Y += StockDiagramGeometries.GridSize;
                        }
                        // If we ran out of room looking for a place to put Shift Register, we need to grow our Loop
                        if (leftRect.Bottom > model.Height - model.OuterBorderThickness.Bottom)
                        {
                            model.Height = leftRect.Bottom + StockDiagramGeometries.StandardTunnelHeight;
                        }

                        LockTunnel lockTunnel = model.MakeTunnel<LockTunnel>(model.Diagram, model.NestedDiagrams.First());
                        UnlockTunnel unlockTunnel = model.MakeTunnel<UnlockTunnel>(model.Diagram, model.NestedDiagrams.First());
                        lockTunnel.UnlockTunnel = unlockTunnel;
                        unlockTunnel.LockTunnel = lockTunnel;
                        // Set both as rules were not consistently picking right one to adjust to other.
                        lockTunnel.Top = leftRect.Y;
                        lockTunnel.Left = leftRect.X;
                        unlockTunnel.Top = lockTunnel.Top;
                        unlockTunnel.Left = rightRect.X;
                    }
                    transaction.Commit();
                }
            }
        }
    }
}
