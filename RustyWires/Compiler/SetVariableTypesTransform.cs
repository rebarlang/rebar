using System.Linq;
using NationalInstruments;
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
            if (wire.SinkTerminals.HasMoreThan(1))
            {
                VariableSet variableSet = wire.DfirRoot.GetVariableSet();
                Variable sourceVariable = variableSet.GetVariableForTerminal(wire.SourceTerminal);
                if (sourceVariable != null)
                {
                    foreach (var sinkVariable in wire.SinkTerminals.Skip(1).Select(variableSet.GetVariableForTerminal))
                    {
                        sinkVariable?.SetType(sourceVariable.Type);
                        sinkVariable?.SetLifetime(sourceVariable.Lifetime);
                    }
                }
            }
        }

        protected override void VisitBorderNode(BorderNode borderNode)
        {
            var rustyWiresBorderNode = borderNode as RustyWiresBorderNode;
            rustyWiresBorderNode?.SetOutputVariableTypesAndLifetimes();
        }
    }
}
