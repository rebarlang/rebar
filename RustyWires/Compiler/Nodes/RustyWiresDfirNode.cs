using System.Collections.Generic;
using System.Threading.Tasks;
using NationalInstruments.Compiler;
using NationalInstruments.Compiler.SemanticAnalysis;
using NationalInstruments.Core;
using NationalInstruments.Dfir;
using NationalInstruments.Dfir.Plugin;

namespace RustyWires.Compiler.Nodes
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

        public abstract T AcceptVisitor<T>(IRustyWiresDfirNodeVisitor<T> visitor);
    }
}
