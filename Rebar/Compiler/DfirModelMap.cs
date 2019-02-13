using System;
using System.Collections.Generic;
using System.Linq;
using NationalInstruments.Compiler;
using NationalInstruments.SourceModel;
using SMElement = NationalInstruments.SourceModel.Element;
using SMTerminal = NationalInstruments.SourceModel.Terminal;
using DfirNode = NationalInstruments.Dfir.Node;
using DfirTerminal = NationalInstruments.Dfir.Terminal;

namespace Rebar.Compiler
{
    internal class DfirModelMap
    {
        private readonly List<Tuple<Content, DfirNode>> _pairs = new List<Tuple<Content, DfirNode>>();
        private readonly List<Tuple<SMTerminal, DfirTerminal>> _terminalPairs = new List<Tuple<SMTerminal, DfirTerminal>>();

        public void AddMapping(Content content, DfirNode node)
        {
            _pairs.Add(new Tuple<Content, DfirNode>(content, node));
            node.SetSourceModelId(content);
        }

        public void AddMapping(SMTerminal modelTerminal, DfirTerminal dfirTerminal)
        {
            _terminalPairs.Add(new Tuple<SMTerminal, DfirTerminal>(modelTerminal, dfirTerminal));
            dfirTerminal.SetSourceModelId(modelTerminal);
        }

        public DfirNode GetDfirForModel(SMElement model)
        {
            return _pairs.FirstOrDefault(pair => pair.Item1 == model).Item2;
        }

        public DfirTerminal GetDfirForTerminal(SMTerminal terminal)
        {
            return _terminalPairs.FirstOrDefault(pair => pair.Item1 == terminal).Item2;
        }
    }
}
