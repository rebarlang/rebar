using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NationalInstruments.Compiler;
using NationalInstruments.Compiler.SemanticAnalysis;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using RustyWires.Common;

namespace RustyWires.Compiler
{
    internal class ImmutablePassthroughNode : RustyWiresDfirNode, ITypePropagationImplementation
    {
        private readonly Terminal _inputTerminal, _outputTerminal;

        public ImmutablePassthroughNode(Node parentNode) : base(parentNode)
        {
            var immutableReferenceType = PFTypes.Void.CreateImmutableReference();
            _inputTerminal = CreateTerminal(Direction.Input, immutableReferenceType, "ref in");
            _outputTerminal = CreateTerminal(Direction.Output, immutableReferenceType, "ref out");
        }

        private ImmutablePassthroughNode(Node parentNode, ImmutablePassthroughNode nodeToCopy, NodeCopyInfo nodeCopyInfo)
            : base(parentNode, nodeToCopy, nodeCopyInfo)
        {
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new ImmutablePassthroughNode(newParentNode, this, copyInfo);
        }

        /// <inheritdoc />
        public override IEnumerable<PassthroughTerminalPair> PassthroughTerminalPairs
        {
            get
            {
                yield return new PassthroughTerminalPair(_inputTerminal, _outputTerminal);
            }
        }

        public Task DoTypePropagationAsync(
            Node node,
            ITypePropagationAccessor typePropagationAccessor,
            CompileCancellationToken cancellationToken)
        {
            var immutablePassthroughNode = (ImmutablePassthroughNode)node;
            CommonNodes.ImmutablePassthrough.PropagateTypes(immutablePassthroughNode);
            return AsyncHelpers.CompletedTask;
        }
    }
}
