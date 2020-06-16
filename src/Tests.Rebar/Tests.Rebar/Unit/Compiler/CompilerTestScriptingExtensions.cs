using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler.Nodes;
using Tests.Rebar.Unit.Compiler;

namespace Tests.Rebar
{
    internal static class CompilerTestScriptingExtensions
    {
        public static FunctionalNode ConnectOutputToOutputTerminal(this CompilerTestBase test, Terminal outputTerminal)
        {
            var output = new FunctionalNode(outputTerminal.ParentDiagram, Signatures.OutputType);
            Wire.Create(outputTerminal.ParentDiagram, outputTerminal, output.InputTerminals[0]);
            return output;
        }

        public static VariantMatchStructure CreateVariantMatchStructure(this CompilerTestBase test, Diagram parentDiagram, int diagramCount)
        {
            var variantMatchStructure = new VariantMatchStructure(parentDiagram);
            for (int i = 0; i < diagramCount - 1; ++i)
            {
                variantMatchStructure.CreateDiagram();
            }
            return variantMatchStructure;
        }
    }
}
