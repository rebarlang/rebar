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
            var structFieldAccessorNode = node as StructFieldAccessorNode;
            if (structFieldAccessorNode != null)
            {
                structFieldAccessorNode.StructType = structFieldAccessorNode.StructInputTerminal.GetTrueVariable().Type.GetReferentType();
            }
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
                if (terminal.ParentNode is TerminateLifetimeTunnel && terminal.Direction == Direction.Input
                    || terminal.ParentNode is OptionPatternStructureSelector && terminal.Index >= 2)
                {
                    // HACK
                    continue;
                }
                VariableReference variable = terminal.GetFacadeVariable();
                NIType terminalType = PFTypes.Void;
                if (variable.TypeVariableReference.TypeVariableSet != null && !variable.Type.IsUnset())
                {
                    terminalType = variable.Type;
                }
                terminal.DataType = terminalType;
            }
        }
    }
}
