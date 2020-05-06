using System;
using NationalInstruments.CommonModel;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

namespace Rebar.Compiler.Nodes
{
    internal class DecomposeStructNode : DfirNode
    {
        public DecomposeStructNode(Node parentNode, NIType structType)
            : base(parentNode)
        {
            Type = structType;
            CreateTerminal(Direction.Input, NITypes.Void, "struct");
            foreach (NIType field in structType.GetFields())
            {
                CreateTerminal(Direction.Output, field.GetDataType(), field.GetName());
            }
        }

        public NIType Type { get; }

        private DecomposeStructNode(Node parentNode, DecomposeStructNode copyFrom, NodeCopyInfo copyInfo)
            : base(parentNode, copyFrom, copyInfo)
        {
            Type = copyFrom.Type;
        }

        public DecomposeMode DecomposeMode { get; }

        public override T AcceptVisitor<T>(IDfirNodeVisitor<T> visitor)
        {
            var internalVisitor = visitor as IInternalDfirNodeVisitor<T>;
            if (internalVisitor != null)
            {
                return internalVisitor.VisitDecomposeStructNode(this);
            }
            throw new NotSupportedException("Only accepts IInternalDfirNodeVisitors");
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new DecomposeStructNode(newParentNode, this, copyInfo);
        }
    }
}
