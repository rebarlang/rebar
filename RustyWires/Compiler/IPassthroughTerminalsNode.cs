using System.Collections.Generic;
using NationalInstruments.Dfir;

namespace RustyWires.Compiler
{
    internal interface IPassthroughTerminalsNode
    {
        IEnumerable<PassthroughTerminalPair> PassthroughTerminalPairs { get; }
    }

    internal struct PassthroughTerminalPair
    {
        public PassthroughTerminalPair(
            Terminal inputTerminal,
            Terminal outputTerminal,
            bool relatedToOutParameters = false)
        {
            InputTerminal = inputTerminal;
            OutputTerminal = outputTerminal;
            RelatedToOutParameters = relatedToOutParameters;
        }

        public Terminal InputTerminal { get; }

        public Terminal OutputTerminal { get; }

        /// <summary>
        /// Whether the parameter for the terminal pair is related by lifetime to any out
        /// parameters on the same node. If it is, then the two terminals cannot be considered
        /// the same variable until the input type is known.
        /// </summary>
        public bool RelatedToOutParameters { get; }
    }
}
