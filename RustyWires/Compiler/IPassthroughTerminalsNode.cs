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
        public PassthroughTerminalPair(Terminal inputTerminal, Terminal outputTerminal)
        {
            InputTerminal = inputTerminal;
            OutputTerminal = outputTerminal;
        }

        public Terminal InputTerminal { get; }

        public Terminal OutputTerminal { get; }
    }
}
