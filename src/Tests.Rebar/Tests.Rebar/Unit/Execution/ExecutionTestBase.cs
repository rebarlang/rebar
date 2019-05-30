using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler.Nodes;
using Rebar.RebarTarget;
using Rebar.RebarTarget.Execution;
using Tests.Rebar.Unit.Compiler;

namespace Tests.Rebar.Unit.Execution
{
    public abstract class ExecutionTestBase : CompilerTestBase
    {
        protected ExecutionContext CompileAndExecuteFunction(DfirRoot function, IRebarTargetRuntimeServices runtimeServices = null)
        {
            runtimeServices = runtimeServices ?? new TestRuntimeServices();
            Function compiledFunction = RunSemanticAnalysisUpToCodeGeneration(function);
            ExecutionContext context = new ExecutionContext(runtimeServices);
            context.LoadFunction(compiledFunction);
            context.FinalizeLoad();
            context.ExecuteFunctionTopLevel(compiledFunction.Name);
            return context;
        }

        internal FunctionalNode ConnectInspectToOutputTerminal(Terminal outputTerminal)
        {
            FunctionalNode inspect = new FunctionalNode(outputTerminal.ParentDiagram, Signatures.InspectType);
            Wire.Create(outputTerminal.ParentDiagram, outputTerminal, inspect.InputTerminals[0]);
            return inspect;
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

        protected void AssertByteArrayIsSomeInteger(byte[] value, int intValue)
        {
            Assert.AreEqual(8, value.Length);
            Assert.AreEqual(1, DataHelpers.ReadIntFromByteArray(value, 0));
            Assert.AreEqual(intValue, DataHelpers.ReadIntFromByteArray(value, 4));
        }

        protected void AssertByteArrayIsNoneInteger(byte[] value)
        {
            Assert.AreEqual(8, value.Length);
            Assert.AreEqual(0, DataHelpers.ReadIntFromByteArray(value, 0));
            Assert.AreEqual(0, DataHelpers.ReadIntFromByteArray(value, 4));
        }
    }

    internal class TestRuntimeServices : IRebarTargetRuntimeServices
    {
        public void Output(string value)
        {
            LastOutputValue = value;
        }

        public string LastOutputValue { get; private set; }
    }
}
