using System.Collections.Generic;
using System.Linq;
using NationalInstruments.Composition;
using NationalInstruments.Controls.Shell;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.Design;
using NationalInstruments.Shell;
using NationalInstruments.SourceModel;
using NationalInstruments.VI.Design;
using Rebar.Common;
using Rebar.Compiler;
using Rebar.SourceModel;

namespace Rebar.Design
{
    /// <summary>
    /// Rebar-specific implementation of <see cref="WireViewModel"/> for providing custom wire visuals.
    /// </summary>
    public class FunctionWireViewModel : WireViewModel
    {
        /// <summary>
        /// Construct a new <see cref="FunctionWireViewModel"/>.
        /// </summary>
        /// <param name="wire">The wire to create a view model for.</param>
        public FunctionWireViewModel(Wire wire) : base(wire)
        {
        }

        #region Commands

        /// <inheritdoc />
        public override void CreateCommandContent(ICommandPresentationContext context)
        {
            base.CreateCommandContent(context);
            using (context.AddConfigurationPaneContent())
            {
                using (context.AddGroup(WireGroupCommand))
                {
                    context.Add(WireBeginsMutableVariableCommand);
                }
            }
        }

        private static readonly ICommandEx WireGroupCommand = new ShellRelayCommand()
        {
            UIType = UITypeForCommand.Group,
            LabelTitle = "Wire",
            UniqueId = "NI.RebarDiagramNodeCommands:TerminalsGroup"
        };

        private static readonly ICommandEx WireBeginsMutableVariableCommand = new ShellSelectionRelayCommand(
            HandleExecuteWireBeginsMutableVariableCommand,
            HandleCanExecuteWireBeginsMutableVariableCommand)
        {
            UIType = UITypeForCommand.CheckBox,
            LabelTitle = "Wire Variable Is Mutable",
            UniqueId = "NI.RebarDiagramNodeCommands:MutableWireVariable",
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
                checkableCommandParameter.IsChecked = selectedWires.Any(wire => wire.GetWireVariable().Mutable);
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

        #endregion

        #region Wire visuals

        /// <inheritdoc />
        public override WireRenderInfoEnumerable WireRenderInfo
        {
            get
            {
                VariableReference variable = ((Wire)Model).GetWireVariable();
                if (!variable.IsValid)
                {
                    return base.WireRenderInfo;
                }
                var stockResources = Host.GetSharedExportedValue<StockDiagramUIResources>();
                if (RebarFeatureToggles.IsVisualizeVariableIdentityEnabled)
                {
                    return CreateVariableIdentityRenderInfo(variable, stockResources);
                }

                if (!variable.Type.IsRebarReferenceType())
                {
                    ITypeAssetProvider innerTypeAssetProvider = stockResources.GetTypeAssets((Element)null, variable.Type);
                    ITypeAssetProvider outerAssetProvider = variable.Mutable
                        ? (ITypeAssetProvider)new MutableValueTypeAssetProvider(innerTypeAssetProvider, 0)
                        : new ImmutableValueTypeAssetProvider(innerTypeAssetProvider, 0);
                    return outerAssetProvider.GetWireRenderInfo(0);
                }
                return base.WireRenderInfo;
            }
        }

        private static WireRenderInfoEnumerable CreateVariableIdentityRenderInfo(VariableReference variable, StockDiagramUIResources stockResources)
        {
            NIType innerType = variable.Type.IsRebarReferenceType()
                ? variable.Type.GetUnderlyingTypeFromRebarType()
                : variable.Type;
            int id = variable.Id;
            ITypeAssetProvider innerTypeAssetProvider = new VariableIdentityTypeAssetProvider(
                stockResources.GetTypeAssets((Element)null, variable.Type).GetShortName(innerType),
                VariableIdentityTypeAssetProvider.GetColor(id));
            ITypeAssetProvider outerTypeAssetProvider;
            if (variable.Type.IsMutableReferenceType())
            {
                outerTypeAssetProvider = new MutableReferenceTypeAssetProvider(
                    innerTypeAssetProvider,
                    0);
            }
            else if (variable.Type.IsImmutableReferenceType())
            {
                outerTypeAssetProvider = new ImmutableReferenceTypeAssetProvider(
                    innerTypeAssetProvider,
                    0);
            }
            else if (variable.Mutable)
            {
                outerTypeAssetProvider = new MutableValueTypeAssetProvider(
                    innerTypeAssetProvider,
                    0);
            }
            else
            {
                outerTypeAssetProvider = new ImmutableValueTypeAssetProvider(
                    innerTypeAssetProvider,
                    0);
            }
            return outerTypeAssetProvider.GetWireRenderInfo(0);
        }

        #endregion
    }
}
