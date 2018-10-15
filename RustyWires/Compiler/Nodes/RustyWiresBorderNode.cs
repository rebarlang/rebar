using System.Threading.Tasks;
using NationalInstruments.Compiler;
using NationalInstruments.Compiler.SemanticAnalysis;
using NationalInstruments.Core;
using NationalInstruments.Dfir;
using NationalInstruments.Dfir.Plugin;

namespace RustyWires.Compiler.Nodes
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

        /// <summary>
        /// Sets the initial <see cref="NIType"/> and <see cref="Lifetime"/> of any <see cref="Variable"/>s associated
        /// with non-passthrough output terminals on this border node. Can assume that all <see cref="Variable"/>s associated 
        /// with input terminals (passthrough and non-passthrough) have initial types and lifetimes set.
        /// </summary>
        public virtual void SetOutputVariableTypesAndLifetimes()
        {
        }

        /// <summary>
        /// Checks that all <see cref="Variable"/> usages associated with input terminals on this border node are correct.
        /// Can assume that all <see cref="Variable"/>s associated with input terminals have initial types and lifetimes set.
        /// </summary>
        public virtual void CheckVariableUsages()
        {
        }
    }
}
