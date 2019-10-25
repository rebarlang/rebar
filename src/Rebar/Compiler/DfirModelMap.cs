using System;
using System.Collections.Generic;
using System.Linq;
using NationalInstruments.Compiler;
using NationalInstruments.SourceModel;
using SMElement = NationalInstruments.SourceModel.Element;
using SMTerminal = NationalInstruments.SourceModel.Terminal;
using DfirElement = NationalInstruments.Dfir.DfirElement;
using DfirTerminal = NationalInstruments.Dfir.Terminal;

namespace Rebar.Compiler
{
    internal class DfirModelMap
    {
        private readonly List<Tuple<Content, DfirElement>> _pairs = new List<Tuple<Content, DfirElement>>();
        private readonly List<Tuple<SMTerminal, DfirTerminal>> _terminalPairs = new List<Tuple<SMTerminal, DfirTerminal>>();

        public void AddMapping(Content content, DfirElement dfirElement)
        {
            _pairs.Add(new Tuple<Content, DfirElement>(content, dfirElement));
            dfirElement.SetSourceModelId(content);
        }

        public void AddMapping(SMTerminal modelTerminal, DfirTerminal dfirTerminal)
        {
            _terminalPairs.Add(new Tuple<SMTerminal, DfirTerminal>(modelTerminal, dfirTerminal));
            dfirTerminal.SetSourceModelId(modelTerminal);
        }

        public DfirElement GetDfirForModel(SMElement model)
        {
            return _pairs.FirstOrDefault(pair => pair.Item1 == model).Item2;
        }

        public DfirTerminal GetDfirForTerminal(SMTerminal terminal)
        {
            return _terminalPairs.FirstOrDefault(pair => pair.Item1 == terminal).Item2;
        }

        public bool ContainsTerminal(SMTerminal terminal)
        {
            return _terminalPairs.Any(pair => pair.Item1 == terminal);
        }
    }
}
