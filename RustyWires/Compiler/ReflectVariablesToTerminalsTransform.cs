using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;

namespace RustyWires.Compiler
{
    internal class ReflectVariablesToTerminalsTransform : VisitorTransformBase
    {
        protected override void VisitNode(Node node)
        {
            foreach (Terminal terminal in node.Terminals)
            {
                Variable variable = terminal.GetVariable();
                terminal.DataType = variable != null && !variable.Type.IsUnset() ? variable.Type : PFTypes.Void;
            }
        }

        protected override void VisitWire(Wire wire)
        {
        }

        protected override void VisitBorderNode(BorderNode borderNode)
        {
            foreach (Terminal terminal in borderNode.Terminals)
            {
                Variable variable = terminal.GetVariable();
                terminal.DataType = variable != null && !variable.Type.IsUnset() ? variable.Type : PFTypes.Void;
            }
        }
    }
}
