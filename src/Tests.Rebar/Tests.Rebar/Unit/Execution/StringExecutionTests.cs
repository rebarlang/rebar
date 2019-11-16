#define LLVM_TEST

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler;
using Rebar.Compiler.Nodes;
using Loop = Rebar.Compiler.Nodes.Loop;

namespace Tests.Rebar.Unit.Execution
{
    [TestClass]
    public class StringExecutionTests : ExecutionTestBase
    {
        [TestMethod]
        public void BranchedStringSliceWire_Execute_BothSinksHaveCorrectValues()
        {
            DfirRoot function = DfirRoot.Create();
            Constant stringSliceConstant = Constant.Create(function.BlockDiagram, "test", DataTypes.StringSliceType.CreateImmutableReference());
            var inspect1Node = new FunctionalNode(function.BlockDiagram, Signatures.InspectType);
            ExplicitBorrowNode borrow1 = ConnectExplicitBorrowToInputTerminals(inspect1Node.InputTerminals[0]);
            var inspect2Node = new FunctionalNode(function.BlockDiagram, Signatures.InspectType);
            ExplicitBorrowNode borrow2 = ConnectExplicitBorrowToInputTerminals(inspect2Node.InputTerminals[0]);
            Wire.Create(function.BlockDiagram, stringSliceConstant.OutputTerminal, borrow1.InputTerminals[0], borrow2.InputTerminals[0]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

#if LLVM_TEST
            const int stringSliceReferenceSize = 16;
#else
            const int stringSliceReferenceSize = 8;
#endif

            byte[] inspect1Value = executionInstance.GetLastValueFromInspectNode(inspect1Node);
            Assert.AreEqual(stringSliceReferenceSize, inspect1Value.Length);
            byte[] inspect2Value = executionInstance.GetLastValueFromInspectNode(inspect2Node);
            Assert.AreEqual(stringSliceReferenceSize, inspect2Value.Length);
            Assert.IsTrue(inspect1Value.Zip(inspect2Value, (a, b) => a == b).All(b => b));
        }

        [TestMethod]
        public void StringSliceConstantToOutput_Execute_CorrectStringOutput()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode output = new FunctionalNode(function.BlockDiagram, Signatures.OutputType);
            FunctionalNode stringFromSlice = new FunctionalNode(function.BlockDiagram, Signatures.StringFromSliceType);
            Wire.Create(function.BlockDiagram, stringFromSlice.OutputTerminals[1], output.InputTerminals[0]);
            Constant stringConstant = ConnectStringConstantToInputTerminal(stringFromSlice.InputTerminals[0], "test");

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            Assert.AreEqual("test", executionInstance.RuntimeServices.LastOutputValue);
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
            Constant stringConstant = ConnectStringConstantToInputTerminal(stringFromSlice.InputTerminals[0], "test");

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            Assert.AreEqual("test", executionInstance.RuntimeServices.LastOutputValue);
        }

        [TestMethod]
        public void StringConcat_Execute_CorrectResult()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode output = new FunctionalNode(function.BlockDiagram, Signatures.OutputType);
            FunctionalNode stringConcat = new FunctionalNode(function.BlockDiagram, Signatures.StringConcatType);
            Wire.Create(function.BlockDiagram, stringConcat.OutputTerminals[2], output.InputTerminals[0]);
            Constant stringAConstant = ConnectStringConstantToInputTerminal(stringConcat.InputTerminals[0], "stringA");
            Constant stringBConstant = ConnectStringConstantToInputTerminal(stringConcat.InputTerminals[1], "stringB");

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            Assert.AreEqual("stringAstringB", executionInstance.RuntimeServices.LastOutputValue);
        }

        [TestMethod]
        public void StringConcatOwnedStrings_Execute_CorrectResult()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode output = new FunctionalNode(function.BlockDiagram, Signatures.OutputType);
            FunctionalNode stringConcat = new FunctionalNode(function.BlockDiagram, Signatures.StringConcatType);
            Wire.Create(function.BlockDiagram, stringConcat.OutputTerminals[2], output.InputTerminals[0]);
            FunctionalNode stringFromSliceA = new FunctionalNode(function.BlockDiagram, Signatures.StringFromSliceType),
                stringFromSliceB = new FunctionalNode(function.BlockDiagram, Signatures.StringFromSliceType);
            Wire.Create(function.BlockDiagram, stringFromSliceA.OutputTerminals[1], stringConcat.InputTerminals[0]);
            Wire.Create(function.BlockDiagram, stringFromSliceB.OutputTerminals[1], stringConcat.InputTerminals[1]);
            Constant stringAConstant = ConnectStringConstantToInputTerminal(stringFromSliceA.InputTerminals[0], "stringA");
            Constant stringBConstant = ConnectStringConstantToInputTerminal(stringFromSliceB.InputTerminals[0], "stringB");

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            Assert.AreEqual("stringAstringB", executionInstance.RuntimeServices.LastOutputValue);
        }

        [TestMethod]
        public void StringAppend_Execute_CorrectResult()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode output = new FunctionalNode(function.BlockDiagram, Signatures.OutputType);
            FunctionalNode stringAppend = new FunctionalNode(function.BlockDiagram, Signatures.StringAppendType);
            Wire.Create(function.BlockDiagram, stringAppend.OutputTerminals[0], output.InputTerminals[0]);
            FunctionalNode stringFromSlice = new FunctionalNode(function.BlockDiagram, Signatures.StringFromSliceType);
            Wire stringWire = Wire.Create(function.BlockDiagram, stringFromSlice.OutputTerminals[1], stringAppend.InputTerminals[0]);
            stringWire.SetWireBeginsMutableVariable(true);
            Constant stringAConstant = ConnectStringConstantToInputTerminal(stringFromSlice.InputTerminals[0], "longString");
            Constant stringBConstant = ConnectStringConstantToInputTerminal(stringAppend.InputTerminals[1], "short");

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            Assert.AreEqual("longStringshort", executionInstance.RuntimeServices.LastOutputValue);
        }

        [TestMethod]
        public void StringSplitIteratorIntoOutput_Execute_CorrectResult()
        {
            DfirRoot function = DfirRoot.Create();
            FunctionalNode stringSliceToStringSplitIterator = new FunctionalNode(function.BlockDiagram, Signatures.StringSliceToStringSplitIteratorType);
            ConnectConstantToInputTerminal(stringSliceToStringSplitIterator.InputTerminals[0], DataTypes.StringSliceType.CreateImmutableReference(), "one two three", false);
            var loop = new Loop(function.BlockDiagram);
            LoopConditionTunnel loopCondition = CreateLoopConditionTunnel(loop);
            IterateTunnel iterateTunnel = CreateIterateTunnel(loop);
            Wire.Create(function.BlockDiagram, stringSliceToStringSplitIterator.OutputTerminals[0], iterateTunnel.InputTerminals[0])
                .SetWireBeginsMutableVariable(true);
            FunctionalNode output = new FunctionalNode(loop.Diagram, Signatures.OutputType);
            Wire.Create(loop.Diagram, iterateTunnel.OutputTerminals[0], output.InputTerminals[0]);

            TestExecutionInstance executionInstance = CompileAndExecuteFunction(function);

            Assert.AreEqual("three", executionInstance.RuntimeServices.LastOutputValue);
        }
    }
}
