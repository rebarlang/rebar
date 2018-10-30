using System.Linq;
using NationalInstruments;
using NationalInstruments.Dfir;
using RustyWires.Compiler.Nodes;
using NationalInstruments.DataTypes;

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
                constant.OutputTerminal.GetVariable()?.SetTypeAndLifetime(constant.DataType, Lifetime.Static);
            }
        }

        protected override void VisitWire(Wire wire)
        {
            if (wire.SinkTerminals.HasMoreThan(1))
            {
                Variable sourceVariable = wire.SourceTerminal.GetVariable();
                if (sourceVariable != null)
                {
                    foreach (var sinkVariable in wire.SinkTerminals.Skip(1).Select(VariableSetExtensions.GetVariable))
                    {
                        sinkVariable?.SetTypeAndLifetime(sourceVariable.Type, sourceVariable.Lifetime);
                    }
                }
            }
        }

        protected override void VisitBorderNode(BorderNode borderNode)
        {
            var rustyWiresBorderNode = borderNode as RustyWiresBorderNode;
            var tunnel = borderNode as Tunnel;
            if (rustyWiresBorderNode != null)
            {
                rustyWiresBorderNode.SetOutputVariableTypesAndLifetimes();
            }
            else if (tunnel != null)
            {
                Terminal inputTerminal = tunnel.Direction == Direction.Input ? tunnel.GetOuterTerminal() : tunnel.GetInnerTerminal();
                Terminal outputTerminal = tunnel.Direction == Direction.Input ? tunnel.GetInnerTerminal() : tunnel.GetOuterTerminal();
                Variable inputVariable = inputTerminal.GetVariable(),
                    outputVariable = outputTerminal.GetVariable();
                if (outputVariable != null)
                {
                    if (inputVariable != null)
                    {
                        // if input is unbounded/static, then output is unbounded/static
                        // if input is from outer diagram, then output is a lifetime that outlasts the inner diagram
                        // if input is from inner diagram and outlasts the inner diagram, we should be able to determine 
                        //    which outer diagram lifetime it came from
                        // otherwise, output is empty/error
                        Lifetime inputLifetime = inputVariable.Lifetime, outputLifetime;
                        if (!inputLifetime.IsBounded)
                        {
                            outputLifetime = inputLifetime;
                        }
                        else if (tunnel.Direction == Direction.Input)
                        {
                            outputLifetime = outputTerminal.GetVariableSet().DefineLifetimeThatOutlastsDiagram();
                        }
                        // else if (inputLifetime outlasts inner diagram) { outputLifetime = outer diagram origin of inputLifetime; }
                        else
                        {
                            outputLifetime = Lifetime.Empty;
                        }
                        outputVariable.SetTypeAndLifetime(inputVariable.Type, outputLifetime);
                    }
                    else
                    {
                        outputVariable.SetTypeAndLifetime(PFTypes.Void.CreateImmutableValue(), Lifetime.Unbounded);
                    }
                }
            }
        }
    }
}
