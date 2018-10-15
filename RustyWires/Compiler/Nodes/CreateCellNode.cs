using System.Collections.Generic;
using System.Linq;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

namespace RustyWires.Compiler.Nodes
{
    internal class CreateCellNode : RustyWiresDfirNode
    {
        public CreateCellNode(Node parentNode) : base(parentNode)
        {
            var immutableReferenceType = PFTypes.Void.CreateImmutableReference();
            CreateTerminal(Direction.Input, immutableReferenceType, "value in");
            CreateTerminal(Direction.Output, immutableReferenceType, "cell out");
        }

        private CreateCellNode(Node parentNode, CreateCellNode nodeToCopy, NodeCopyInfo nodeCopyInfo)
            : base(parentNode, nodeToCopy, nodeCopyInfo)
        {
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new CreateCellNode(newParentNode, this, copyInfo);
        }

        /// <inheritdoc />
        public override IEnumerable<PassthroughTerminalPair> PassthroughTerminalPairs => Enumerable.Empty<PassthroughTerminalPair>();

        /// <inheritdoc />
        public override void SetOutputVariableTypesAndLifetimes()
        {
            VariableSet variableSet = DfirRoot.GetVariableSet();
            Terminal valueInTerminal = Terminals.ElementAt(0);
            Terminal cellOutTerminal = Terminals.ElementAt(1);
            NIType cellType;
            Variable inputVariable = variableSet.GetVariableForTerminal(valueInTerminal);
            if (inputVariable != null)
            {
                NIType underlyingType = inputVariable.Type.GetUnderlyingTypeFromRustyWiresType();
                cellType = inputVariable.Type.IsMutableValueType()
                    ? underlyingType.CreateLockingCell()
                    : underlyingType.CreateNonLockingCell();
            }
            else
            {
                cellType = PFTypes.Void.CreateNonLockingCell();
            }
            variableSet.GetVariableForTerminal(cellOutTerminal)?.SetType(cellType.CreateMutableValue());
        }

        /// <inheritdoc />
        public override void CheckVariableUsages()
        {
            VariableUsageValidator validator = DfirRoot.GetVariableSet().GetValidatorForTerminal(Terminals[0]);
            validator.TestVariableIsOwnedType();
        }
    }
}
