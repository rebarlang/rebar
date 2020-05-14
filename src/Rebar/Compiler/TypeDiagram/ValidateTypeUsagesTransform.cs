using System;
using NationalInstruments.Compiler.SemanticAnalysis;
using NationalInstruments.Dfir;
using Rebar.Compiler.Nodes;

namespace Rebar.Compiler.TypeDiagram
{
    internal class ValidateTypeUsagesTransform : VisitorTransformBase
    {
        protected override void VisitBorderNode(NationalInstruments.Dfir.BorderNode borderNode)
        {
            throw new NotImplementedException();
        }

        protected override void VisitNode(Node node)
        {
            var selfTypeNode = node as SelfTypeNode;
            if (selfTypeNode == null || selfTypeNode.Mode == Common.SelfTypeMode.Struct)
            {
                foreach (Terminal inputTerminal in node.InputTerminals)
                {
                    inputTerminal.TestRequiredTerminalConnected();
                }
            }
        }

        protected override void VisitWire(Wire wire)
        {
        }
    }
}
