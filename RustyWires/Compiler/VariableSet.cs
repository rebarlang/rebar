using NationalInstruments.Dfir;
using System.Collections.Generic;

namespace RustyWires.Compiler
{
    internal sealed class VariableSet
    {
        private readonly List<Variable> _variables = new List<Variable>();
        private readonly Dictionary<Terminal, Variable> _terminalVariables = new Dictionary<Terminal, Variable>();

        private Variable CreateNewVariable(Terminal originatingTerminal)
        {
            var variable = new Variable(_variables.Count, originatingTerminal);
            _variables.Add(variable);
            return variable;
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
