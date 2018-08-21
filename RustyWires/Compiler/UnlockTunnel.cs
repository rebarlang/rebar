using System.Linq;
using System.Threading.Tasks;
using NationalInstruments.Compiler;
using NationalInstruments.Compiler.SemanticAnalysis;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

namespace RustyWires.Compiler
{
    internal class UnlockTunnel : RustyWiresBorderNode, ITypePropagationImplementation
    {
        public UnlockTunnel(Structure parentStructure) : base(parentStructure)
        {
            CreateStandardTerminals(Direction.Output, 1u, 1u, PFTypes.Void);
        }

        private UnlockTunnel(Structure parentStructure, UnlockTunnel toCopy, NodeCopyInfo copyInfo)
            : base(parentStructure, toCopy, copyInfo)
        {
            Node mappedTunnel;
            if (copyInfo.TryGetMappingFor(toCopy.AssociatedLockTunnel, out mappedTunnel))
            {
                AssociatedLockTunnel = (LockTunnel)mappedTunnel;
                AssociatedLockTunnel.AssociatedUnlockTunnel = this;
            }
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new UnlockTunnel((Structure)newParentNode, this, copyInfo);
        }

        public LockTunnel AssociatedLockTunnel { get; internal set; }

        public Task DoTypePropagationAsync(
            Node node,
            ITypePropagationAccessor typePropagationAccessor,
            CompileCancellationToken cancellationToken)
        {
            var unlockTunnel = (UnlockTunnel)node;
            Terminal inputTerminal = unlockTunnel.Terminals.ElementAt(1),
                outputTerminal = unlockTunnel.Terminals.ElementAt(0);
            Terminal lockTunnelInputTerminal = unlockTunnel.AssociatedLockTunnel.Terminals.ElementAt(0);
            var lockTunnelType = lockTunnelInputTerminal.DataType;
            inputTerminal.DataType = lockTunnelType;
            outputTerminal.DataType = lockTunnelType;
            if (outputTerminal.DataType.IsRWReferenceType())
            {
                Lifetime sourceLifetime = lockTunnelInputTerminal.GetSourceLifetime();
                outputTerminal.SetLifetime(sourceLifetime);
            }
            return AsyncHelpers.CompletedTask;
        }
    }
}
