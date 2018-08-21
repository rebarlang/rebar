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

namespace RustyWires.Compiler
{
    internal class PureUnaryPrimitive : RustyWiresDfirNode, ITypePropagationImplementation
    {
        public PureUnaryPrimitive(Node parentNode) : base(parentNode)
        {
            NIType intReferenceType = PFTypes.Int32.CreateImmutableReference();
            NIType intOwnedType = PFTypes.Int32.CreateMutableValue();
            CreateTerminal(Direction.Input, intReferenceType, "x in");
            CreateTerminal(Direction.Output, intReferenceType, "x out");
            CreateTerminal(Direction.Output, intOwnedType, "result");
        }

        private PureUnaryPrimitive(Node parentNode, PureUnaryPrimitive nodeToCopy, NodeCopyInfo nodeCopyInfo)
            : base(parentNode, nodeToCopy, nodeCopyInfo)
        {
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new PureUnaryPrimitive(newParentNode, this, copyInfo);
        }

        public Task DoTypePropagationAsync(
            Node node,
            ITypePropagationAccessor typePropagationAccessor,
            CompileCancellationToken cancellationToken)
        {
            var pureUnaryPrimitive = (PureUnaryPrimitive)node;
            pureUnaryPrimitive.PullInputTypes();

            Terminal refInTerminal1 = pureUnaryPrimitive.Terminals.ElementAt(0),
                refOutTerminal1 = pureUnaryPrimitive.Terminals.ElementAt(1),
                resultOutTerminal = pureUnaryPrimitive.Terminals.ElementAt(2);
            bool terminal1Connected = refInTerminal1.TestRequiredTerminalConnected();
            NIType refInType1 = refInTerminal1.DataType;
            NIType underlyingType1 = refInType1.GetUnderlyingTypeFromRustyWiresType();
            if (terminal1Connected)
            {
                refOutTerminal1.DataType = refInType1;
                refInTerminal1.PropagateLifetimeAndTestNonEmpty(refOutTerminal1);

                NIType outType;
                if (!underlyingType1.IsInt32())
                {
                    refInTerminal1.SetDfirMessage(TerminalUserMessages.CreateTypeConflictMessage(underlyingType1, PFTypes.Int32));
                    outType = PFTypes.Void;
                }
                else
                {
                    outType = PFTypes.Int32;
                }
                resultOutTerminal.DataType = outType.CreateMutableValue();
            }
            else
            {
                refOutTerminal1.DataType = PFTypes.Void.CreateImmutableReference();
                resultOutTerminal.DataType = PFTypes.Void.CreateMutableValue();
            }
            return AsyncHelpers.CompletedTask;
        }
    }
}

