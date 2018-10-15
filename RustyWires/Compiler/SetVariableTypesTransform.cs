using NationalInstruments.Dfir;
using RustyWires.Compiler.Nodes;

namespace RustyWires.Compiler
{
    internal class SetVariableTypesAndLifetimesTransform : VisitorTransformBase
    {
        protected override void VisitNode(Node node)
        {
            RustyWiresDfirNode rustyWiresDfirNode = node as RustyWiresDfirNode;
            Constant constant = node as Constant;
            if (rustyWiresDfirNode != null)
            {
                rustyWiresDfirNode.SetOutputVariableTypesAndLifetimes();
            }
            else if (constant != null)
            {
                VariableSet variableSet = node.DfirRoot.GetVariableSet();
                Variable constantVariable = variableSet.GetVariableForTerminal(constant.OutputTerminal);
                constantVariable?.SetType(constant.DataType);
                constantVariable?.SetLifetime(node.DfirRoot.GetLifetimeSet().StaticLifetime);
            }
        }

        protected override void VisitWire(Wire wire)
        {
        }

        protected override void VisitBorderNode(BorderNode borderNode)
        {
            var rustyWiresBorderNode = borderNode as RustyWiresBorderNode;
            rustyWiresBorderNode?.SetOutputVariableTypesAndLifetimes();
        }
    }
}
