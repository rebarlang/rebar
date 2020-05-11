using NationalInstruments.Composition;
using NationalInstruments.Controls.Shell;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.Shell;

namespace Rebar.Design
{
    internal interface IFieldTerminalViewModel
    {
        void ChangeFieldName(string newFieldName);
    }

    internal static class FieldTerminalViewModelExtensions
    {
        private static readonly ICommandEx _changeFieldNameCommand = new ShellRelayCommand(HandleChangeFieldName, HandleCanChangeFieldName);

        private static void HandleChangeFieldName(ICommandParameter parameter, ICompositionHost host, DocumentEditSite site)
        {
            var fieldChangeRequest = (FieldChangeRequest)((ICommandParameter)parameter.Parameter).Parameter;
            fieldChangeRequest.TerminalViewModel.ChangeFieldName(fieldChangeRequest.FieldName);
        }

        private static bool HandleCanChangeFieldName(ICommandParameter parameter, ICompositionHost host, DocumentEditSite site) => true;

        public static ICommandEx CreateCommandFromFieldType(this IFieldTerminalViewModel terminalViewModel, NIType fieldType)
        {
            string fieldName = fieldType.GetName();
            return new ShellCommandInstance()
            {
                LabelTitle = fieldName,
                Weight = 1.0,
                PopupMenuParent = MenuPathCommands.RootMenu,
                Command = _changeFieldNameCommand,
                CommandParameter = new BaseCommandParameter() { Parameter = new FieldChangeRequest(terminalViewModel, fieldName) }
            };
        }

        private sealed class FieldChangeRequest
        {
            public FieldChangeRequest(IFieldTerminalViewModel terminalViewModel, string fieldName)
            {
                TerminalViewModel = terminalViewModel;
                FieldName = fieldName;
            }

            public IFieldTerminalViewModel TerminalViewModel { get; }

            public string FieldName { get; }
        }
    }
}
