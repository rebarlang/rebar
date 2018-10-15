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
            VariableSet variableSet = DfirRoot.GetVariableSet();
            Terminal refInTerminal1 = Terminals.ElementAt(0),
                refInTerminal2 = Terminals.ElementAt(1),
                resultOutTerminal = Terminals.ElementAt(4);
            NIType input1UnderlyingType = variableSet.GetVariableForTerminal(refInTerminal1).GetUnderlyingTypeOrVoid();
            NIType input2UnderlyingType = variableSet.GetVariableForTerminal(refInTerminal2).GetUnderlyingTypeOrVoid();
            NIType outputUnderlyingType = input1UnderlyingType.IsInt32() && input2UnderlyingType.IsInt32() ? PFTypes.Int32 : PFTypes.Void;
            variableSet.GetVariableForTerminal(resultOutTerminal)?.SetType(outputUnderlyingType.CreateMutableValue());
        }

        /// <inheritdoc />
        public override void CheckVariableUsages()
        {
            VariableUsageValidator validator1 = DfirRoot.GetVariableSet().GetValidatorForTerminal(Terminals[0]);
            validator1.TestExpectedUnderlyingType(PFTypes.Int32);
            VariableUsageValidator validator2 = DfirRoot.GetVariableSet().GetValidatorForTerminal(Terminals[1]);
            validator2.TestExpectedUnderlyingType(PFTypes.Int32);
        }
    }
}
