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
            VariableSet variableSet = DfirRoot.GetVariableSet();
            Terminal refInTerminal = Terminals.ElementAt(0),
                resultOutTerminal = Terminals.ElementAt(2);
            NIType inputUnderlyingType = variableSet.GetVariableForTerminal(refInTerminal).GetUnderlyingTypeOrVoid();
            NIType outputUnderlyingType = inputUnderlyingType.IsInt32() ? PFTypes.Int32 : PFTypes.Void;
            variableSet.GetVariableForTerminal(resultOutTerminal)?.SetType(outputUnderlyingType.CreateMutableValue());
        }

        /// <inheritdoc />
        public override void CheckVariableUsages()
        {
            VariableUsageValidator validator = DfirRoot.GetVariableSet().GetValidatorForTerminal(Terminals[0]);
            validator.TestExpectedUnderlyingType(PFTypes.Int32);
        }
    }
}

