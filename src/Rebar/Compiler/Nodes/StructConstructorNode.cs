using NationalInstruments.CommonModel;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

namespace Rebar.Compiler.Nodes
{
    internal class StructConstructorNode : DfirNode
    {
        public StructConstructorNode(Node parentNode, NIType structType)
            : base(parentNode)
        {
            // TODO: create appropriate dependencies on structType and its field types
            Type = structType;
            CreateTerminal(Direction.Output, structType, "struct");
            foreach (NIType field in structType.GetFields())
            {
                CreateTerminal(Direction.Input, field.GetDataType(), field.GetName());
            }
        }

        private StructConstructorNode(Node newParentNode, StructConstructorNode nodeToCopy, NodeCopyInfo copyInfo) 
            : base(newParentNode, nodeToCopy, copyInfo)
        {
            Type = nodeToCopy.Type;
        }

        public NIType Type { get; }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new StructConstructorNode(newParentNode, this, copyInfo);
        }

        public override T AcceptVisitor<T>(IDfirNodeVisitor<T> visitor)
        {
            return visitor.VisitStructConstructorNode(this);
        }
    }
}
