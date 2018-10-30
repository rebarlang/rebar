using System.Collections.Generic;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

namespace RustyWires.Compiler.Nodes
{
    internal class MutatingUnaryPrimitive : RustyWiresDfirNode
    {
        public MutatingUnaryPrimitive(Node parentNode) : base(parentNode)
        {
            NIType intMutableReferenceType = PFTypes.Int32.CreateMutableReference();
            CreateTerminal(Direction.Input, intMutableReferenceType, "x in");
            CreateTerminal(Direction.Output, intMutableReferenceType, "x out");
        }

        private MutatingUnaryPrimitive(Node parentNode, MutatingUnaryPrimitive nodeToCopy, NodeCopyInfo nodeCopyInfo)
            : base(parentNode, nodeToCopy, nodeCopyInfo)
        {
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new MutatingUnaryPrimitive(newParentNode, this, copyInfo);
        }

        /// <inheritdoc />
        public override IEnumerable<PassthroughTerminalPair> PassthroughTerminalPairs
        {
            get
            {
                yield return new PassthroughTerminalPair(Terminals[0], Terminals[1]);
            }
        }

        /// <inheritdoc />
        public override void CheckVariableUsages()
        {
            VariableUsageValidator validator = Terminals[0].GetValidator();
            validator.TestExpectedUnderlyingType(PFTypes.Int32);
        }
    }
}
