using System.Linq;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

namespace RustyWires.Compiler.Nodes
{
    internal class LockTunnel : RustyWiresBorderNode
    {
        public LockTunnel(Structure parentStructure) : base(parentStructure)
        {
            CreateStandardTerminals(Direction.Input, 1u, 1u, PFTypes.Void);
        }

        private LockTunnel(Structure parentStructure, LockTunnel toCopy, NodeCopyInfo copyInfo)
            : base(parentStructure, toCopy, copyInfo)
        {
            Node mappedTunnel;
            if (copyInfo.TryGetMappingFor(toCopy.AssociatedUnlockTunnel, out mappedTunnel))
            {
                AssociatedUnlockTunnel = (UnlockTunnel)mappedTunnel;
                AssociatedUnlockTunnel.AssociatedLockTunnel = this;
            }
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new LockTunnel((Structure)newParentNode, this, copyInfo);
        }

        public UnlockTunnel AssociatedUnlockTunnel { get; internal set; }

        /// <inheritdoc />
        public override void SetOutputVariableTypesAndLifetimes()
        {
            VariableSet variableSet = DfirRoot.GetVariableSet();
            Terminal inputTerminal = Terminals.ElementAt(0),
                outputTerminal = Terminals.ElementAt(1);
            Variable inputVariable = variableSet.GetVariableForTerminal(inputTerminal);
            NIType inputUnderlyingType = inputVariable.GetUnderlyingTypeOrVoid();
            NIType outputUnderlyingType = inputUnderlyingType.IsLockingCellType()
                ? inputUnderlyingType.GetUnderlyingTypeFromLockingCellType()
                : PFTypes.Void;
            Variable outputVariable = variableSet.GetVariableForTerminal(outputTerminal);
            outputVariable?.SetType(outputUnderlyingType.CreateMutableReference());

            LifetimeSet lifetimeSet = DfirRoot.GetLifetimeSet();
            Lifetime sourceLifetime = inputVariable?.Lifetime ?? lifetimeSet.EmptyLifetime;
            Lifetime outputLifetime = lifetimeSet.DefineLifetime(
                LifetimeCategory.Structure,
                ParentNode,
                sourceLifetime.IsEmpty ? null : sourceLifetime);
            outputVariable?.SetLifetime(outputLifetime);
        }

        /// <inheritdoc />
        public override void CheckVariableUsages()
        {
            VariableUsageValidator validator = DfirRoot.GetVariableSet().GetValidatorForTerminal(Terminals[0]);
            // TODO: report error if variable type !.IsLockingCellType()
        }
    }
}
