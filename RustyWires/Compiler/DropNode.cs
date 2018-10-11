using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NationalInstruments.Compiler;
using NationalInstruments.Compiler.SemanticAnalysis;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Direction = NationalInstruments.Dfir.Direction;
using Node = NationalInstruments.Dfir.Node;

namespace RustyWires.Compiler
{
    internal class DropNode : RustyWiresDfirNode, ITypePropagationImplementation
    {
        public DropNode(Node parentNode) : base(parentNode)
        {
            CreateTerminal(Direction.Input, PFTypes.Void, "value in");
        }

        private DropNode(Node newParentNode, Node nodeToCopy, NodeCopyInfo copyInfo) 
            : base(newParentNode, nodeToCopy, copyInfo)
        {
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new DropNode(newParentNode, this, copyInfo);
        }

        /// <inheritdoc />
        public override IEnumerable<PassthroughTerminalPair> PassthroughTerminalPairs => Enumerable.Empty<PassthroughTerminalPair>();

        public Task DoTypePropagationAsync(
            Node node,
            ITypePropagationAccessor typePropagationAccessor,
            CompileCancellationToken cancellationToken)
        {
            var dropNode = (DropNode)node;
            NationalInstruments.Dfir.Terminal valueInTerminal = dropNode.Terminals.ElementAt(0);
            if (valueInTerminal.TestRequiredTerminalConnected())
            {
                dropNode.PullInputTypes();
                valueInTerminal.TestTerminalHasOwnedValueConnected();
            }
            return AsyncHelpers.CompletedTask;
        }
    }
}
