using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.Dfir;
using Rebar.Compiler.Nodes;
using Rebar.RebarTarget;
using Rebar.RebarTarget.Execution;
using Tests.Rebar.Unit.Compiler;

namespace Tests.Rebar.Unit.Execution
{
    public abstract class ExecutionTestBase : CompilerTestBase
    {
        protected ExecutionContext CompileAndExecuteFunction(DfirRoot function)
        {
            Function compiledFunction = RunSemanticAnalysisUpToCodeGeneration(function);
            ExecutionContext context = new ExecutionContext(new TestRuntimeServices());
            context.LoadFunction(compiledFunction);
            context.FinalizeLoad();
            context.ExecuteFunctionTopLevel(compiledFunction.Name);
            return context;
        }

        internal byte[] GetLastValueFromInspectNode(ExecutionContext context, FunctionalNode inspectNode)
        {
            return context.ReadStaticData(StaticDataIdentifier.CreateFromNode(inspectNode));
        }

        protected void AssertByteArrayIsInt32(byte[] region, int value)
        {
            Assert.AreEqual(4, region.Length);
            Assert.AreEqual(value, DataHelpers.ReadIntFromByteArray(region, 0));
        }
    }

    internal class TestRuntimeServices : IRebarTargetRuntimeServices
    {
        public void Output(string value)
        {
        }
    }
}
