using System.Collections.Generic;
using System.Linq;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.Design;
using NationalInstruments.SourceModel;
using Rebar.SourceModel;

namespace Rebar.Design
{
    internal sealed class ConstructorTerminalViewModel : NodeTerminalViewModel, IFieldTerminalViewModel
    {
        public ConstructorTerminalViewModel(ConstructorTerminal terminal)
            : base(terminal)
        {
        }

        private ConstructorTerminal ConstructorTerminal => (ConstructorTerminal)Terminal;

        private Constructor Constructor => (Constructor)Terminal.Owner;

        /// <inheritdoc />
        public override string Name => ConstructorTerminal.FieldName ?? string.Empty;

        void IFieldTerminalViewModel.ChangeFieldName(string newFieldName)
        {
            using (var transaction = Terminal.TransactionManager.BeginTransaction("Set field", TransactionPurpose.User))
            {
                ConstructorTerminal.FieldName = newFieldName;
                transaction.Commit();
            }
        }

        public override IEnumerable<ICommandEx> SwitchCommands => CreateFieldSelectionCommands();

        private IEnumerable<ICommandEx> CreateFieldSelectionCommands()
        {
            IEnumerable<NIType> fieldTypes;
            NIType variantType = Constructor.Type;
            if (!variantType.IsUnion() || !(fieldTypes = variantType.GetFields()).Any())
            {
                return Enumerable.Empty<ICommandEx>();
            }
            return fieldTypes.Select(this.CreateCommandFromFieldType).ToList();
        }
    }
}
