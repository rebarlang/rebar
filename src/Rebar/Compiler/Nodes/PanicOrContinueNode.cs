using System;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

namespace Rebar.Compiler.Nodes
{
    internal class PanicOrContinueNode : DfirNode
    {
        public PanicOrContinueNode(Node parentNode) : base(parentNode)
        {
            InputTerminal = CreateTerminal(Direction.Input, PFTypes.Void, "panicResult");
            OutputTerminal = CreateTerminal(Direction.Output, PFTypes.Void, "result");
        }

        private PanicOrContinueNode(Node newParentNode, PanicOrContinueNode nodeToCopy, NodeCopyInfo copyInfo)
            : base(newParentNode, nodeToCopy, copyInfo)
        {
            InputTerminal = copyInfo.GetMappingFor(nodeToCopy.InputTerminal);
            OutputTerminal = copyInfo.GetMappingFor(nodeToCopy.OutputTerminal);
        }

        public Terminal InputTerminal { get; }

        public Terminal OutputTerminal { get; }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new PanicOrContinueNode(newParentNode, this, copyInfo);
        }

        public override T AcceptVisitor<T>(IDfirNodeVisitor<T> visitor)
        {
            var internalVisitor = visitor as IInternalDfirNodeVisitor<T>;
            if (internalVisitor != null)
            {
                return internalVisitor.VisitPanicOrContinueNode(this);
            }
            throw new NotSupportedException("Only accepts IInternalDfirNodeVisitors");
        }
    }
}
