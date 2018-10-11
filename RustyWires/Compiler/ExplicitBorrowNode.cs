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
    internal class ExplicitBorrowNode : RustyWiresDfirNode, ITypePropagationImplementation
    {
        public ExplicitBorrowNode(Node parentNode, BorrowMode borrowMode) : base(parentNode)
        {
            BorrowMode = borrowMode;
            NIType inputType, outputType;
            switch (borrowMode)
            {
                case BorrowMode.OwnerToMutable:
                    inputType = PFTypes.Void;
                    outputType = PFTypes.Void.CreateMutableReference();
                    break;
                case BorrowMode.OwnerToImmutable:
                    inputType = PFTypes.Void;
                    outputType = PFTypes.Void.CreateImmutableReference();
                    break;
                default:
                    inputType = PFTypes.Void.CreateMutableReference();
                    outputType = PFTypes.Void.CreateImmutableReference();
                    break;
            }
            InputTerminal = CreateTerminal(Direction.Input, inputType, "in");
            OutputTerminal = CreateTerminal(Direction.Output, outputType, "out");
        }

        private ExplicitBorrowNode(Node parentNode, ExplicitBorrowNode copyFrom, NodeCopyInfo copyInfo)
            : base(parentNode, copyFrom, copyInfo)
        {
            BorrowMode = copyFrom.BorrowMode;
        }

        public BorrowMode BorrowMode { get; }

        public Terminal InputTerminal { get; }

        public Terminal OutputTerminal { get; }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new ExplicitBorrowNode(newParentNode, this, copyInfo);
        }

        /// <inheritdoc />
        public override IEnumerable<PassthroughTerminalPair> PassthroughTerminalPairs => Enumerable.Empty<PassthroughTerminalPair>();

        public Task DoTypePropagationAsync(
            Node node,
            ITypePropagationAccessor typePropagationAccessor,
            CompileCancellationToken cancellationToken)
        {
            var explicitBorrowNode = (ExplicitBorrowNode)node;
            var inputTerminal = explicitBorrowNode.Terminals.ElementAt(0);
            var outputTerminal = explicitBorrowNode.Terminals.ElementAt(1);
            if (inputTerminal.TestRequiredTerminalConnected())
            {
                inputTerminal.PullInputType();
                if (BorrowMode == BorrowMode.OwnerToImmutable)
                {
                    outputTerminal.DataType = inputTerminal.DataType.GetUnderlyingTypeFromRustyWiresType().CreateImmutableReference();

                    LifetimeSet lifetimeSet = node.DfirRoot.GetLifetimeSet();
                    Lifetime sourceLifetime = lifetimeSet.EmptyLifetime;
                    if (inputTerminal.DataType.IsRWReferenceType())
                    {
                        sourceLifetime = inputTerminal.ComputeInputTerminalEffectiveLifetime();
                    }
                    Lifetime outputLifetime = lifetimeSet.DefineLifetime(
                        LifetimeCategory.Node,
                        node.UniqueId,
                        sourceLifetime.IsEmpty ? null : sourceLifetime);
                    outputTerminal.SetLifetime(outputLifetime);
                }
                else
                {
                    outputTerminal.DataType = PFTypes.Void.CreateImmutableReference();
                }
            }
            else
            {
                outputTerminal.DataType = PFTypes.Void.CreateImmutableReference();
            }
            return AsyncHelpers.CompletedTask;
        }
    }

    internal enum BorrowMode
    {
        OwnerToMutable,
        OwnerToImmutable,
        MutableToImmutable
    }
}
