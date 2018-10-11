using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using NationalInstruments.Compiler;
using NationalInstruments.Compiler.SemanticAnalysis;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

namespace RustyWires.Compiler
{
    internal class FreezeNode : RustyWiresDfirNode, ITypePropagationImplementation
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

        public Task DoTypePropagationAsync(
            Node node,
            ITypePropagationAccessor typePropagationAccessor,
            CompileCancellationToken cancellationToken)
        {
            var freezeNode = (FreezeNode)node;
            Terminal valueInTerminal = freezeNode.Terminals.ElementAt(0);
            Terminal valueOutTerminal = freezeNode.Terminals.ElementAt(1);
            if (valueInTerminal.TestRequiredTerminalConnected())
            {
                freezeNode.PullInputTypes();
                valueInTerminal.TestTerminalHasOwnedValueConnected();
                valueInTerminal.TestTerminalHasMutableTypeConnected();
                NIType valueInType = valueInTerminal.DataType;
                NIType valueUnderlyingType = valueInType.GetUnderlyingTypeFromRustyWiresType();
                valueOutTerminal.DataType = valueUnderlyingType.CreateImmutableValue();
            }
            else
            {
                valueOutTerminal.DataType = PFTypes.Void.CreateImmutableValue();
            }
            return AsyncHelpers.CompletedTask;
        }
    }
}
