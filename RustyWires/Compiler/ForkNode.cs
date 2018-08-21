using NationalInstruments.Dfir;

namespace RustyWires.Compiler
{
    internal class ForkNode : RustyWiresDfirNode
    {
        public ForkNode(Node parentNode, int outputs) : base(parentNode)
        {
            CreateTerminal(Direction.Input, "input");
            for (int i = 0; i < outputs; ++i)
            {
                CreateTerminal(Direction.Output, "output " + i);
            }
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new ForkNode(newParentNode, OutputTerminals.Count);
        }
    }
}
