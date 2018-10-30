using System.Collections.Generic;
using System.Linq;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

namespace RustyWires.Compiler.Nodes
{
    internal class PureUnaryPrimitive : RustyWiresDfirNode
    {
        public PureUnaryPrimitive(Node parentNode) : base(parentNode)
        {
            NIType intReferenceType = PFTypes.Int32.CreateImmutableReference();
            NIType intOwnedType = PFTypes.Int32.CreateMutableValue();
            CreateTerminal(Direction.Input, intReferenceType, "x in");
            CreateTerminal(Direction.Output, intReferenceType, "x out");
            CreateTerminal(Direction.Output, intOwnedType, "result");
        }

        private PureUnaryPrimitive(Node parentNode, PureUnaryPrimitive nodeToCopy, NodeCopyInfo nodeCopyInfo)
            : base(parentNode, nodeToCopy, nodeCopyInfo)
        {
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new PureUnaryPrimitive(newParentNode, this, copyInfo);
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
        public override void SetOutputVariableTypesAndLifetimes()
        {
            Terminal refInTerminal = Terminals.ElementAt(0),
                resultOutTerminal = Terminals.ElementAt(2);
            NIType inputUnderlyingType = refInTerminal.GetVariable().GetUnderlyingTypeOrVoid();
            NIType outputUnderlyingType = inputUnderlyingType.IsInt32() ? PFTypes.Int32 : PFTypes.Void;
            resultOutTerminal.GetVariable()?.SetTypeAndLifetime(outputUnderlyingType.CreateMutableValue(), Lifetime.Unbounded);
        }

        /// <inheritdoc />
        public override void CheckVariableUsages()
        {
            VariableUsageValidator validator = Terminals[0].GetValidator();
            validator.TestExpectedUnderlyingType(PFTypes.Int32);
        }
    }
}

