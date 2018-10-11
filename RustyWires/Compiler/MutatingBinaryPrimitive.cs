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
    internal class MutatingBinaryPrimitive : RustyWiresDfirNode, ITypePropagationImplementation
    {
        public MutatingBinaryPrimitive(Node parentNode) : base(parentNode)
        {
            NIType intMutableReferenceType = PFTypes.Int32.CreateMutableReference();
            NIType intImmutableReferenceType = PFTypes.Int32.CreateImmutableReference();
            CreateTerminal(Direction.Input, intMutableReferenceType, "x in");
            CreateTerminal(Direction.Input, intImmutableReferenceType, "y in");
            CreateTerminal(Direction.Output, intMutableReferenceType, "x out");
            CreateTerminal(Direction.Output, intImmutableReferenceType, "y out");
        }

        private MutatingBinaryPrimitive(Node parentNode, MutatingBinaryPrimitive nodeToCopy, NodeCopyInfo nodeCopyInfo)
            : base(parentNode, nodeToCopy, nodeCopyInfo)
        {
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new MutatingBinaryPrimitive(newParentNode, this, copyInfo);
        }

        /// <inheritdoc />
        public override IEnumerable<PassthroughTerminalPair> PassthroughTerminalPairs
        {
            get
            {
                yield return new PassthroughTerminalPair(Terminals[0], Terminals[2]);
                yield return new PassthroughTerminalPair(Terminals[1], Terminals[3]);
            }
        }

        public Task DoTypePropagationAsync(
            Node node,
            ITypePropagationAccessor typePropagationAccessor,
            CompileCancellationToken cancellationToken)
        {
            var mutatingBinaryPrimitive = (MutatingBinaryPrimitive)node;
            mutatingBinaryPrimitive.PullInputTypes();

            Terminal refInTerminal1 = mutatingBinaryPrimitive.Terminals.ElementAt(0),
                refInTerminal2 = mutatingBinaryPrimitive.Terminals.ElementAt(1),
                refOutTerminal1 = mutatingBinaryPrimitive.Terminals.ElementAt(2),
                refOutTerminal2 = mutatingBinaryPrimitive.Terminals.ElementAt(3);
            bool terminal1Connected = refInTerminal1.TestRequiredTerminalConnected();
            bool terminal2Connected = refInTerminal2.TestRequiredTerminalConnected();
            NIType refInType1 = refInTerminal1.DataType;
            NIType refInType2 = refInTerminal2.DataType;
            NIType underlyingType1 = refInType1.GetUnderlyingTypeFromRustyWiresType();
            NIType underlyingType2 = refInType2.GetUnderlyingTypeFromRustyWiresType();
            if (terminal1Connected)
            {
                refOutTerminal1.DataType = refInType1;
                refInTerminal1.TestTerminalHasMutableTypeConnected();
                refInTerminal1.PropagateLifetimeAndTestNonEmpty(refOutTerminal1);

                if (!underlyingType1.IsInt32())
                {
                    refInTerminal1.SetDfirMessage(TerminalUserMessages.CreateTypeConflictMessage(underlyingType1, PFTypes.Int32));
                }
            }
            else
            {
                refOutTerminal1.DataType = PFTypes.Void.CreateImmutableReference();
            }
            if (terminal2Connected)
            {
                refOutTerminal2.DataType = refInType2;
                refInTerminal2.PropagateLifetimeAndTestNonEmpty(refOutTerminal2);

                if (!underlyingType2.IsInt32())
                {
                    refInTerminal2.SetDfirMessage(TerminalUserMessages.CreateTypeConflictMessage(underlyingType2, PFTypes.Int32));
                }
            }
            else
            {
                refOutTerminal2.DataType = PFTypes.Void.CreateImmutableReference();
            }
            return AsyncHelpers.CompletedTask;
        }
    }
}
