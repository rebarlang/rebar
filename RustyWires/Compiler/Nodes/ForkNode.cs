using NationalInstruments.Dfir;
using System.Collections.Generic;

namespace RustyWires.Compiler.Nodes
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

        /// <inheritdoc />
        public override IEnumerable<PassthroughTerminalPair> PassthroughTerminalPairs
        {
            get
            {
                yield return new PassthroughTerminalPair(Terminals[0], Terminals[1]);
            }
        }
    }
}
