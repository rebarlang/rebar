using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler.Nodes;

namespace Rebar.Compiler
{
    internal static class TerminateLifetimeNodeHelpers
    {
        public static TerminateLifetimeNode CreateTerminateLifetimeWithFacades(Diagram parentDiagram, int inputs, int outputs)
        {
            var terminateLifetime = new TerminateLifetimeNode(parentDiagram, inputs, outputs);
            AutoBorrowNodeFacade terminateLifetimeFacade = AutoBorrowNodeFacade.GetNodeFacade(terminateLifetime);
            foreach (var terminal in terminateLifetime.Terminals)
            {
                terminateLifetimeFacade[terminal] = new SimpleTerminalFacade(terminal, default(TypeVariableReference));
            }
            return terminateLifetime;
        }
    }
}
