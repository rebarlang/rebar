using NationalInstruments.Shell;
using System.Collections.Generic;
using System.Linq;
using NationalInstruments.Core;
using NationalInstruments.Controls.Shell;
using NationalInstruments.Composition;
using NationalInstruments.VI.Design;
using NationalInstruments.Design;
using NationalInstruments.SourceModel;
using RustyWires.SourceModel;

namespace RustyWires.Design
{
    public class WireMutabilityViewModelService : IProvideCommandContent
    {
        public static readonly ICommandEx WireGroupCommand = new ShellRelayCommand()
        {
            UIType = UITypeForCommand.Group,
            LabelTitle = "Wire",
            UniqueId = "NI.RWDiagramNodeCommands:TerminalsGroup"
        };

        public static readonly ICommandEx WireBeginsMutableVariableCommand = new ShellSelectionRelayCommand(
            HandleExecuteWireBeginsMutableVariableCommand,
            HandleCanExecuteWireBeginsMutableVariableCommand)
        {
            UIType = UITypeForCommand.CheckBox,
            LabelTitle = "Wire Variable Is Mutable",
            UniqueId = "NI.RWDiagramNodeCommands:MutableWireVariable",
            SmallImageSource = VIDiagramNodeCommands.LoadVIResource("Designer/Resources/BlockDiagram/placeholder_16x16.PNG"),
            LargeImageSource = VIDiagramNodeCommands.LoadVIResource("Designer/Resources/BlockDiagram/placeholder_32x32.PNG"),
        };

        private static bool HandleCanExecuteWireBeginsMutableVariableCommand(ICommandParameter parameter, IEnumerable<IViewModel> selection, ICompositionHost host, DocumentEditSite site)
        {
            IEnumerable<Wire> selectedWires = selection.GetSelectedModels<Wire>();
            if (!selectedWires.Any())
            {
                return true;
            }
            var checkableCommandParameter = (ICheckableCommandParameter)parameter;
            if (selectedWires.All(wire => wire.GetIsFirstVariableWire()))
            {
                bool firstSetting = selectedWires.First().GetWireBeginsMutableVariable();
                bool multipleSettings = selectedWires.Any(wire => wire.GetWireBeginsMutableVariable() != firstSetting);
                checkableCommandParameter.IsChecked = multipleSettings ? null : (bool?)firstSetting;
                return true;
            }
            else
            {
                checkableCommandParameter.IsChecked = selectedWires.Any(wire => wire.GetWireBeginsMutableVariable());
                return false;
            }
        }

        private static void HandleExecuteWireBeginsMutableVariableCommand(ICommandParameter parameter, IEnumerable<IViewModel> selection, ICompositionHost host, DocumentEditSite site)
        {
            IEnumerable<Wire> selectedWires = selection.GetSelectedModels<Wire>();
            if (!selectedWires.Any())
            {
                return;
            }
            using (var transaction = selectedWires.First().TransactionManager.BeginTransaction("Set Mutable Terminal Bindings mode", TransactionPurpose.User))
            {
                bool value = ((ICheckableCommandParameter)parameter).IsChecked ?? false;
                foreach (Wire wire in selectedWires)
                {
                    wire.SetWireBeginsMutableVariable(value);
                }
                transaction.Commit();
            }
        }

        public string EditingContext => GetType().FullName;

        public void CreateCommandContent(ICommandPresentationContext context)
        {
            using (context.AddConfigurationPaneContent())
            {
                using (context.AddGroup(WireGroupCommand))
                {
                    context.Add(WireBeginsMutableVariableCommand);
                }
            }
        }

        public bool GetHandled<T>() => false;

        public double GetWeight<T>() => 0.0;
    }
}
