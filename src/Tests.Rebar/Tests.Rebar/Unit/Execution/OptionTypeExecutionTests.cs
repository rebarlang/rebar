using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler.Nodes;
using Rebar.RebarTarget.Execution;

namespace Tests.Rebar.Unit.Execution
{
    [TestClass]
    public class OptionTypeExecutionTests : ExecutionTestBase
    {
        [TestMethod]
        public void IsSomeWithSomeValueInput_Execute_IsSomeOutputsTrue()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode inspect = new FunctionalNode(function.BlockDiagram, Signatures.InspectType);
            FunctionalNode isSome = new FunctionalNode(function.BlockDiagram, Signatures.IsSomeType);
            Wire.Create(function.BlockDiagram, isSome.OutputTerminals[1], inspect.InputTerminals[0]);
            FunctionalNode some = new FunctionalNode(function.BlockDiagram, Signatures.SomeConstructorType);
            Wire.Create(function.BlockDiagram, some.OutputTerminals[0], isSome.InputTerminals[0]);
            ConnectConstantToInputTerminal(some.InputTerminals[0], PFTypes.Int32, false);

            ExecutionContext context = CompileAndExecuteFunction(function);

            byte[] lastValue = GetLastValueFromInspectNode(context, inspect);
            AssertByteArrayIsInt32(lastValue, 1);
        }
    }
}
