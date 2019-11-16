using NationalInstruments.Dfir;

namespace Rebar.Compiler.Nodes
{
    /// <summary>
    /// DFIR representation of <see cref="SourceModel.Loop"/>.
    /// </summary>
    internal class Loop : Structure
    {
        public Loop(Diagram parentDiagram) : base(parentDiagram)
        {
        }

        private Loop(Diagram parentDiagram, Loop nodeToCopy, NodeCopyInfo nodeCopyInfo)
            : base(parentDiagram, nodeToCopy, nodeCopyInfo)
        {
        }

        /// <inheritdoc />
        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            Loop copiedLoop = new Loop((Diagram)newParentNode, this, copyInfo);
            copiedLoop.CloneDiagrams(this, copyInfo);
            copiedLoop.CloneBorderNodes(this, copyInfo);
            copiedLoop.CopyContents(this, copyInfo);
            return copiedLoop;
        }

        /// <inheritdoc />
        public override bool IsYielding => true;

        public Diagram Diagram => Diagrams[0];
    }
}
