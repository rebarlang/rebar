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
using Rebar.SourceModel.TypeDiagram;

namespace Rebar.Design.TypeDiagram
{
    public class SelfTypeViewModel : GrowNodeViewModel
    {
        public SelfTypeViewModel(SelfType selfType) : base(selfType)
        {
        }

        /// <inheritdoc />
        public override void CreateCommandContent(ICommandPresentationContext context)
        {
            base.CreateCommandContent(context);
            using (context.AddConfigurationPaneContent())
            {
                using (context.AddGroup(TypeModeGroupCommand))
                {
                    context.Add(StructModeCommand);
                    context.Add(VariantModeCommand);
                }
            }
        }

        public static readonly ICommandEx TypeModeGroupCommand = new ShellRelayCommand()
        {
            UIType = UITypeForCommand.RadioGroup,
            LabelTitle = "Type Mode",
            UniqueId = "NI.TypeDiagramCommands:TypeMode"
        };

        /// <summary>
        /// Command for setting a type diagram to define a struct type.
        /// </summary>
        public static readonly ICommandEx StructModeCommand = new ShellSelectionRelayCommand(HandleExecuteStructCommand, HandleCanExecuteStructCommand)
        {
            UniqueId = "NI.TypeDiagramCommands:StructMode",
            UIType = UITypeForCommand.RadioButton,
            LabelTitle = "Struct",
            SmallImageSource = VIDiagramNodeCommands.LoadVIResource("Designer/Resources/BlockDiagram/placeholder_16x16.PNG"),
            LargeImageSource = VIDiagramNodeCommands.LoadVIResource("Designer/Resources/BlockDiagram/placeholder_32x32.PNG"),
        };

        /// <summary>
        /// Command for setting a borrow tunnel to borrow mutably.
        /// </summary>
        public static readonly ICommandEx VariantModeCommand = new ShellSelectionRelayCommand(HandleExecuteVariantCommand, HandleCanExecuteVariantCommand)
        {
            UniqueId = "NI.TypeDiagramCommands:VariantMode",
            UIType = UITypeForCommand.RadioButton,
            LabelTitle = "Variant",
            SmallImageSource = VIDiagramNodeCommands.LoadVIResource("Designer/Resources/BlockDiagram/placeholder_16x16.PNG"),
            LargeImageSource = VIDiagramNodeCommands.LoadVIResource("Designer/Resources/BlockDiagram/placeholder_32x32.PNG"),
        };

        private static SelfType FindSelfType(IEnumerable<IViewModel> selection)
        {
            return selection.First().Model as SelfType;
        }

        private static bool HandleCanExecuteStructCommand(ICommandParameter parameter, IEnumerable<IViewModel> selection, ICompositionHost host, DocumentEditSite site)
        {
            return UpdateParameterFromModeValue(parameter, selection, SelfTypeMode.Struct);
        }

        private static void HandleExecuteStructCommand(ICommandParameter parameter, IEnumerable<IViewModel> selection, ICompositionHost host, DocumentEditSite site)
        {
            SetTypeDiagramSelfMode(selection, SelfTypeMode.Struct);
        }

        private static bool HandleCanExecuteVariantCommand(ICommandParameter parameter, IEnumerable<IViewModel> selection, ICompositionHost host, DocumentEditSite site)
        {
            return UpdateParameterFromModeValue(parameter, selection, SelfTypeMode.Variant);
        }

        private static void HandleExecuteVariantCommand(ICommandParameter parameter, IEnumerable<IViewModel> selection, ICompositionHost host, DocumentEditSite site)
        {
            SetTypeDiagramSelfMode(selection, SelfTypeMode.Variant);
        }

        private static bool UpdateParameterFromModeValue(ICommandParameter parameter, IEnumerable<IViewModel> selection, SelfTypeMode mode)
        {
            var selfType = FindSelfType(selection);
            if (selfType == null)
            {
                return false;
            }

            ((ICheckableCommandParameter)parameter).IsChecked = selfType.Mode == mode;
            return mode != SelfTypeMode.Variant || RebarFeatureToggles.IsVariantTypesEnabled;
        }

        private static void SetTypeDiagramSelfMode(IEnumerable<IViewModel> selection, SelfTypeMode mode)
        {
            SelfType selfType = FindSelfType(selection);
            using (IActiveTransaction transaction = selfType.TransactionManager.BeginTransaction("Set SelfType mode", TransactionPurpose.User))
            {
                selfType.Mode = mode;
                transaction.Commit();
            }
        }
    }
}
