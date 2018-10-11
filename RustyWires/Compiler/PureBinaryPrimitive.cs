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
    internal class PureBinaryPrimitive : RustyWiresDfirNode, ITypePropagationImplementation
    {
        public PureBinaryPrimitive(Node parentNode) : base(parentNode)
        {
            NIType intReferenceType = PFTypes.Int32.CreateImmutableReference();
            NIType intOwnedType = PFTypes.Int32.CreateMutableValue();
            CreateTerminal(Direction.Input, intReferenceType, "x in");
            CreateTerminal(Direction.Input, intReferenceType, "y in");
            CreateTerminal(Direction.Output, intReferenceType, "x out");
            CreateTerminal(Direction.Output, intReferenceType, "y out");
            CreateTerminal(Direction.Output, intOwnedType, "result");
        }

        private PureBinaryPrimitive(Node parentNode, PureBinaryPrimitive nodeToCopy, NodeCopyInfo nodeCopyInfo)
            : base(parentNode, nodeToCopy, nodeCopyInfo)
        {
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new PureBinaryPrimitive(newParentNode, this, copyInfo);
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
            var pureBinaryPrimitive = (PureBinaryPrimitive)node;
            pureBinaryPrimitive.PullInputTypes();

            Terminal refInTerminal1 = pureBinaryPrimitive.Terminals.ElementAt(0),
                refInTerminal2 = pureBinaryPrimitive.Terminals.ElementAt(1),
                refOutTerminal1 = pureBinaryPrimitive.Terminals.ElementAt(2),
                refOutTerminal2 = pureBinaryPrimitive.Terminals.ElementAt(3),
                resultOutTerminal = pureBinaryPrimitive.Terminals.ElementAt(4);
            bool terminal1Connected = refInTerminal1.TestRequiredTerminalConnected();
            bool terminal2Connected = refInTerminal2.TestRequiredTerminalConnected();
            NIType refInType1 = refInTerminal1.DataType;
            NIType refInType2 = refInTerminal2.DataType;
            NIType underlyingType1 = refInType1.GetUnderlyingTypeFromRustyWiresType();
            NIType underlyingType2 = refInType2.GetUnderlyingTypeFromRustyWiresType();
            if (terminal1Connected)
            {
                refOutTerminal1.DataType = refInType1;
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
            if (terminal1Connected && terminal2Connected)
            {
                NIType outType = underlyingType1.IsInt32() && underlyingType2.IsInt32()
                    ? PFTypes.Int32
                    : PFTypes.Void;
                resultOutTerminal.DataType = outType.CreateMutableValue();
            }
            else
            {
                resultOutTerminal.DataType = PFTypes.Void.CreateMutableValue();
            }
            return AsyncHelpers.CompletedTask;
        }
    }
}
