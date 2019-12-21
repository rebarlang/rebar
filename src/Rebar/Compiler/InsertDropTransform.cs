using System.Linq;
using NationalInstruments.Compiler;
using NationalInstruments.DataTypes;
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
                if (liveUnboundedLifetimeVariable.Variable.Type.IsCluster())
                {
                    DecomposeTupleNode decomposeTuple = InsertDecompositionForTupleVariable(
                        parentDiagram,
                        liveUnboundedLifetimeVariable,
                        _unificationResultFactory);
                    foreach (Terminal outputTerminal in decomposeTuple.OutputTerminals)
                    {
                        _lifetimeVariableAssociation.MarkVariableLive(outputTerminal.GetFacadeVariable(), outputTerminal);
                    }
                    _lifetimeVariableAssociation.MarkVariableConsumed(liveUnboundedLifetimeVariable.Variable);
                    continue;
                }
                InsertDropForVariable(parentDiagram, liveUnboundedLifetimeVariable, _unificationResultFactory);
                _lifetimeVariableAssociation.MarkVariableConsumed(liveUnboundedLifetimeVariable.Variable);
            }
        }

        internal static void InsertDropForVariable(Diagram parentDiagram, LiveVariable liveUnboundedLifetimeVariable, ITypeUnificationResultFactory unificationResultFactory)
        {
            var drop = new DropNode(parentDiagram);
            Terminal inputTerminal = drop.InputTerminals[0];
            AutoBorrowNodeFacade nodeFacade = AutoBorrowNodeFacade.GetNodeFacade(drop);
            nodeFacade[inputTerminal] = new SimpleTerminalFacade(
                inputTerminal,
                parentDiagram.GetTypeVariableSet().CreateReferenceToNewTypeVariable());

            liveUnboundedLifetimeVariable.ConnectToTerminalAsInputAndUnifyVariables(inputTerminal, unificationResultFactory);
        }

        internal static DecomposeTupleNode InsertDecompositionForTupleVariable(Diagram parentDiagram, LiveVariable liveTupleVariable, ITypeUnificationResultFactory unificationResultFactory)
        {
            NIType variableType = liveTupleVariable.Variable.Type;
            DecomposeTupleNode decomposeTuple = TupleNodeHelpers.CreateDecomposeTupleNodeWithFacades(
                parentDiagram,
                variableType.GetFields().Count(),
                DecomposeMode.Move);

            Terminal tupleInputTerminal = decomposeTuple.InputTerminals[0];
            liveTupleVariable.ConnectToTerminalAsInputAndUnifyVariables(
                tupleInputTerminal,
                unificationResultFactory);
            return decomposeTuple;
        }
    }
}
