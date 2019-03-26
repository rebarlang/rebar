using NationalInstruments.Dfir;
using Rebar.Common;

namespace Rebar.Compiler
{
    internal static class VariableExtensions
    {
        private static readonly AttributeDescriptor _variableSetTokenName = new AttributeDescriptor("Rebar.Compiler.VariableSet", true);

        public static void SetVariableSet(this Diagram diagram, VariableSet variableSet)
        {
            var token = diagram.DfirRoot.GetOrCreateNamedSparseAttributeToken<VariableSet>(_variableSetTokenName);
            diagram.SetAttribute(token, variableSet);
        }

        public static VariableSet GetVariableSet(this Diagram diagram)
        {
            var token = diagram.DfirRoot.GetOrCreateNamedSparseAttributeToken<VariableSet>(_variableSetTokenName);
            return token.GetAttribute(diagram);
        }

        public static VariableSet GetVariableSet(this Terminal terminal)
        {
            return terminal.ParentDiagram.GetVariableSet();
        }

        /// <summary>
        /// Gets the true <see cref="VariableReference"/> associated with the <see cref="Terminal"/>, i.e., the reference
        /// to the variable that will be supplied directly to the terminal as input.
        /// </summary>
        /// <param name="terminal">The terminal.</param>
        /// <returns>The true <see cref="VariableReference"/>.</returns>
        public static VariableReference GetTrueVariable(this Terminal terminal)
        {
            TerminalFacade terminalFacade = AutoBorrowNodeFacade.GetNodeFacade(terminal.ParentNode)[terminal];
            return terminalFacade?.TrueVariable ?? new VariableReference();
        }

        /// <summary>
        /// Gets the facade <see cref="VariableReference"/> associated with the <see cref="Terminal"/>, i.e., the reference
        /// to the variable seen from the outside to be associated with the terminal. This may be different from the 
        /// terminal's true variable as a result of auto-borrowing.
        /// </summary>
        /// <param name="terminal">The terminal.</param>
        /// <returns>The true <see cref="VariableReference"/>.</returns>
        public static VariableReference GetFacadeVariable(this Terminal terminal)
        {
            TerminalFacade terminalFacade = AutoBorrowNodeFacade.GetNodeFacade(terminal.ParentNode)[terminal];
            return terminalFacade?.FacadeVariable ?? new VariableReference();
        }

        public static VariableUsageValidator GetValidator(this Terminal terminal)
        {
            return new VariableUsageValidator(terminal);
        }
    }
}
