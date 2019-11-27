using System.Collections.Generic;
using System.Linq;
using NationalInstruments.Compiler;
using NationalInstruments.DataTypes;
using DfirDiagram = NationalInstruments.Dfir.Diagram;
using DfirTerminal = NationalInstruments.Dfir.Terminal;
using SMTerminal = NationalInstruments.SourceModel.Terminal;
using DfirWire = NationalInstruments.Dfir.Wire;
using SMWire = NationalInstruments.SourceModel.Wire;

namespace Rebar.Compiler
{
    internal static class DfirTranslationHelpers
    {
        public static DfirWire TranslateModelWire(this DfirModelMap dfirModelMap, SMWire wire)
        {
            var connectedDfirTerminals = new List<DfirTerminal>();
            var looseEnds = new List<SMTerminal>();
            foreach (SMTerminal terminal in wire.Terminals)
            {
                if (terminal.ConnectedTerminal != null)
                {
                    connectedDfirTerminals.Add(dfirModelMap.GetDfirForTerminal(terminal.ConnectedTerminal));
                }
                else
                {
                    looseEnds.Add(terminal);
                }
            }

            var parentDiagram = (DfirDiagram)dfirModelMap.GetDfirForModel(wire.Owner);
            DfirWire dfirWire = DfirWire.Create(parentDiagram, connectedDfirTerminals);
            dfirModelMap.AddMapping(wire, dfirWire);
            int i = 0;
            // Map connected model wire terminals
            foreach (SMTerminal terminal in wire.Terminals.Where(t => t.ConnectedTerminal != null))
            {
                dfirModelMap.MapTerminalAndType(terminal, dfirWire.Terminals[i]);
                i++;
            }
            // Map unconnected model wire terminals
            foreach (SMTerminal terminal in looseEnds)
            {
                DfirTerminal dfirTerminal = dfirWire.CreateBranch();
                dfirModelMap.MapTerminalAndType(terminal, dfirTerminal);
            }
            // "Map" loose ends with no terminals in the model
            int numberOfLooseEndsInModel = wire.Joints.Count(j => j.Dangling);
            for (int looseEndsIndex = 0; looseEndsIndex < numberOfLooseEndsInModel; ++looseEndsIndex)
            {
                DfirTerminal dfirTerminal = dfirWire.CreateBranch();
                dfirTerminal.DataType = PFTypes.Void;
            }
            return dfirWire;
        }

        private static void MapTerminalAndType(
            this DfirModelMap dfirModelMap,
            SMTerminal modelTerminal,
            DfirTerminal dfirTerminal)
        {
            dfirModelMap.AddMapping(modelTerminal, dfirTerminal);
            dfirTerminal.SetSourceModelId(modelTerminal);
            dfirTerminal.DataType = modelTerminal.DataType.IsUnset() ? PFTypes.Void : modelTerminal.DataType;
        }
    }
}
