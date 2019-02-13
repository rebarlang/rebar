using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

namespace Rebar.Compiler.Nodes
{
    /// <summary>
    /// DFIR representation for the <see cref="SourceModel.UnwrapOptionTunnel"/>.
    /// </summary>
    internal class UnwrapOptionTunnel : BorderNode
    {
        public UnwrapOptionTunnel(Structure parentStructure) : base(parentStructure)
        {
            CreateStandardTerminals(Direction.Input, 1u, 1u, PFTypes.Void);
        }

        private UnwrapOptionTunnel(Structure parentStructure, UnwrapOptionTunnel toCopy, NodeCopyInfo copyInfo)
            : base(parentStructure, toCopy, copyInfo)
        {
        }

        /// <inheritdoc />
        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new UnwrapOptionTunnel((Structure)newParentNode, this, copyInfo);
        }

        /// <inheritdoc />
        public override T AcceptVisitor<T>(IDfirNodeVisitor<T> visitor)
        {
            return visitor.VisitUnwrapOptionTunnel(this);
        }
    }
}
