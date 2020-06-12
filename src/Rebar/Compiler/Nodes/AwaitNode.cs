using System;
using NationalInstruments.CommonModel;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;

namespace Rebar.Compiler.Nodes
{
    internal class AwaitNode : DfirNode
    {
        public AwaitNode(Node parentNode) : base(parentNode)
        {
            InputTerminal = CreateTerminal(Direction.Input, NITypes.Void, "promise");
            OutputTerminal = CreateTerminal(Direction.Output, NITypes.Void, "value");
        }

        private AwaitNode(Node newParentNode, AwaitNode nodeToCopy, NodeCopyInfo copyInfo)
            : base(newParentNode, nodeToCopy, copyInfo)
        {
            InputTerminal = copyInfo.GetMappingFor(nodeToCopy.InputTerminal);
            OutputTerminal = copyInfo.GetMappingFor(nodeToCopy.OutputTerminal);
            PollResultVariable = nodeToCopy.PollResultVariable;
        }

        public Terminal InputTerminal { get; }

        public Terminal OutputTerminal { get; }

        public VariableReference PollResultVariable { get; set; }

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
