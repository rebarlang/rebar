using System;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

namespace Rebar.Compiler.Nodes
{
    internal class SelfTypeNode : DfirNode
    {
        public SelfTypeNode(Node parentNode) : base(parentNode)
        {
            InputTerminal = CreateTerminal(Direction.Input, PFTypes.Void, "type");
        }

        private SelfTypeNode(Node newParentNode, SelfTypeNode nodeToCopy, NodeCopyInfo copyInfo)
            : base(newParentNode, nodeToCopy, copyInfo)
        {
            InputTerminal = copyInfo.GetMappingFor(nodeToCopy.InputTerminal);
        }

        public Terminal InputTerminal { get; }

        public NIType Type => InputTerminal.GetTrueVariable().Type;

        public override T AcceptVisitor<T>(IDfirNodeVisitor<T> visitor)
        {
            throw new NotImplementedException();
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new SelfTypeNode(newParentNode, this, copyInfo);
        }
    }
}
