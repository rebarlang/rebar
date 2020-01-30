using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NationalInstruments;
using NationalInstruments.Composition;
using NationalInstruments.Controls.Shell;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.Design;
using NationalInstruments.Shell;
using NationalInstruments.SourceModel;
using Rebar.SourceModel;

namespace Rebar.Design
{
    public class StructFieldAccessorTerminalViewModel : NodeTerminalViewModel
    {
        private static readonly ICommandEx _changeFieldNameCommand = new ShellRelayCommand(HandleChangeFieldName, HandleCanChangeFieldName);

        public StructFieldAccessorTerminalViewModel(NodeTerminal terminal)
            : base(terminal)
        {
        }

        /// <inheritdoc />
        public override string Name => StructFieldAccessorTerminal.FieldName ?? string.Empty;

        private StructFieldAccessor StructFieldAccessor => (StructFieldAccessor)Terminal.Owner;

        private StructFieldAccessorTerminal StructFieldAccessorTerminal => (StructFieldAccessorTerminal)Terminal;

        private void ChangeFieldName(string newFieldName)
        {
            using (var transaction = StructFieldAccessorTerminal.TransactionManager.BeginTransaction("Set field", TransactionPurpose.User))
            {
                StructFieldAccessorTerminal.FieldName = newFieldName;
                transaction.Commit();
            }
        }

        private static void HandleChangeFieldName(ICommandParameter parameter, ICompositionHost host, DocumentEditSite site)
        {
            var fieldChangeRequest = (FieldChangeRequest)((ICommandParameter)parameter.Parameter).Parameter;
            fieldChangeRequest.TerminalViewModel.ChangeFieldName(fieldChangeRequest.FieldName);
        }

        private static bool HandleCanChangeFieldName(ICommandParameter parameter, ICompositionHost host, DocumentEditSite site)
        {
            return true;
        }

        public override IEnumerable<ICommandEx> SwitchCommands => CreateFieldSelectionCommands();

        private IEnumerable<ICommandEx> CreateFieldSelectionCommands()
        {
            IEnumerable<NIType> fieldTypes;
            NIType structType = StructFieldAccessor.StructType;
            if (!structType.IsValueClass() || !(fieldTypes = structType.GetFields()).Any())
            {
                return Enumerable.Empty<ICommandEx>();
            }
            return fieldTypes.Select(CreateCommandFromFieldType).ToList();
        }

        private ICommandEx CreateCommandFromFieldType(NIType fieldType)
        {
            string fieldName = fieldType.GetName();
            return new ShellCommandInstance()
            {
                LabelTitle = fieldName,
                Weight = 1.0,
                PopupMenuParent = MenuPathCommands.RootMenu,
                Command = _changeFieldNameCommand,
                CommandParameter = new BaseCommandParameter() { Parameter = new FieldChangeRequest(this, fieldName) }
            };
        }

        private sealed class FieldChangeRequest
        {
            public FieldChangeRequest(StructFieldAccessorTerminalViewModel terminalViewModel, string fieldName)
            {
                TerminalViewModel = terminalViewModel;
                FieldName = fieldName;
            }

            public StructFieldAccessorTerminalViewModel TerminalViewModel { get; }

            public string FieldName { get; }
        }
    }
}
