using System.Linq;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

namespace RustyWires.Compiler.Nodes
{
    internal class BorrowTunnel : RustyWiresBorderNode
    {
        public BorrowTunnel(Structure parentStructure, Common.BorrowMode borrowMode) : base(parentStructure)
        {
            CreateStandardTerminals(Direction.Input, 1u, 1u, PFTypes.Void);
            BorrowMode = borrowMode;
        }

        private BorrowTunnel(Structure parentStructure, BorrowTunnel toCopy, NodeCopyInfo copyInfo)
            : base(parentStructure, toCopy, copyInfo)
        {
            BorrowMode = toCopy.BorrowMode;
            Node mappedTunnel;
            if (copyInfo.TryGetMappingFor(toCopy.AssociatedUnborrowTunnel, out mappedTunnel))
            {
                AssociatedUnborrowTunnel = (UnborrowTunnel)mappedTunnel;
                AssociatedUnborrowTunnel.AssociatedBorrowTunnel = this;
            }
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new BorrowTunnel((Structure)newParentNode, this, copyInfo);
        }

        public Common.BorrowMode BorrowMode { get; }

        public UnborrowTunnel AssociatedUnborrowTunnel { get; internal set; }

        /// <inheritdoc />
        public override void SetOutputVariableTypesAndLifetimes()
        {
            VariableSet variableSet = DfirRoot.GetVariableSet();
            Terminal inputTerminal = Terminals.ElementAt(0),
                outputTerminal = Terminals.ElementAt(1);
            Variable inputVariable = variableSet.GetVariableForTerminal(inputTerminal);
            NIType outputUnderlyingType = inputVariable.GetUnderlyingTypeOrVoid();
            NIType outputType = BorrowMode == Common.BorrowMode.Mutable
                ? outputUnderlyingType.CreateMutableReference()
                : outputUnderlyingType.CreateImmutableReference();
            Variable outputVariable = variableSet.GetVariableForTerminal(outputTerminal);
            outputVariable?.SetType(outputType);

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
            var validator = DfirRoot.GetVariableSet().GetValidatorForTerminal(Terminals[0]);
            validator.TestVariableIsMutableType();
        }
    }
}
