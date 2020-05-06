using System;
using NationalInstruments.CommonModel;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

namespace Rebar.Compiler.Nodes
{
    internal class PrimitiveTypeNode : DfirNode
    {
        public PrimitiveTypeNode(Node parentNode, NIType type) : base(parentNode)
        {
            Type = type;
            OutputTerminal = CreateTerminal(Direction.Output, type, "type");
        }

        private PrimitiveTypeNode(Node newParentNode, PrimitiveTypeNode nodeToCopy, NodeCopyInfo copyInfo) 
            : base(newParentNode, nodeToCopy, copyInfo)
        {
            Type = nodeToCopy.Type;
            OutputTerminal = copyInfo.GetMappingFor(nodeToCopy.OutputTerminal);
        }

        public Terminal OutputTerminal { get; }

        public NIType Type { get; }

        /// <inheritdoc />
        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new PrimitiveTypeNode(newParentNode, this, copyInfo);
        }

        public override T AcceptVisitor<T>(IDfirNodeVisitor<T> visitor)
        {
            throw new NotImplementedException();
        }
    }
}
