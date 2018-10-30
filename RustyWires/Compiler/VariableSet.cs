using System;
using System.Collections.Generic;
using System.Linq;
using NationalInstruments.Dfir;

namespace RustyWires.Compiler
{
    internal sealed class VariableSet
    {
        private readonly List<Variable> _variables = new List<Variable>();
        private readonly Dictionary<Terminal, Variable> _terminalVariables = new Dictionary<Terminal, Variable>();
        private readonly Dictionary<Lifetime, List<Variable>> _variablesInterruptedByLifetimes = new Dictionary<Lifetime, List<Variable>>();
        private readonly BoundedLifetimeGraph _boundedLifetimeGraph = new BoundedLifetimeGraph();

        private Variable CreateNewVariable(Terminal originatingTerminal)
        {
            var variable = new Variable(_variables.Count, originatingTerminal);
            _variables.Add(variable);
            return variable;
        }

        public IEnumerable<Variable> Variables => _variables;

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

        public void MergeVariables(Variable toMerge, Variable mergeWith)
        {
            if (!_variables.Contains(mergeWith))
            {
                throw new ArgumentException(nameof(mergeWith));
            }
            List<Terminal> terminalsToMerge = _terminalVariables.Where(pair => pair.Value == toMerge).Select(pair => pair.Key).ToList();
            terminalsToMerge.ForEach(terminal => _terminalVariables[terminal] = mergeWith);
            _variables.Remove(toMerge);
        }

        public IEnumerable<Variable> GetVariablesInterruptedByLifetime(Lifetime lifetime)
        {
            List<Variable> variables;
            if (_variablesInterruptedByLifetimes.TryGetValue(lifetime, out variables))
            {
                return variables;
            }
            return Enumerable.Empty<Variable>();
        }

        public VariableUsageValidator GetValidatorForTerminal(Terminal terminal)
        {
            return new VariableUsageValidator(GetVariableForTerminal(terminal), terminal);
        }

        public Lifetime DefineLifetimeThatOutlastsDiagram()
        {
            return _boundedLifetimeGraph.CreateLifetimeThatOutlastsDiagram();
        }

        public Lifetime DefineLifetimeThatIsBoundedByDiagram(IEnumerable<Variable> decomposedVariables)
        {
            Lifetime lifetime = _boundedLifetimeGraph.CreateLifetimeThatIsBoundedByDiagram();
            _variablesInterruptedByLifetimes.Add(lifetime, decomposedVariables.ToList());
            return lifetime;
        }

        public Lifetime ComputeCommonLifetime(Lifetime left, Lifetime right)
        {
            if (_boundedLifetimeGraph.DoesOutlast(left, right))
            {
                return right;
            }
            if (_boundedLifetimeGraph.DoesOutlast(right, left))
            {
                return left;
            }
            return Lifetime.Empty;
        }
    }

    internal static class VariableSetExtensions
    {
        private static readonly AttributeDescriptor _variableSetTokenName = new AttributeDescriptor("RustyWires.Compiler.VariableSet", false);

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

        public static Variable GetVariable(this Terminal terminal)
        {
            return terminal.GetVariableSet().GetVariableForTerminal(terminal);
        }

        public static void AddTerminalToVariable(this Terminal terminal, Variable variable)
        {
            // TODO: validate that variable is part of terminal.GetVariableSet()
            terminal.GetVariableSet().AddTerminalToVariable(variable, terminal);
        }

        public static Variable AddTerminalToNewVariable(this Terminal terminal)
        {
            return terminal.GetVariableSet().AddTerminalToNewVariable(terminal);
        }

        public static VariableUsageValidator GetValidator(this Terminal terminal)
        {
            return terminal.GetVariableSet().GetValidatorForTerminal(terminal);
        }
    }
}
