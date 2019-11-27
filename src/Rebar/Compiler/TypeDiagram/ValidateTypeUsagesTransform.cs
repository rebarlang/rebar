using System;
using NationalInstruments.Compiler.SemanticAnalysis;
using NationalInstruments.Dfir;

namespace Rebar.Compiler.TypeDiagram
{
    internal class ValidateTypeUsagesTransform : VisitorTransformBase
    {
        protected override void VisitBorderNode(BorderNode borderNode)
        {
            throw new NotImplementedException();
        }

        protected override void VisitNode(Node node)
        {
            foreach (Terminal inputTerminal in node.InputTerminals)
            {
                inputTerminal.TestRequiredTerminalConnected();
            }
        }

        protected override void VisitWire(Wire wire)
        {
        }
    }
}
