using NationalInstruments.Compiler;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler.Nodes;

namespace Rebar.Compiler
{
    internal class InsertDropTransform : IDfirTransform
    {
        private readonly LifetimeVariableAssociation _lifetimeVariableAssociation;

        public InsertDropTransform(LifetimeVariableAssociation lifetimeVariableAssociation)
        {
            _lifetimeVariableAssociation = lifetimeVariableAssociation;
        }

        public void Execute(DfirRoot dfirRoot, CompileCancellationToken cancellationToken)
        {
            LiveVariable liveUnboundedLifetimeVariable;
            while (_lifetimeVariableAssociation.TryGetLiveVariableWithUnboundedLifetime(out liveUnboundedLifetimeVariable))
            {
                Diagram parentDiagram = liveUnboundedLifetimeVariable.Terminal.ParentDiagram;
                var drop = new DropNode(parentDiagram);
                Terminal inputTerminal = drop.InputTerminals[0];
                AutoBorrowNodeFacade nodeFacade = AutoBorrowNodeFacade.GetNodeFacade(drop);
                nodeFacade[inputTerminal] = new SimpleTerminalFacade(inputTerminal, default(TypeVariableReference));

                Wire.Create(parentDiagram, liveUnboundedLifetimeVariable.Terminal, inputTerminal);
                inputTerminal.GetFacadeVariable().MergeInto(liveUnboundedLifetimeVariable.Variable);
                _lifetimeVariableAssociation.MarkVariableConsumed(liveUnboundedLifetimeVariable.Variable);
            }
        }
    }
}
