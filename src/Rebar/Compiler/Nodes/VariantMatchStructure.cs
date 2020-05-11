using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NationalInstruments.Dfir;

namespace Rebar.Compiler.Nodes
{
    internal sealed class VariantMatchStructure : Structure
    {
        public VariantMatchStructure(Diagram parentDiagram)
            : base(parentDiagram)
        {
            new VariantMatchStructureSelector(this);
        }

        private VariantMatchStructure(Diagram parentDiagram, VariantMatchStructure nodeToCopy, NodeCopyInfo nodeCopyInfo)
            : base(parentDiagram, nodeToCopy, nodeCopyInfo)
        {
        }

        /// <inheritdoc />
        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            var copiedVariantMatchStructure = new VariantMatchStructure((Diagram)newParentNode, this, copyInfo);
            copiedVariantMatchStructure.CloneDiagrams(this, copyInfo);
            copiedVariantMatchStructure.CloneBorderNodes(this, copyInfo);
            copiedVariantMatchStructure.CopyContents(this, copyInfo);
            return copiedVariantMatchStructure;
        }

        /// <inheritdoc />
        public override bool IsYielding => true;

        public new Diagram CreateDiagram()
        {
            return base.CreateDiagram();
        }

        public VariantMatchStructureSelector Selector => BorderNodes.OfType<VariantMatchStructureSelector>().First();
    }
}
