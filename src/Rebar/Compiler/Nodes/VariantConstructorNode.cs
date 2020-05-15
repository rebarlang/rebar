using System.Linq;
using NationalInstruments.CommonModel;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;

namespace Rebar.Compiler.Nodes
{
    internal class VariantConstructorNode : DfirNode
    {
        public VariantConstructorNode(Node parentNode, NIType variantType, int selectedFieldIndex)
            : base(parentNode)
        {
            VariantType = variantType;
            SelectedFieldIndex = selectedFieldIndex;
            NIType fieldType = variantType.GetFields().ElementAt(selectedFieldIndex).GetDataType();

            CreateTerminal(Direction.Output, NITypes.Void, "variant");
            if (!SelectedFieldType.IsUnit())
            {
                CreateTerminal(Direction.Input, NITypes.Void, "field");
            }
        }

        private VariantConstructorNode(Node newParentNode, VariantConstructorNode nodeToCopy, NodeCopyInfo copyInfo) 
            : base(newParentNode, nodeToCopy, copyInfo)
        {
            VariantType = nodeToCopy.VariantType;
            SelectedFieldIndex = nodeToCopy.SelectedFieldIndex;
        }

        public NIType VariantType { get; }

        public int SelectedFieldIndex { get; }

        public NIType SelectedFieldType => VariantType.GetFields().ElementAt(SelectedFieldIndex).GetDataType();

        public bool RequiresInput => !SelectedFieldType.IsUnit();

        public Terminal VariantOutputTerminal => OutputTerminals[0];

        public Terminal FieldInputTerminal => InputTerminals.FirstOrDefault();

        public override T AcceptVisitor<T>(IDfirNodeVisitor<T> visitor)
        {
            return visitor.VisitVariantConstructorNode(this);
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new VariantConstructorNode(newParentNode, this, copyInfo);
        }
    }
}
