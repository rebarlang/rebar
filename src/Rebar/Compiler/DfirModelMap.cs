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
        private readonly HashSet<SMTerminal> _unmappedModelTerminals = new HashSet<SMTerminal>();

        public void AddMapping(Content content, DfirElement dfirElement)
        {
            _pairs.Add(new Tuple<Content, DfirElement>(content, dfirElement));
            dfirElement.SetSourceModelIds(content);
        }

        public void AddMapping(SMTerminal modelTerminal, DfirTerminal dfirTerminal)
        {
            _terminalPairs.Add(new Tuple<SMTerminal, DfirTerminal>(modelTerminal, dfirTerminal));
            var contentOwner = modelTerminal.Owner as Content;
            if (contentOwner != null)
            {
                dfirTerminal.SetSourceModelIds(contentOwner, modelTerminal.TerminalIdentifier);
            }
        }

        public void AddUnmappedSourceModelTerminal(SMTerminal modelTerminal) => _unmappedModelTerminals.Add(modelTerminal);

        public DfirElement GetDfirForModel(SMElement model) => _pairs.FirstOrDefault(pair => pair.Item1 == model).Item2;

        public DfirTerminal GetDfirForTerminal(SMTerminal terminal) => _terminalPairs.FirstOrDefault(pair => pair.Item1 == terminal).Item2;

        public bool IsUnmappedSourceModelTerminal(SMTerminal modelTerminal) => _unmappedModelTerminals.Contains(modelTerminal);

        public bool ContainsTerminal(SMTerminal terminal) => _terminalPairs.Any(pair => pair.Item1 == terminal);
    }
}
