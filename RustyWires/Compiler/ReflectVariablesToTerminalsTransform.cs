using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

namespace RustyWires.Compiler
{
    internal class ReflectVariablesToTerminalsTransform : VisitorTransformBase
    {
        protected override void VisitNode(Node node)
        {
            VariableSet variableSet = node.DfirRoot.GetVariableSet();
            foreach (Terminal terminal in node.Terminals)
            {
                Variable variable = variableSet.GetVariableForTerminal(terminal);
                if (variable != null)
                {
                    terminal.DataType = !variable.Type.IsUnset() ? variable.Type : PFTypes.Void;
                    terminal.SetLifetime(variable.Lifetime);
                }
                else
                {
                    terminal.DataType = PFTypes.Void;
                }
            }
        }

        protected override void VisitWire(Wire wire)
        {
        }

        protected override void VisitBorderNode(BorderNode borderNode)
        {
            VariableSet variableSet = borderNode.DfirRoot.GetVariableSet();
            foreach (Terminal terminal in borderNode.Terminals)
            {
                Variable variable = variableSet.GetVariableForTerminal(terminal);
                if (variable != null)
                {
                    terminal.DataType = !variable.Type.IsUnset() ? variable.Type : PFTypes.Void;
                    terminal.SetLifetime(variable.Lifetime);
                }
                else
                {
                    terminal.DataType = PFTypes.Void;
                }
            }
        }
    }
}
