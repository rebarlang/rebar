using NationalInstruments.Compiler;
using NationalInstruments.Compiler.SemanticAnalysis;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace RustyWires.Compiler
{
    internal class MutatingUnaryPrimitive : RustyWiresDfirNode, ITypePropagationImplementation
    {
        public MutatingUnaryPrimitive(Node parentNode) : base(parentNode)
        {
            NIType intMutableReferenceType = PFTypes.Int32.CreateMutableReference();
            CreateTerminal(Direction.Input, intMutableReferenceType, "x in");
            CreateTerminal(Direction.Output, intMutableReferenceType, "x out");
        }

        private MutatingUnaryPrimitive(Node parentNode, MutatingUnaryPrimitive nodeToCopy, NodeCopyInfo nodeCopyInfo)
            : base(parentNode, nodeToCopy, nodeCopyInfo)
        {
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new MutatingUnaryPrimitive(newParentNode, this, copyInfo);
        }

        /// <inheritdoc />
        public override IEnumerable<PassthroughTerminalPair> PassthroughTerminalPairs
        {
            get
            {
                yield return new PassthroughTerminalPair(Terminals[0], Terminals[1]);
            }
        }

        public Task DoTypePropagationAsync(
            Node node,
            ITypePropagationAccessor typePropagationAccessor,
            CompileCancellationToken cancellationToken)
        {
            var mutatingUnaryPrimitive = (MutatingUnaryPrimitive)node;
            mutatingUnaryPrimitive.PullInputTypes();

            Terminal refInTerminal = mutatingUnaryPrimitive.Terminals.ElementAt(0),
                refOutTerminal = mutatingUnaryPrimitive.Terminals.ElementAt(1);
            bool terminalConnected = refInTerminal.TestRequiredTerminalConnected();
            NIType refInType = refInTerminal.DataType;
            NIType underlyingType = refInType.GetUnderlyingTypeFromRustyWiresType();
            if (terminalConnected)
            {
                refOutTerminal.DataType = refInType;
                refInTerminal.TestTerminalHasMutableTypeConnected();
                refInTerminal.PropagateLifetimeAndTestNonEmpty(refOutTerminal);

                if (!underlyingType.IsInt32())
                {
                    refInTerminal.SetDfirMessage(TerminalUserMessages.CreateTypeConflictMessage(underlyingType, PFTypes.Int32));
                }
            }
            else
            {
                refOutTerminal.DataType = PFTypes.Void.CreateImmutableReference();
            }
            return AsyncHelpers.CompletedTask;
        }
    }
}
