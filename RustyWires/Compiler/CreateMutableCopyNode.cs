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
    internal class CreateMutableCopyNode : RustyWiresDfirNode, ITypePropagationImplementation
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

        public Task DoTypePropagationAsync(
            Node node,
            ITypePropagationAccessor typePropagationAccessor,
            CompileCancellationToken cancellationToken)
        {
            var createMutableCopyNode = (CreateMutableCopyNode)node;
            if (createMutableCopyNode._refInTerminal.TestRequiredTerminalConnected())
            {
                createMutableCopyNode.PullInputTypes();
                NIType refInType = createMutableCopyNode._refInTerminal.DataType;
                createMutableCopyNode._refOutTerminal.DataType = refInType;
                // TODO: check that the underlying type can be copied
                _valueOutTerminal.DataType = refInType.GetUnderlyingTypeFromRustyWiresType().CreateMutableValue();

                createMutableCopyNode._refInTerminal.PropagateLifetimeAndTestNonEmpty(createMutableCopyNode._refOutTerminal);
            }
            else
            {
                createMutableCopyNode._refOutTerminal.DataType = PFTypes.Void.CreateImmutableReference();
                _valueOutTerminal.DataType = PFTypes.Void.CreateMutableValue();
            }
            return AsyncHelpers.CompletedTask;
        }
    }
}
