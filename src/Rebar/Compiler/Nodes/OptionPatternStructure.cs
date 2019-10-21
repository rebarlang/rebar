using System.Linq;
using NationalInstruments.Dfir;

namespace Rebar.Compiler.Nodes
{
    internal class OptionPatternStructure : Structure
    {
        public OptionPatternStructure(Diagram parentDiagram) : base(parentDiagram)
        {
            new OptionPatternStructureSelector(this);
        }

        private OptionPatternStructure(Diagram parentDiagram, OptionPatternStructure nodeToCopy, NodeCopyInfo nodeCopyInfo)
            : base(parentDiagram, nodeToCopy, nodeCopyInfo)
        {
        }

        /// <inheritdoc />
        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            OptionPatternStructure copiedOptionPatternStructure = new OptionPatternStructure((Diagram)newParentNode, this, copyInfo);
            copiedOptionPatternStructure.CloneDiagrams(this, copyInfo);
            copiedOptionPatternStructure.CloneBorderNodes(this, copyInfo);
            copiedOptionPatternStructure.CopyContents(this, copyInfo);
            return copiedOptionPatternStructure;
        }

        /// <inheritdoc />
        public override bool IsYielding => true;

        public new Diagram CreateDiagram()
        {
            return base.CreateDiagram();
        }

        public OptionPatternStructureSelector Selector => BorderNodes.OfType<OptionPatternStructureSelector>().First();
    }
}
