using System.Collections.Generic;
using System.Threading.Tasks;
using NationalInstruments.Compiler;
using NationalInstruments.Compiler.SemanticAnalysis;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

namespace RustyWires.Compiler
{
    internal class MutablePassthroughNode : RustyWiresDfirNode, ITypePropagationImplementation
    {
        private readonly Terminal _inputTerminal, _outputTerminal;

        public MutablePassthroughNode(Node parentNode) : base(parentNode)
        {
            var mutableReferenceType = PFTypes.Void.CreateMutableReference();
            _inputTerminal = CreateTerminal(Direction.Input, mutableReferenceType, "ref in");
            _outputTerminal = CreateTerminal(Direction.Output, mutableReferenceType, "ref out");
        }

        private MutablePassthroughNode(Node parentNode, MutablePassthroughNode nodeToCopy, NodeCopyInfo nodeCopyInfo)
            : base(parentNode, nodeToCopy, nodeCopyInfo)
        {
        }

        /// <inheritdoc />
        public override IEnumerable<PassthroughTerminalPair> PassthroughTerminalPairs
        {
            get
            {
                yield return new PassthroughTerminalPair(_inputTerminal, _outputTerminal);
            }
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new MutablePassthroughNode(newParentNode, this, copyInfo);
        }

        public Task DoTypePropagationAsync(
            Node node,
            ITypePropagationAccessor typePropagationAccessor,
            CompileCancellationToken cancellationToken)
        {
            var mutablePassthroughNode = (MutablePassthroughNode)node;
            if (mutablePassthroughNode._inputTerminal.TestRequiredTerminalConnected())
            {
                mutablePassthroughNode.PullInputTypes();
                if (mutablePassthroughNode._inputTerminal.TestTerminalHasMutableTypeConnected())
                {
                    mutablePassthroughNode._outputTerminal.DataType = mutablePassthroughNode._inputTerminal.DataType;
                    mutablePassthroughNode._inputTerminal.PropagateLifetimeAndTestNonEmpty(mutablePassthroughNode._outputTerminal);
                }
                else
                {
                    mutablePassthroughNode._outputTerminal.DataType = PFTypes.Void.CreateMutableReference();
                }
            }
            else
            {
                mutablePassthroughNode._outputTerminal.DataType = PFTypes.Void.CreateMutableReference();
            }
            return AsyncHelpers.CompletedTask;
        }
    }
}
