using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NationalInstruments.Compiler;
using NationalInstruments.Compiler.SemanticAnalysis;
using NationalInstruments.Core;
using NationalInstruments.Dfir;
using NationalInstruments.Dfir.Plugin;

namespace RustyWires.Compiler
{
    internal abstract class RustyWiresDfirNode : Node, IDecomposeImplementation
    {
        protected RustyWiresDfirNode(
            Node parentNode,
            IEnumerable<DfirDependency> allDfirDependencies)
            : base(parentNode, allDfirDependencies)
        {
        }

        protected RustyWiresDfirNode(Node parentNode) : base(parentNode)
        {
        }

        protected RustyWiresDfirNode(Node newParentNode, Node nodeToCopy, NodeCopyInfo copyInfo)
            : base(newParentNode, nodeToCopy, copyInfo)
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

    internal static class RustyWiresLifetimes
    {
        private static readonly AttributeDescriptor _lifetimeTokenName = new AttributeDescriptor("RustyWires.Compiler.Lifetime", false);
        private static readonly AttributeDescriptor _lifetimeSetTokenName = new AttributeDescriptor("RustyWires.Compiler.LifetimeSet", false);

        public static void SetLifetime(this Terminal terminal, Lifetime lifetime)
        {
            var token = terminal.DfirRoot.GetOrCreateNamedSparseAttributeToken<Lifetime>(_lifetimeTokenName);
            token.SetAttribute(terminal, lifetime);
        }

        public static Lifetime GetLifetime(this Terminal terminal)
        {
            var token = terminal.DfirRoot.GetOrCreateNamedSparseAttributeToken<Lifetime>(_lifetimeTokenName);
            return token.GetAttribute(terminal);
        }

        public static Lifetime GetSourceLifetime(this Terminal terminal)
        {
            if (!terminal.IsConnected)
            {
                return null;
            }

            var token = terminal.DfirRoot.GetOrCreateNamedSparseAttributeToken<Lifetime>(_lifetimeTokenName);
            var connectedTerminal = terminal.ConnectedTerminal;
            if (!token.HasAttributeStorage(connectedTerminal) && connectedTerminal.ParentNode is Wire)
            {
                Wire wire = (Wire)connectedTerminal.ParentNode;
                Lifetime sourceLifetime = wire.SourceTerminal.GetSourceLifetime();
                wire.SourceTerminal.SetLifetime(sourceLifetime);
                foreach (var sinkTerminal in wire.SinkTerminals)
                {
                    sinkTerminal.SetLifetime(sourceLifetime);
                }
            }
            return connectedTerminal.GetLifetime();
        }

        public static Lifetime ComputeInputTerminalEffectiveLifetime(this Terminal inputTerminal)
        {
            // TODO: this should take a parameter for the type permission level above which to consider the input to be re-borrowed;
            // for now, assume that this level is ImmutableReference.
            if (inputTerminal.DataType.IsImmutableReferenceType())
            {
                return inputTerminal.GetSourceLifetime();
            }
            else
            {
                return inputTerminal.DfirRoot.GetLifetimeSet().EmptyLifetime;
            }
        }

        public static LifetimeSet GetLifetimeSet(this DfirRoot dfirRoot)
        {
            var token = dfirRoot.GetOrCreateNamedSparseAttributeToken<LifetimeSet>(_lifetimeSetTokenName);
            return token.GetAttribute(dfirRoot);
        }
    }

    [DebuggerDisplay("{Category} {Id} : {BaseLifetime}")]
    internal class Lifetime
    {
        public Lifetime()
        {
            BaseLifetime = null;
            Category = LifetimeCategory.Empty;
            Id = 0;
        }

        public Lifetime(LifetimeCategory category, int id, Lifetime baseLifetime)
        {
            BaseLifetime = baseLifetime;
            Category = category;
            Id = id;
        }

        public int Id { get; }

        public LifetimeCategory Category { get; }

        public Lifetime BaseLifetime { get; }

        public bool IsEmpty => Category == LifetimeCategory.Empty;
    }

    internal enum LifetimeCategory
    {
        Empty,

        Node,

        Structure,

        FunctionParameter,

        FunctionStatic
    }

    internal class LifetimeSet
    {
        private readonly List<Lifetime> _lifetimes = new List<Lifetime>();

        public LifetimeSet()
        {
            EmptyLifetime = new Lifetime(LifetimeCategory.Empty, 0, null);
            _lifetimes.Add(EmptyLifetime);
            StaticLifetime = new Lifetime(LifetimeCategory.FunctionStatic, 0, null);
        }

        public Lifetime StaticLifetime { get; }

        public Lifetime EmptyLifetime { get; }

        public Lifetime DefineLifetime(LifetimeCategory category, int id, Lifetime baseLifetime)
        {
            Lifetime existing = _lifetimes.FirstOrDefault(l => l.Category == category && l.Id == id);
            if (existing != null)
            {
                // TODO: check that base lifetimes match
                return existing;
            }
            existing = new Lifetime(category, id, baseLifetime);
            _lifetimes.Add(existing);
            return existing;
        }

        public Lifetime ComputeCommonLifetime(Lifetime left, Lifetime right)
        {
            if (left.Category == LifetimeCategory.FunctionStatic)
            {
                return right;
            }
            if (right.Category == LifetimeCategory.FunctionStatic)
            {
                return left;
            }

            if (left.Category == LifetimeCategory.Structure && right.Category == LifetimeCategory.Structure)
            {
                if (left.Id == right.Id)
                {
                    return left;
                }
                // TODO: if one lifetime is a descendant of the other, return the descendant lifetime
            }
            return EmptyLifetime;
        }
    }
}
