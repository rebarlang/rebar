using NationalInstruments.Dfir;
using Rebar.Common;

namespace Rebar.Compiler
{
    internal interface ITypeUnificationResultFactory
    {
        /// <summary>
        /// Gets a <see cref="ITypeUnificationResult"/> to use for unifying a <paramref name="terminalTypeVariable"/>
        /// for <paramref name="terminal"/>and <paramref name="unifyWith"/>.
        /// </summary>
        /// <param name="terminal">The <see cref="Terminal"/> whose type variable will be unified.</param>
        /// <param name="terminalTypeVariable">The <see cref="TypeVariableReference"/> for the terminal.</param>
        /// <param name="unifyWith">The <see cref="TypeVariableReference"/> that will be unified with.</param>
        /// <returns>An <see cref="ITypeUnificationResult"/> for unifying the type variables.</returns>
        ITypeUnificationResult GetTypeUnificationResult(Terminal terminal, TypeVariableReference terminalTypeVariable, TypeVariableReference unifyWith);
    }
}
