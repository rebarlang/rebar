using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NationalInstruments.CommonModel;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

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
            CreateTerminal(Direction.Input, fieldType, "field");
            CreateTerminal(Direction.Output, variantType, "variant");
        }

        private VariantConstructorNode(Node newParentNode, VariantConstructorNode nodeToCopy, NodeCopyInfo copyInfo) 
            : base(newParentNode, nodeToCopy, copyInfo)
        {
            VariantType = nodeToCopy.VariantType;
            SelectedFieldIndex = nodeToCopy.SelectedFieldIndex;
        }

        public NIType VariantType { get; }

        public int SelectedFieldIndex { get; }

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
