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
            Terminal refInTerminal1 = Terminals.ElementAt(0),
                refInTerminal2 = Terminals.ElementAt(1),
                refOutTerminal = Terminals.ElementAt(2);
            Variable input1Variable = refInTerminal1.GetVariable();
            Variable input2Variable = refInTerminal2.GetVariable();
            NIType input1UnderlyingType = input1Variable.GetUnderlyingTypeOrVoid();
            NIType input2UnderlyingType = input2Variable.GetUnderlyingTypeOrVoid();
            NIType outputUnderlyingType = input1UnderlyingType == input2UnderlyingType ? input1UnderlyingType : PFTypes.Void;

            Lifetime inputLifetime1 = input1Variable?.Lifetime ?? Lifetime.Empty;
            Lifetime inputLifetime2 = input2Variable?.Lifetime ?? Lifetime.Empty;
            Lifetime commonLifetime = refInTerminal1.GetVariableSet().ComputeCommonLifetime(inputLifetime1, inputLifetime2);
            refOutTerminal.GetVariable()?.SetTypeAndLifetime(outputUnderlyingType.CreateImmutableReference(), commonLifetime);
        }

        /// <inheritdoc />
        public override void CheckVariableUsages()
        {
            VariableUsageValidator validator1 = Terminals[0].GetValidator();
            VariableUsageValidator validator2 = Terminals[1].GetValidator();
        }
    }
}
