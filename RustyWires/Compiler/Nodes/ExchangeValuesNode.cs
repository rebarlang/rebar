using System.Collections.Generic;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

namespace RustyWires.Compiler.Nodes
{
    internal class ExchangeValuesNode : RustyWiresDfirNode
    {
        public ExchangeValuesNode(Node parentNode) : base(parentNode)
        {
            NIType mutableReferenceType = PFTypes.Void.CreateMutableReference();
            CreateTerminal(Direction.Input, mutableReferenceType, "value in 1");
            CreateTerminal(Direction.Input, mutableReferenceType, "value in 2");
            CreateTerminal(Direction.Output, mutableReferenceType, "value out 1");
            CreateTerminal(Direction.Output, mutableReferenceType, "value out 2");
        }

        private ExchangeValuesNode(Node parentNode, ExchangeValuesNode nodeToCopy, NodeCopyInfo nodeCopyInfo)
            : base(parentNode, nodeToCopy, nodeCopyInfo)
        {
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new ExchangeValuesNode(newParentNode, this, copyInfo);
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
            validator1.TestVariableIsMutableType();
            VariableUsageValidator validator2 = Terminals[1].GetValidator();
            validator2.TestVariableIsMutableType();
            // TODO: ensure that lifetimes of exchanged values and references are compatible
        }
    }
}
