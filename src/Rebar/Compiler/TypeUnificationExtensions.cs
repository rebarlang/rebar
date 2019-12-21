using NationalInstruments.Dfir;
using Rebar.Common;

namespace Rebar.Compiler
{
    internal static class TypeUnificationExtensions
    {
        public static void UnifyNodeInputTerminalTypes(this Node node, ITypeUnificationResultFactory typeUnificationResultFactory)
        {
            AutoBorrowNodeFacade nodeFacade = AutoBorrowNodeFacade.GetNodeFacade(node);
            foreach (var nodeTerminal in node.InputTerminals)
            {
                var connectedWireTerminal = nodeTerminal.ConnectedTerminal;
                VariableReference unifyWithVariable = connectedWireTerminal != null
                    // Unify node input terminal with its connected source
                    ? connectedWireTerminal.GetFacadeVariable()
                    // Unify node input with immutable Void type
                    : nodeTerminal.CreateNewVariableForUnwiredTerminal();
                nodeFacade[nodeTerminal].UnifyWithConnectedWireTypeAsNodeInput(unifyWithVariable, typeUnificationResultFactory);
            }
        }

        public static void UnifyWireInputTerminalTypes(this Wire wire, TerminalTypeUnificationResults typeUnificationResults)
        {
            // Merge the wire's input terminal with its connected source
            foreach (var wireTerminal in wire.InputTerminals)
            {
                var connectedNodeTerminal = wireTerminal.ConnectedTerminal;
                if (connectedNodeTerminal != null)
                {
                    VariableReference wireVariable = wireTerminal.GetFacadeVariable(),
                        nodeVariable = connectedNodeTerminal.GetFacadeVariable();
                    wireTerminal.UnifyTerminalTypeWith(
                        wireVariable.TypeVariableReference,
                        nodeVariable.TypeVariableReference,
                        typeUnificationResults);
                    wireVariable.MergeInto(nodeVariable);
                }
            }
        }
    }
}
