using System.Collections.Generic;
using System.Linq;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

namespace RustyWires.Compiler.Nodes
{
    internal class PureBinaryPrimitive : RustyWiresDfirNode
    {
        public PureBinaryPrimitive(Node parentNode) : base(parentNode)
        {
            NIType intReferenceType = PFTypes.Int32.CreateImmutableReference();
            NIType intOwnedType = PFTypes.Int32.CreateMutableValue();
            CreateTerminal(Direction.Input, intReferenceType, "x in");
            CreateTerminal(Direction.Input, intReferenceType, "y in");
            CreateTerminal(Direction.Output, intReferenceType, "x out");
            CreateTerminal(Direction.Output, intReferenceType, "y out");
            CreateTerminal(Direction.Output, intOwnedType, "result");
        }

        private PureBinaryPrimitive(Node parentNode, PureBinaryPrimitive nodeToCopy, NodeCopyInfo nodeCopyInfo)
            : base(parentNode, nodeToCopy, nodeCopyInfo)
        {
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new PureBinaryPrimitive(newParentNode, this, copyInfo);
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
        public override void SetOutputVariableTypesAndLifetimes()
        {
            Terminal refInTerminal1 = Terminals.ElementAt(0),
                refInTerminal2 = Terminals.ElementAt(1),
                resultOutTerminal = Terminals.ElementAt(4);
            NIType input1UnderlyingType = refInTerminal1.GetVariable().GetUnderlyingTypeOrVoid();
            NIType input2UnderlyingType = refInTerminal2.GetVariable().GetUnderlyingTypeOrVoid();
            NIType outputUnderlyingType = input1UnderlyingType.IsInt32() && input2UnderlyingType.IsInt32() ? PFTypes.Int32 : PFTypes.Void;
            resultOutTerminal.GetVariable()?.SetTypeAndLifetime(outputUnderlyingType.CreateMutableValue(), Lifetime.Unbounded);
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
