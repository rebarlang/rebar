using System.Collections.Generic;
using System.Linq;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.Design;
using NationalInstruments.SourceModel;
using Rebar.SourceModel;

namespace Rebar.Design
{
    public class StructFieldAccessorTerminalViewModel : NodeTerminalViewModel, IFieldTerminalViewModel
    {
        public StructFieldAccessorTerminalViewModel(NodeTerminal terminal)
            : base(terminal)
        {
        }

        /// <inheritdoc />
        public override string Name => StructFieldAccessorTerminal.FieldName ?? string.Empty;

        private StructFieldAccessor StructFieldAccessor => (StructFieldAccessor)Terminal.Owner;

        private StructFieldAccessorTerminal StructFieldAccessorTerminal => (StructFieldAccessorTerminal)Terminal;

        void IFieldTerminalViewModel.ChangeFieldName(string newFieldName)
        {
            using (var transaction = StructFieldAccessorTerminal.TransactionManager.BeginTransaction("Set field", TransactionPurpose.User))
            {
                StructFieldAccessorTerminal.FieldName = newFieldName;
                transaction.Commit();
            }
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
            return fieldTypes.Select(this.CreateCommandFromFieldType).ToList();
        }
    }
}
