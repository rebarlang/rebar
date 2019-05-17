﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler.Nodes;
using Rebar.RebarTarget.Execution;

namespace Tests.Rebar.Unit.Execution
{
    [TestClass]
    public class StringExecutionTests : ExecutionTestBase
    {
        [TestMethod]
        public void StringSliceConstantToOutput_Execute_CorrectStringOutput()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode output = new FunctionalNode(function.BlockDiagram, Signatures.OutputType);
            FunctionalNode stringFromSlice = new FunctionalNode(function.BlockDiagram, Signatures.StringFromSliceType);
            Wire.Create(function.BlockDiagram, stringFromSlice.OutputTerminals[1], output.InputTerminals[0]);
            Constant stringConstant = ConnectConstantToInputTerminal(stringFromSlice.InputTerminals[0], DataTypes.StringSliceType.CreateImmutableReference(), false);
            stringConstant.Value = "test";

            var runtimeServices = new TestRuntimeServices();
            ExecutionContext context = CompileAndExecuteFunction(function, runtimeServices);

            Assert.AreEqual("test", runtimeServices.LastOutputValue);
        }

        [TestMethod]
        public void StringToStringSliceToOutput_Execute_CorrectStringOutput()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode output = new FunctionalNode(function.BlockDiagram, Signatures.OutputType);
            FunctionalNode stringToSlice = new FunctionalNode(function.BlockDiagram, Signatures.StringToSliceType);
            Wire.Create(function.BlockDiagram, stringToSlice.OutputTerminals[0], output.InputTerminals[0]);
            FunctionalNode stringFromSlice = new FunctionalNode(function.BlockDiagram, Signatures.StringFromSliceType);
            Wire.Create(function.BlockDiagram, stringFromSlice.OutputTerminals[1], stringToSlice.InputTerminals[0]);
            Constant stringConstant = ConnectConstantToInputTerminal(stringFromSlice.InputTerminals[0], DataTypes.StringSliceType.CreateImmutableReference(), false);
            stringConstant.Value = "test";

            var runtimeServices = new TestRuntimeServices();
            ExecutionContext context = CompileAndExecuteFunction(function, runtimeServices);

            Assert.AreEqual("test", runtimeServices.LastOutputValue);
        }
    }
}
