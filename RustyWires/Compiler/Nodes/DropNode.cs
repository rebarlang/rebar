using System.Collections.Generic;
using System.Linq;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Direction = NationalInstruments.Dfir.Direction;
using Node = NationalInstruments.Dfir.Node;

namespace RustyWires.Compiler.Nodes
{
    internal class DropNode : RustyWiresDfirNode
    {
        public DropNode(Node parentNode) : base(parentNode)
        {
            CreateTerminal(Direction.Input, PFTypes.Void, "value in");
        }

        private DropNode(Node newParentNode, Node nodeToCopy, NodeCopyInfo copyInfo) 
            : base(newParentNode, nodeToCopy, copyInfo)
        {
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new DropNode(newParentNode, this, copyInfo);
        }

        /// <inheritdoc />
        public override IEnumerable<PassthroughTerminalPair> PassthroughTerminalPairs => Enumerable.Empty<PassthroughTerminalPair>();

        /// <inheritdoc />
        public override void CheckVariableUsages()
        {
            VariableUsageValidator validator = Terminals[0].GetValidator();
            validator.TestVariableIsOwnedType();
        }
    }
}
