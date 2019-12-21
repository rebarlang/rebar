using System;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

namespace Rebar.Compiler.Nodes
{
    internal class AwaitNode : DfirNode
    {
        public AwaitNode(Node parentNode) : base(parentNode)
        {
            InputTerminal = CreateTerminal(Direction.Input, PFTypes.Void, "promise");
            OutputTerminal = CreateTerminal(Direction.Output, PFTypes.Void, "value");
        }

        private AwaitNode(Node newParentNode, AwaitNode nodeToCopy, NodeCopyInfo copyInfo)
            : base(newParentNode, nodeToCopy, copyInfo)
        {
            InputTerminal = copyInfo.GetMappingFor(nodeToCopy.InputTerminal);
            OutputTerminal = copyInfo.GetMappingFor(nodeToCopy.OutputTerminal);
        }

        public Terminal InputTerminal { get; }

        public Terminal OutputTerminal { get; }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new AwaitNode(newParentNode, this, copyInfo);
        }

        public override T AcceptVisitor<T>(IDfirNodeVisitor<T> visitor)
        {
            var internalVisitor = visitor as IInternalDfirNodeVisitor<T>;
            if (internalVisitor != null)
            {
                return internalVisitor.VisitAwaitNode(this);
            }
            throw new NotSupportedException("Only accepts IInternalDfirNodeVisitors");
        }

        public override bool IsYielding => true;
    }
}
