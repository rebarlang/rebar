using System.Collections.Generic;
using System.Linq;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Direction = NationalInstruments.CommonModel.Direction;
using Node = NationalInstruments.Dfir.Node;

namespace Rebar.Compiler.Nodes
{
    internal class DropNode : DfirNode
    {
        public DropNode(Node parentNode) : base(parentNode)
        {
            CreateTerminal(Direction.Input, NITypes.Void, "value in");
        }

        private DropNode(Node newParentNode, Node nodeToCopy, NodeCopyInfo copyInfo) 
            : base(newParentNode, nodeToCopy, copyInfo)
        {
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new DropNode(newParentNode, this, copyInfo);
        }

        /// <inheritdoc />
        public override T AcceptVisitor<T>(IDfirNodeVisitor<T> visitor)
        {
            return visitor.VisitDropNode(this);
        }
    }
}
