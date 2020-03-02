using System;
using System.Linq;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using NationalInstruments.Linking;

namespace Rebar.Compiler.Nodes
{
    internal class PanickingMethodCallNode : DfirNode
    {
        public PanickingMethodCallNode(Node parentNode, NIType signature, ExtendedQualifiedName targetName) : base(parentNode)
        {
            PanicResultTerminal = CreateTerminal(Direction.Output, PFTypes.Void, "panicResult");
            foreach (NIType parameter in signature.GetParameters().Where(p => p.GetInputParameterPassingRule() == NIParameterPassingRule.Required))
            {
                CreateTerminal(Direction.Input, PFTypes.Void, parameter.GetName());
            }
            Signature = signature;
            TargetName = targetName;
        }

        private PanickingMethodCallNode(Node newParentNode, PanickingMethodCallNode nodeToCopy, NodeCopyInfo copyInfo)
            : base(newParentNode, nodeToCopy, copyInfo)
        {
            PanicResultTerminal = copyInfo.GetMappingFor(nodeToCopy.PanicResultTerminal);
            Signature = nodeToCopy.Signature;
            TargetName = nodeToCopy.TargetName;
        }

        public Terminal PanicResultTerminal { get; }

        public NIType Signature { get; }

        public ExtendedQualifiedName TargetName { get; }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new PanickingMethodCallNode(newParentNode, this, copyInfo);
        }

        public override T AcceptVisitor<T>(IDfirNodeVisitor<T> visitor)
        {
            var internalVisitor = visitor as IInternalDfirNodeVisitor<T>;
            if (internalVisitor != null)
            {
                return internalVisitor.VisitPanickingMethodCallNode(this);
            }
            throw new NotSupportedException("Only accepts IInternalDfirNodeVisitors");
        }
    }
}
