using System;
using System.Linq;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using NationalInstruments.Linking;

namespace Rebar.Compiler.Nodes
{
    internal class CreateMethodCallPromise : DfirNode
    {
        public CreateMethodCallPromise(Node parentNode, NIType signature, ExtendedQualifiedName targetName, bool mayPanic) : base(parentNode)
        {
            PromiseTerminal = CreateTerminal(Direction.Output, PFTypes.Void, "promise");
            foreach (NIType parameter in signature.GetParameters().Where(p => p.GetInputParameterPassingRule() == NIParameterPassingRule.Required))
            {
                CreateTerminal(Direction.Input, PFTypes.Void, parameter.GetName());
            }
            Signature = signature;
            TargetName = targetName;
            MayPanic = mayPanic;
        }

        private CreateMethodCallPromise(Node newParentNode, CreateMethodCallPromise nodeToCopy, NodeCopyInfo copyInfo)
            : base(newParentNode, nodeToCopy, copyInfo)
        {
            PromiseTerminal = copyInfo.GetMappingFor(nodeToCopy.PromiseTerminal);
            Signature = nodeToCopy.Signature;
            TargetName = nodeToCopy.TargetName;
            MayPanic = nodeToCopy.MayPanic;
        }

        public Terminal PromiseTerminal { get; }

        public NIType Signature { get; }

        public ExtendedQualifiedName TargetName { get; }

        public bool MayPanic { get; }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new CreateMethodCallPromise(newParentNode, this, copyInfo);
        }

        public override T AcceptVisitor<T>(IDfirNodeVisitor<T> visitor)
        {
            var internalVisitor = visitor as IInternalDfirNodeVisitor<T>;
            if (internalVisitor != null)
            {
                return internalVisitor.VisitCreateMethodCallPromise(this);
            }
            throw new NotSupportedException("Only accepts IInternalDfirNodeVisitors");
        }
    }
}
