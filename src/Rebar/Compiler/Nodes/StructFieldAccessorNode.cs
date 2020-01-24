using System.Linq;
using NationalInstruments;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

namespace Rebar.Compiler.Nodes
{
    internal class StructFieldAccessorNode : DfirNode
    {
        public StructFieldAccessorNode(Node parentNode, string[] fieldNames) : base(parentNode)
        {
            StructType = PFTypes.Void;
            StructInputTerminal = CreateTerminal(Direction.Input, PFTypes.Void, "struct");
            FieldNames = fieldNames;

            foreach (var fieldNamePair in fieldNames.Zip(Enumerable.Range(0, fieldNames.Length)))
            {
                CreateTerminal(Direction.Output, PFTypes.Void, fieldNamePair.Key ?? $"field{fieldNamePair.Value}");
            }
        }

        private StructFieldAccessorNode(Node newParentNode, StructFieldAccessorNode nodeToCopy, NodeCopyInfo copyInfo) 
            : base(newParentNode, nodeToCopy, copyInfo)
        {
            StructInputTerminal = copyInfo.GetMappingFor(nodeToCopy.StructInputTerminal);
            StructType = nodeToCopy.StructType;
            FieldNames = nodeToCopy.FieldNames;
        }

        public Terminal StructInputTerminal { get; }

        public NIType StructType { get; set; }

        public string[] FieldNames { get; }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new StructFieldAccessorNode(newParentNode, this, copyInfo);
        }

        /// <inheritdoc />
        public override T AcceptVisitor<T>(IDfirNodeVisitor<T> visitor)
        {
            return visitor.VisitStructFieldAccessorNode(this);
        }
    }
}
