using System;
using System.Linq;
using NationalInstruments.CommonModel;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;

namespace Rebar.Compiler.Nodes
{
    internal class SelfTypeNode : DfirNode
    {
        public SelfTypeNode(Node parentNode, SelfTypeMode mode, int inputCount) : base(parentNode)
        {
            Mode = mode;
            foreach (int i in Enumerable.Range(0, inputCount))
            {
                CreateTerminal(Direction.Input, NITypes.Void, $"type{i}");
            }
        }

        private SelfTypeNode(Node newParentNode, SelfTypeNode nodeToCopy, NodeCopyInfo copyInfo)
            : base(newParentNode, nodeToCopy, copyInfo)
        {
            Mode = nodeToCopy.Mode;
        }

        public SelfTypeMode Mode { get; }

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
