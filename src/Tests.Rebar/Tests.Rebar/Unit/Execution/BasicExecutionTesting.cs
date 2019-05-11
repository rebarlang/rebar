using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler.Nodes;
using Rebar.RebarTarget.Execution;

namespace Tests.Rebar.Unit.Execution
{
    [TestClass]
    public class BasicExecutionTesting : ExecutionTestBase
    {
        [TestMethod]
        public void FunctionWithInt32Constant_Execute_CorrectValue()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode inspect = new FunctionalNode(function.BlockDiagram, Signatures.InspectType);
            Constant constant = ConnectConstantToInputTerminal(inspect.InputTerminals[0], PFTypes.Int32, false);
            constant.Value = 1;

            ExecutionContext context = CompileAndExecuteFunction(function);

            byte[] inspectValue = GetLastValueFromInspectNode(context, inspect);
            AssertByteArrayIsInt32(inspectValue, 1);
        }

        [TestMethod]
        public void FunctionWithInt32AssignedToNewValue_Execute_CorrectFinalValue()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode inspect = new FunctionalNode(function.BlockDiagram, Signatures.InspectType);
            FunctionalNode assign = new FunctionalNode(function.BlockDiagram, Signatures.AssignType);
            Wire.Create(function.BlockDiagram, assign.OutputTerminals[0], inspect.InputTerminals[0]);
            Constant finalValue = ConnectConstantToInputTerminal(assign.InputTerminals[1], PFTypes.Int32, false);
            finalValue.Value = 2;
            Constant initialValue = ConnectConstantToInputTerminal(assign.InputTerminals[0], PFTypes.Int32, true);

            ExecutionContext context = CompileAndExecuteFunction(function);

            byte[] inspectValue = GetLastValueFromInspectNode(context, inspect);
            AssertByteArrayIsInt32(inspectValue, 2);
        }
    }
}
