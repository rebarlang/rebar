using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NationalInstruments.Compiler;
using NationalInstruments.Compiler.SemanticAnalysis;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

namespace RustyWires.Compiler
{
    internal class CreateCellNode : RustyWiresDfirNode, ITypePropagationImplementation
    {
        public CreateCellNode(Node parentNode) : base(parentNode)
        {
            var immutableReferenceType = PFTypes.Void.CreateImmutableReference();
            CreateTerminal(Direction.Input, immutableReferenceType, "value in");
            CreateTerminal(Direction.Output, immutableReferenceType, "cell out");
        }

        private CreateCellNode(Node parentNode, CreateCellNode nodeToCopy, NodeCopyInfo nodeCopyInfo)
            : base(parentNode, nodeToCopy, nodeCopyInfo)
        {
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new CreateCellNode(newParentNode, this, copyInfo);
        }

        /// <inheritdoc />
        public override IEnumerable<PassthroughTerminalPair> PassthroughTerminalPairs => Enumerable.Empty<PassthroughTerminalPair>();

        public Task DoTypePropagationAsync(
            Node node,
            ITypePropagationAccessor typePropagationAccessor,
            CompileCancellationToken cancellationToken)
        {
            var createCellNode = (CreateCellNode)node;
            Terminal valueInTerminal = createCellNode.Terminals.ElementAt(0);
            Terminal cellOutTerminal = createCellNode.Terminals.ElementAt(1);
            NIType cellType;
            if (valueInTerminal.TestRequiredTerminalConnected())
            {
                createCellNode.PullInputTypes();
                valueInTerminal.TestTerminalHasOwnedValueConnected();
                NIType valueInType = valueInTerminal.DataType;
                NIType valueUnderlyingType = valueInType.GetUnderlyingTypeFromRustyWiresType();
                cellType = valueInType.IsMutableValueType()
                    ? valueUnderlyingType.CreateLockingCell()
                    : valueUnderlyingType.CreateNonLockingCell();
            }
            else
            {
                cellType = PFTypes.Void.CreateNonLockingCell();
            }
            cellOutTerminal.DataType = cellType.CreateMutableValue();

            return AsyncHelpers.CompletedTask;
        }
    }
}
