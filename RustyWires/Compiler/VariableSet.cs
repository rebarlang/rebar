using NationalInstruments.Dfir;
using System.Collections.Generic;

namespace RustyWires.Compiler
{
    internal sealed class VariableSet
    {
        private readonly Dictionary<Wire, Variable> _wireVariables = new Dictionary<Wire, Variable>();
        private readonly Dictionary<Terminal, Variable> _terminalVariables = new Dictionary<Terminal, Variable>();
        private int _setCount = 0;

        private Variable CreateNewVariable(Terminal originatingTerminal)
        {
            return new Variable(_setCount++, originatingTerminal);
        }

        public Variable AddTerminalToNewVariable(Terminal terminal)
        {
            Variable set = CreateNewVariable(terminal);
            _terminalVariables[terminal] = set;
            return set;
        }

        public Variable GetVariableForTerminal(Terminal terminal)
        {
            Variable variable;
            _terminalVariables.TryGetValue(terminal, out variable);
            return variable;
        }

        public void AddTerminalToVariable(Variable variable, Terminal terminal)
        {
            _terminalVariables[terminal] = variable;
        }

        public Variable GetVariableForWire(Wire wire)
        {
            Variable variable;
            _wireVariables.TryGetValue(wire, out variable);
            return variable;
        }

        public void AddWireToVariable(Variable variable, Wire wire)
        {
            _wireVariables[wire] = variable;
            variable.Wires.Add(wire);
        }

        public VariableUsageValidator GetValidatorForTerminal(Terminal terminal)
        {
            return new VariableUsageValidator(GetVariableForTerminal(terminal), terminal);
        }
    }

    internal static class VariableSetExtensions
    {
        private static readonly AttributeDescriptor _variableSetTokenName = new AttributeDescriptor("RustyWires.Compiler.VariableSet", false);

        public static void SetVariableSet(this DfirRoot dfirRoot, VariableSet variableSet)
        {
            var token = dfirRoot.GetOrCreateNamedSparseAttributeToken<VariableSet>(_variableSetTokenName);
            dfirRoot.SetAttribute(token, variableSet);
        }

        public static VariableSet GetVariableSet(this DfirRoot dfirRoot)
        {
            var token = dfirRoot.GetOrCreateNamedSparseAttributeToken<VariableSet>(_variableSetTokenName);
            return token.GetAttribute(dfirRoot);
        }
    }
}
