using NationalInstruments.Compiler;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler.Nodes;

namespace Rebar.Compiler
{
    internal class InsertDropTransform : IDfirTransform
    {
        private readonly LifetimeVariableAssociation _lifetimeVariableAssociation;
        private readonly ITypeUnificationResultFactory _unificationResultFactory;

        public InsertDropTransform(LifetimeVariableAssociation lifetimeVariableAssociation, ITypeUnificationResultFactory unificationResultFactory)
        {
            _lifetimeVariableAssociation = lifetimeVariableAssociation;
            _unificationResultFactory = unificationResultFactory;
        }

        public void Execute(DfirRoot dfirRoot, CompileCancellationToken cancellationToken)
        {
            LiveVariable liveUnboundedLifetimeVariable;
            while (_lifetimeVariableAssociation.TryGetLiveVariableWithUnboundedLifetime(out liveUnboundedLifetimeVariable))
            {
                Diagram parentDiagram = liveUnboundedLifetimeVariable.Terminal.ParentDiagram;
                InsertDropForVariable(parentDiagram, liveUnboundedLifetimeVariable);
            }
        }

        private void InsertDropForVariable(Diagram parentDiagram, LiveVariable liveUnboundedLifetimeVariable)
        {
            var drop = new DropNode(parentDiagram);
            Terminal inputTerminal = drop.InputTerminals[0];
            AutoBorrowNodeFacade nodeFacade = AutoBorrowNodeFacade.GetNodeFacade(drop);
            nodeFacade[inputTerminal] = new SimpleTerminalFacade(
                inputTerminal,
                parentDiagram.GetTypeVariableSet().CreateReferenceToNewTypeVariable());

            liveUnboundedLifetimeVariable.ConnectToTerminalAsInputAndUnifyVariables(inputTerminal, _unificationResultFactory);
            _lifetimeVariableAssociation.MarkVariableConsumed(liveUnboundedLifetimeVariable.Variable);
        }
    }
}
