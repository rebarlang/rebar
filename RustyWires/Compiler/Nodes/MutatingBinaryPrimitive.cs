using System.Collections.Generic;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

namespace RustyWires.Compiler.Nodes
{
    internal class MutatingBinaryPrimitive : RustyWiresDfirNode
    {
        public MutatingBinaryPrimitive(Node parentNode) : base(parentNode)
        {
            NIType intMutableReferenceType = PFTypes.Int32.CreateMutableReference();
            NIType intImmutableReferenceType = PFTypes.Int32.CreateImmutableReference();
            CreateTerminal(Direction.Input, intMutableReferenceType, "x in");
            CreateTerminal(Direction.Input, intImmutableReferenceType, "y in");
            CreateTerminal(Direction.Output, intMutableReferenceType, "x out");
            CreateTerminal(Direction.Output, intImmutableReferenceType, "y out");
        }

        private MutatingBinaryPrimitive(Node parentNode, MutatingBinaryPrimitive nodeToCopy, NodeCopyInfo nodeCopyInfo)
            : base(parentNode, nodeToCopy, nodeCopyInfo)
        {
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new MutatingBinaryPrimitive(newParentNode, this, copyInfo);
        }

        /// <inheritdoc />
        public override IEnumerable<PassthroughTerminalPair> PassthroughTerminalPairs
        {
            get
            {
                yield return new PassthroughTerminalPair(Terminals[0], Terminals[2]);
                yield return new PassthroughTerminalPair(Terminals[1], Terminals[3]);
            }
        }

        /// <inheritdoc />
        public override void CheckVariableUsages()
        {
            VariableUsageValidator validator1 = Terminals[0].GetValidator();
            validator1.TestExpectedUnderlyingType(PFTypes.Int32);
            VariableUsageValidator validator2 = Terminals[1].GetValidator();
            validator2.TestExpectedUnderlyingType(PFTypes.Int32);
        }
    }
}
