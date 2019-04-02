using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler.Nodes;

namespace Rebar.Compiler
{
    internal class ReflectVariablesToTerminalsTransform : VisitorTransformBase
    {
        protected override void VisitNode(Node node)
        {
            ReflectAllTerminalTypes(node);
        }

        protected override void VisitWire(Wire wire)
        {
        }

        protected override void VisitBorderNode(NationalInstruments.Dfir.BorderNode borderNode)
        {
            ReflectAllTerminalTypes(borderNode);
        }

        private void ReflectAllTerminalTypes(Node node)
        {
            foreach (Terminal terminal in node.Terminals)
            {
                if (terminal.ParentNode is TerminateLifetimeTunnel && terminal.Direction == Direction.Input)
                {
                    // HACK
                    continue;
                }
                VariableReference variable = terminal.GetFacadeVariable();
                terminal.DataType = !variable.Type.IsUnset() ? variable.Type : PFTypes.Void;
            }
        }
    }
}
