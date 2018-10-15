using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

namespace RustyWires.Compiler.Nodes
{
    internal class UnborrowTunnel : RustyWiresBorderNode
    {
        public UnborrowTunnel(Structure parentStructure) : base(parentStructure)
        {
            CreateStandardTerminals(Direction.Output, 1u, 1u, PFTypes.Void);
        }

        private UnborrowTunnel(Structure parentStructure, UnborrowTunnel toCopy, NodeCopyInfo copyInfo)
            : base(parentStructure, toCopy, copyInfo)
        {
            Node mappedTunnel;
            if (copyInfo.TryGetMappingFor(toCopy.AssociatedBorrowTunnel, out mappedTunnel))
            {
                AssociatedBorrowTunnel = (BorrowTunnel)mappedTunnel;
                AssociatedBorrowTunnel.AssociatedUnborrowTunnel = this;
            }
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new UnborrowTunnel((Structure)newParentNode, this, copyInfo);
        }

        public BorrowTunnel AssociatedBorrowTunnel { get; internal set; }

        /// <inheritdoc />
        public override void SetOutputVariableTypesAndLifetimes()
        {
            // Do nothing; the output terminal's variable is the same as the associated BorrowTunnel's input variable
        }
    }
}
