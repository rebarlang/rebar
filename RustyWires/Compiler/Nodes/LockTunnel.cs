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
            Terminal inputTerminal = Terminals.ElementAt(0),
                outputTerminal = Terminals.ElementAt(1);
            Variable inputVariable = inputTerminal.GetVariable();
            NIType inputUnderlyingType = inputVariable.GetUnderlyingTypeOrVoid();
            NIType outputUnderlyingType = inputUnderlyingType.IsLockingCellType()
                ? inputUnderlyingType.GetUnderlyingTypeFromLockingCellType()
                : PFTypes.Void;

            Lifetime sourceLifetime = inputVariable?.Lifetime ?? Lifetime.Empty;
            Lifetime outputLifetime = outputTerminal.GetVariableSet().DefineLifetimeThatOutlastsDiagram();
            outputTerminal.GetVariable()?.SetTypeAndLifetime(outputUnderlyingType.CreateMutableReference(), outputLifetime);
        }

        /// <inheritdoc />
        public override void CheckVariableUsages()
        {
            VariableUsageValidator validator = Terminals[0].GetValidator();
            // TODO: report error if variable type !.IsLockingCellType()
        }
    }
}
