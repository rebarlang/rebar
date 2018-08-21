using System.Linq;
using System.Threading.Tasks;
using NationalInstruments.Compiler;
using NationalInstruments.Compiler.SemanticAnalysis;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

namespace RustyWires.Compiler
{
    internal class LockTunnel : RustyWiresBorderNode, ITypePropagationImplementation
    {
        public LockTunnel(Structure parentStructure) : base(parentStructure)
        {
            CreateStandardTerminals(Direction.Input, 1u, 1u, PFTypes.Void);
        }

        private LockTunnel(Structure parentStructure, LockTunnel toCopy, NodeCopyInfo copyInfo)
            : base(parentStructure, toCopy, copyInfo)
        {
            Node mappedTunnel;
            if (copyInfo.TryGetMappingFor(toCopy.AssociatedUnlockTunnel, out mappedTunnel))
            {
                AssociatedUnlockTunnel = (UnlockTunnel)mappedTunnel;
                AssociatedUnlockTunnel.AssociatedLockTunnel = this;
            }
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new LockTunnel((Structure)newParentNode, this, copyInfo);
        }

        public UnlockTunnel AssociatedUnlockTunnel { get; internal set; }

        public Task DoTypePropagationAsync(
            Node node,
            ITypePropagationAccessor typePropagationAccessor,
            CompileCancellationToken cancellationToken)
        {
            var lockTunnel = (LockTunnel)node;
            Terminal inputTerminal = lockTunnel.Terminals.ElementAt(0),
                outputTerminal = lockTunnel.Terminals.ElementAt(1);
            if (inputTerminal.TestRequiredTerminalConnected())
            {
                inputTerminal.PullInputType();
                NIType inputType = inputTerminal.DataType;
                NIType inputUnderlyingType = inputType.GetUnderlyingTypeFromRustyWiresType();
                if (inputUnderlyingType.IsLockingCellType())
                {
                    outputTerminal.DataType = inputUnderlyingType.GetUnderlyingTypeFromLockingCellType().CreateMutableReference();

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
                    // TODO: add error message
                    outputTerminal.DataType = PFTypes.Void.CreateMutableReference();
                }
            }
            else
            {
                outputTerminal.DataType = PFTypes.Void.CreateMutableReference();
            }
            return AsyncHelpers.CompletedTask;
        }
    }
}
