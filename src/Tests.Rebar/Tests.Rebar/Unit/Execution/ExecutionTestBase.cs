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
        internal TestExecutionInstance CompileAndExecuteFunction(DfirRoot function)
        {
            var testExecutionInstance = new TestExecutionInstance();
            testExecutionInstance.CompileAndExecuteFunction(this, function);
            return testExecutionInstance;
        }

        internal FunctionalNode ConnectInspectToOutputTerminal(Terminal outputTerminal)
        {
            FunctionalNode inspect = new FunctionalNode(outputTerminal.ParentDiagram, Signatures.InspectType);
            Wire.Create(outputTerminal.ParentDiagram, outputTerminal, inspect.InputTerminals[0]);
            return inspect;
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
