using System.Linq;
using System.Threading.Tasks;
using NationalInstruments.Compiler;
using NationalInstruments.Compiler.SemanticAnalysis;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using NationalInstruments.Dfir.Plugin;

namespace RustyWires.Compiler
{
    internal abstract class RustyWiresBorderNode : BorderNode, IDecomposeImplementation
    {
        protected RustyWiresBorderNode(Structure parentStructure) : base(parentStructure)
        {
        }

        protected RustyWiresBorderNode(Structure parentStructure, BorderNode toCopy, NodeCopyInfo copyInfo)
            : base(parentStructure, toCopy, copyInfo)
        {
        }

        public override bool IsYielding => false;
        public override bool CausesRuntimeSideEffects => false;
        public override bool CausesDfirRootInvariantSideEffects => false;
        public override bool ReliesOnRuntimeState => false;
        public override bool ReliesOnDfirRootInvariantState => false;

        public DecomposeStrategy DecomposeWhen(ISemanticAnalysisTargetInfo targetInfo)
        {
            return DecomposeStrategy.AfterSemanticAnalysis;
        }

        public Task DecomposeAsync(Diagram diagram, DecompositionTerminalAssociator terminalAssociator,
            ISemanticAnalysisTargetInfo targetInfo, CompileCancellationToken cancellationToken)
        {
            // Don't do anything; one of the transforms added by RustyWiresMocPlugin should remove all of these nodes from the DfirGraph during
            // target DFIR translation.
            return AsyncHelpers.CompletedTask;
        }

        public bool SynchronizeAtNodeBoundaries => false;
    }

    internal class BorrowTunnel : RustyWiresBorderNode, ITypePropagationImplementation
    {
        public BorrowTunnel(Structure parentStructure, Common.BorrowMode borrowMode) : base(parentStructure)
        {
            CreateStandardTerminals(Direction.Input, 1u, 1u, PFTypes.Void);
            BorrowMode = borrowMode;
        }

        private BorrowTunnel(Structure parentStructure, BorrowTunnel toCopy, NodeCopyInfo copyInfo)
            : base(parentStructure, toCopy, copyInfo)
        {
            BorrowMode = toCopy.BorrowMode;
            Node mappedTunnel;
            if (copyInfo.TryGetMappingFor(toCopy.AssociatedUnborrowTunnel, out mappedTunnel))
            {
                AssociatedUnborrowTunnel = (UnborrowTunnel)mappedTunnel;
                AssociatedUnborrowTunnel.AssociatedBorrowTunnel = this;
            }
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new BorrowTunnel((Structure)newParentNode, this, copyInfo);
        }

        public Common.BorrowMode BorrowMode { get; }

        public UnborrowTunnel AssociatedUnborrowTunnel { get; internal set; }

        public Task DoTypePropagationAsync(
            Node node,
            ITypePropagationAccessor typePropagationAccessor,
            CompileCancellationToken cancellationToken)
        {
            var borrowTunnel = (BorrowTunnel)node;
            Terminal inputTerminal = borrowTunnel.Terminals.ElementAt(0),
                outputTerminal = borrowTunnel.Terminals.ElementAt(1);
            NIType outputUnderlyingType;
            if (inputTerminal.TestRequiredTerminalConnected())
            {
                inputTerminal.PullInputType();
                outputUnderlyingType = inputTerminal.DataType.GetUnderlyingTypeFromRustyWiresType();
                if (borrowTunnel.BorrowMode == Common.BorrowMode.Mutable)
                {
                    inputTerminal.TestTerminalHasMutableTypeConnected();
                }

                LifetimeSet lifetimeSet = node.DfirRoot.GetLifetimeSet();
                Lifetime sourceLifetime = lifetimeSet.EmptyLifetime;
                if (inputTerminal.DataType.IsRWReferenceType())
                {
                    sourceLifetime = inputTerminal.ComputeInputTerminalEffectiveLifetime();
                }
                Lifetime outputLifetime = lifetimeSet.DefineLifetime(
                    LifetimeCategory.Structure,
                    node.ParentNode.UniqueId,
                    sourceLifetime.IsEmpty ? null : sourceLifetime);
                outputTerminal.SetLifetime(outputLifetime);
            }
            else
            {
                outputUnderlyingType = PFTypes.Void;
            }
            outputTerminal.DataType = borrowTunnel.BorrowMode == Common.BorrowMode.Mutable
                ? outputUnderlyingType.CreateMutableReference()
                : outputUnderlyingType.CreateImmutableReference();
            return AsyncHelpers.CompletedTask;
        }
    }

    internal class UnborrowTunnel : RustyWiresBorderNode, ITypePropagationImplementation
    {
        public UnborrowTunnel(Structure parentStructure) : base(parentStructure)
        {
            CreateStandardTerminals(Direction.Output, 1u, 1u, PFTypes.Void);
        }

        private UnborrowTunnel(Structure parentStructure, UnborrowTunnel toCopy, NodeCopyInfo copyInfo)
            : base(parentStructure, toCopy, copyInfo)
        {
            Node mappedTunnel;
            if (copyInfo.TryGetMappingFor(toCopy.AssociatedBorrowTunnel, out mappedTunnel))
            {
                AssociatedBorrowTunnel = (BorrowTunnel)mappedTunnel;
                AssociatedBorrowTunnel.AssociatedUnborrowTunnel = this;
            }
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new UnborrowTunnel((Structure)newParentNode, this, copyInfo);
        }

        public BorrowTunnel AssociatedBorrowTunnel { get; internal set; }

        public Task DoTypePropagationAsync(
            Node node,
            ITypePropagationAccessor typePropagationAccessor,
            CompileCancellationToken cancellationToken)
        {
            var unborrowTunnel = (UnborrowTunnel)node;
            Terminal inputTerminal = unborrowTunnel.Terminals.ElementAt(1),
                outputTerminal = unborrowTunnel.Terminals.ElementAt(0);
            Terminal borrowTunnelInputTerminal = unborrowTunnel.AssociatedBorrowTunnel.Terminals.ElementAt(0);
            var borrowTunnelType = borrowTunnelInputTerminal.DataType;
            inputTerminal.DataType = borrowTunnelType;
            outputTerminal.DataType = borrowTunnelType;
            if (outputTerminal.DataType.IsRWReferenceType())
            {
                Lifetime sourceLifetime = borrowTunnelInputTerminal.GetSourceLifetime();
                outputTerminal.SetLifetime(sourceLifetime);
            }
            return AsyncHelpers.CompletedTask;
        }
    }
}
