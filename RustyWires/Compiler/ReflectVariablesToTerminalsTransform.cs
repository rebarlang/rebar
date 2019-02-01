using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using RustyWires.Common;

namespace RustyWires.Compiler
{
    internal class ReflectVariablesToTerminalsTransform : VisitorTransformBase
    {
        protected override void VisitNode(Node node)
        {
            foreach (Terminal terminal in node.Terminals)
            {
                terminal.DataType = GetTerminalTypeFromVariable(terminal.GetVariable());
            }
        }

        protected override void VisitWire(Wire wire)
        {
        }

        protected override void VisitBorderNode(BorderNode borderNode)
        {
            foreach (Terminal terminal in borderNode.Terminals)
            {
                terminal.DataType = GetTerminalTypeFromVariable(terminal.GetVariable());
            }
        }

        private NIType GetTerminalTypeFromVariable(Variable variable)
        {
            NIType variableType = variable != null && !variable.Type.IsUnset() ? variable.Type : PFTypes.Void;
            bool mutableVariable = variable?.Mutable ?? false;
            return variableType.IsRWReferenceType()
                ? variableType
                : variableType.CreateValue(mutableVariable);
        }
    }
}
