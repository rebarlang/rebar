using NationalInstruments.Dfir;
using Rebar.Common;

namespace Rebar.Compiler
{
    /// <summary>
    /// <see cref="TerminalFacade"/> implementation for output terminals that will terminate the lifetime started by
    /// a related auto-borrowed input terminal. Its variables are identical to the corresponding variables of the related input.
    /// </summary>
    internal class TerminateLifetimeOutputTerminalFacade : TerminalFacade
    {
        public TerminateLifetimeOutputTerminalFacade(Terminal terminal, TerminalFacade inputFacade)
            : base(terminal)
        {
            InputFacade = inputFacade;
        }

        public override VariableReference FacadeVariable => InputFacade.FacadeVariable;

        public override VariableReference TrueVariable => InputFacade.TrueVariable;

        public TerminalFacade InputFacade { get; }

        public override void UpdateFromFacadeInput()
        {
        }
    }
}
