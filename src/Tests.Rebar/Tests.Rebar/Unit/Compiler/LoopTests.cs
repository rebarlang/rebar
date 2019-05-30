using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler;
using Loop = Rebar.Compiler.Nodes.Loop;

namespace Tests.Rebar.Unit.Compiler
{
    [TestClass]
    public class LoopTests : CompilerTestBase
    {
        [TestMethod]
        public void LoopWithOutputTunnel_SetVariableTypes_OutputTunnelOutputsOptionOfInput()
        {
            DfirRoot function = DfirRoot.Create();
            Loop loop = new Loop(function.BlockDiagram);
            Tunnel outputTunnel = CreateOutputTunnel(loop);
            ConnectConstantToInputTerminal(outputTunnel.InputTerminals[0], PFTypes.Int32, false);

            RunSemanticAnalysisUpToSetVariableTypes(function);

            VariableReference tunnelOutputVariable = outputTunnel.OutputTerminals[0].GetTrueVariable();
            NIType optionInnerType;
            Assert.IsTrue(tunnelOutputVariable.Type.TryDestructureOptionType(out optionInnerType));
            Assert.IsTrue(optionInnerType.IsInt32());
        }
    }
}
