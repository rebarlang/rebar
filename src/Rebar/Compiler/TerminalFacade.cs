using NationalInstruments.Dfir;
using Rebar.Common;

namespace Rebar.Compiler
{
    /// <summary>
    /// Represents a pair of <see cref="VariableReference"/>s for a <see cref="Terminal"/>, the true and facade variables.
    /// Depending on the behavior of the facade, these may be the same, may be different, or may start out different and be merged.
    /// Input to the <see cref="Terminal"/> always comes into the facade variable, and the parent node does type calculations using
    /// the true variable; the <see cref="TerminalFacade"/> knows whether to merge the two or not based on the input.
    /// </summary>
    internal abstract class TerminalFacade
    {
        /// <summary>
        /// Construct a new <see cref="TerminalFacade"/> for the given <see cref="Terminal"/>.
        /// </summary>
        /// <param name="terminal">The <see cref="Terminal"/> to create a facade for.</param>
        protected TerminalFacade(Terminal terminal)
        {
            Terminal = terminal;
        }

        public Terminal Terminal { get; }

        public abstract VariableReference FacadeVariable { get; }

        public abstract VariableReference TrueVariable { get; }

        public abstract void UpdateFromFacadeInput();
    }
}
