using System.Linq;
using System.Collections.Generic;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

namespace RustyWires.Compiler.Nodes
{
    internal class FreezeNode : RustyWiresDfirNode
    {
        public FreezeNode(Node parentNode) : base(parentNode)
        {
            CreateTerminal(Direction.Input, PFTypes.Void.CreateMutableValue(), "mutable value in");
            CreateTerminal(Direction.Output, PFTypes.Void.CreateImmutableValue(), "immutable value out");
        }

        private FreezeNode(Node parentNode, FreezeNode nodeToCopy, NodeCopyInfo nodeCopyInfo)
            : base(parentNode, nodeToCopy, nodeCopyInfo)
        {
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new FreezeNode(newParentNode, this, copyInfo);
        }

        /// <inheritdoc />
        public override IEnumerable<PassthroughTerminalPair> PassthroughTerminalPairs => Enumerable.Empty<PassthroughTerminalPair>();

        /// <inheritdoc />
        public override void SetOutputVariableTypesAndLifetimes()
        {
            Terminal valueInTerminal = Terminals.ElementAt(0);
            Terminal valueOutTerminal = Terminals.ElementAt(1);
            NIType underlyingType = valueInTerminal.GetVariable().GetUnderlyingTypeOrVoid();
            valueOutTerminal.GetVariable()?.SetTypeAndLifetime(underlyingType.CreateImmutableValue(), Lifetime.Unbounded);
        }

        /// <inheritdoc />
        public override void CheckVariableUsages()
        {
            VariableUsageValidator validator = Terminals[0].GetValidator();
            validator.TestVariableIsOwnedType();
            validator.TestVariableIsMutableType();
        }
    }
}
