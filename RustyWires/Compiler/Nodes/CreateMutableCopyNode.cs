using System.Collections.Generic;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

namespace RustyWires.Compiler.Nodes
{
    internal class CreateMutableCopyNode : RustyWiresDfirNode
    {
        private readonly Terminal _refInTerminal, _refOutTerminal, _valueOutTerminal;

        public CreateMutableCopyNode(Node parentNode) : base(parentNode)
        {
            var immutableReferenceType = PFTypes.Void.CreateImmutableReference();
            _refInTerminal = CreateTerminal(Direction.Input, immutableReferenceType, "ref in");
            _refOutTerminal = CreateTerminal(Direction.Output, immutableReferenceType, "ref out");
            _valueOutTerminal = CreateTerminal(Direction.Output, PFTypes.Void.CreateMutableValue(), "copy value");
        }

        private CreateMutableCopyNode(Node parentNode, CreateMutableCopyNode nodeToCopy, NodeCopyInfo nodeCopyInfo)
            : base(parentNode, nodeToCopy, nodeCopyInfo)
        {
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new CreateMutableCopyNode(newParentNode, this, copyInfo);
        }

        /// <inheritdoc />
        public override IEnumerable<PassthroughTerminalPair> PassthroughTerminalPairs
        {
            get
            {
                yield return new PassthroughTerminalPair(_refInTerminal, _refOutTerminal);
            }
        }

        /// <inheritdoc />
        public override void SetOutputVariableTypesAndLifetimes()
        {
            VariableSet variableSet = DfirRoot.GetVariableSet();
            NIType outputType = variableSet.GetVariableForTerminal(_refInTerminal).GetUnderlyingTypeOrVoid().CreateMutableValue();
            variableSet.GetVariableForTerminal(_valueOutTerminal)?.SetType(outputType);
        }

        /// <inheritdoc />
        public override void CheckVariableUsages()
        {
            VariableUsageValidator validator = DfirRoot.GetVariableSet().GetValidatorForTerminal(_refInTerminal);
            // TODO: check that the underlying type can be copied
        }
    }
}
