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
                if (liveUnboundedLifetimeVariable.Variable.Type.IsCluster())
                {
                    InsertDecompositionForTupleVariable(parentDiagram, liveUnboundedLifetimeVariable);
                    continue;
                }
                InsertDropForVariable(parentDiagram, liveUnboundedLifetimeVariable);
            }
        }

        private void InsertDropForVariable(Diagram parentDiagram, LiveVariable liveUnboundedLifetimeVariable)
        {
            var drop = new DropNode(parentDiagram);
            Terminal inputTerminal = drop.InputTerminals[0];
            AutoBorrowNodeFacade nodeFacade = AutoBorrowNodeFacade.GetNodeFacade(drop);
            nodeFacade[inputTerminal] = new SimpleTerminalFacade(inputTerminal, default(TypeVariableReference));

            Wire.Create(parentDiagram, liveUnboundedLifetimeVariable.Terminal, inputTerminal);
            inputTerminal.GetFacadeVariable().MergeInto(liveUnboundedLifetimeVariable.Variable);
            _lifetimeVariableAssociation.MarkVariableConsumed(liveUnboundedLifetimeVariable.Variable);
        }

        private void InsertDecompositionForTupleVariable(Diagram parentDiagram, LiveVariable liveTupleVariable)
        {
            NIType variableType = liveTupleVariable.Variable.Type;
            DecomposeTupleNode decomposeTuple = TupleNodeHelpers.CreateDecomposeTupleNodeWithFacades(
                parentDiagram,
                variableType.GetFields().Count(),
                DecomposeMode.Move);

            Terminal tupleInputTerminal = decomposeTuple.InputTerminals[0];
            Wire.Create(parentDiagram, liveTupleVariable.Terminal, tupleInputTerminal);
            tupleInputTerminal.GetFacadeVariable().UnifyTypeVariableInto(
                liveTupleVariable.Variable,
                new RequireSuccessTypeUnificationResult());
            _lifetimeVariableAssociation.MarkVariableConsumed(liveTupleVariable.Variable);
            foreach (Terminal outputTerminal in decomposeTuple.OutputTerminals)
            {
                _lifetimeVariableAssociation.MarkVariableLive(outputTerminal.GetFacadeVariable(), outputTerminal);
            }
        }
    }
}
