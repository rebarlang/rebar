using System.Collections.Generic;
using System.Linq;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

namespace RustyWires.Compiler.Nodes
{
    internal class SelectReferenceNode : RustyWiresDfirNode
    {
        public SelectReferenceNode(Node parentNode) : base(parentNode)
        {
            var immutableReferenceType = PFTypes.Void.CreateImmutableReference();
            CreateTerminal(Direction.Input, immutableReferenceType, "ref in 1");
            CreateTerminal(Direction.Input, immutableReferenceType, "ref in 2");
            CreateTerminal(Direction.Output, immutableReferenceType, "ref out");
        }

        private SelectReferenceNode(Node parentNode, SelectReferenceNode nodeToCopy, NodeCopyInfo nodeCopyInfo)
            : base(parentNode, nodeToCopy, nodeCopyInfo)
        {
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new SelectReferenceNode(newParentNode, this, copyInfo);
        }

        /// <inheritdoc />
        public override IEnumerable<PassthroughTerminalPair> PassthroughTerminalPairs => Enumerable.Empty<PassthroughTerminalPair>();

        /// <inheritdoc />
        public override void SetOutputVariableTypesAndLifetimes()
        {
            VariableSet variableSet = DfirRoot.GetVariableSet();
            Terminal refInTerminal1 = Terminals.ElementAt(0),
                refInTerminal2 = Terminals.ElementAt(1),
                refOutTerminal = Terminals.ElementAt(2);
            Variable input1Variable = variableSet.GetVariableForTerminal(refInTerminal1);
            Variable input2Variable = variableSet.GetVariableForTerminal(refInTerminal2);
            NIType input1UnderlyingType = input1Variable.GetUnderlyingTypeOrVoid();
            NIType input2UnderlyingType = input2Variable.GetUnderlyingTypeOrVoid();
            NIType outputUnderlyingType = input1UnderlyingType == input2UnderlyingType ? input1UnderlyingType : PFTypes.Void;
            Variable outputVariable = variableSet.GetVariableForTerminal(refOutTerminal);
            outputVariable?.SetType(outputUnderlyingType.CreateImmutableReference());

            LifetimeSet lifetimeSet = DfirRoot.GetLifetimeSet();
            Lifetime inputLifetime1 = input1Variable?.Lifetime ?? lifetimeSet.EmptyLifetime;
            Lifetime inputLifetime2 = input2Variable?.Lifetime ?? lifetimeSet.EmptyLifetime;
            Lifetime commonLifetime = lifetimeSet.ComputeCommonLifetime(inputLifetime1, inputLifetime2);
            outputVariable?.SetLifetime(commonLifetime);
        }

        /// <inheritdoc />
        public override void CheckVariableUsages()
        {
            VariableUsageValidator validator1 = DfirRoot.GetVariableSet().GetValidatorForTerminal(Terminals[0]);
            VariableUsageValidator validator2 = DfirRoot.GetVariableSet().GetValidatorForTerminal(Terminals[1]);
        }
    }
}
