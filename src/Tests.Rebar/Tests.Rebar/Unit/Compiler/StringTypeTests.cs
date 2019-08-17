using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler;
using Rebar.Compiler.Nodes;

namespace Tests.Rebar.Unit.Compiler
{
    [TestClass]
    public class StringTypeTests : CompilerTestBase
    {
        [TestMethod]
        public void StringSliceConstant_SetVariableTypes_OutputsReferenceInStaticLifetime()
        {
            DfirRoot function = DfirRoot.Create();
            Constant constant = Constant.Create(function.BlockDiagram, null, DataTypes.StringSliceType.CreateImmutableReference());

            RunSemanticAnalysisUpToSetVariableTypes(function);

            VariableReference output = constant.OutputTerminal.GetTrueVariable();
            Assert.AreEqual(Lifetime.Static, output.Lifetime);
        }

        [TestMethod]
        public void StringIntoStringConcat_SetVariableTypes_StringAutoborrowedIntoStringSliceReference()
        {
            DfirRoot function = CreateStringIntoStringConcatFunction();
            var stringConcat = function.BlockDiagram.Nodes.OfType<FunctionalNode>().First(fn => fn.Signature == Signatures.StringConcatType);

            RunSemanticAnalysisUpToSetVariableTypes(function);

            VariableReference stringConcatInput = stringConcat.InputTerminals[0].GetTrueVariable();
            Assert.IsTrue(stringConcatInput.Type.IsImmutableReferenceType());
            Assert.AreEqual(DataTypes.StringSliceType, stringConcatInput.Type.GetReferentType());
        }

        [TestMethod]
        public void StringIntoStringConcat_Validate_NoTypeMismatchError()
        {
            DfirRoot function = CreateStringIntoStringConcatFunction();
            var stringConcat = function.BlockDiagram.Nodes.OfType<FunctionalNode>().First(fn => fn.Signature == Signatures.StringConcatType);
            RunSemanticAnalysisUpToValidation(function);

            this.AssertTerminalDoesNotHaveTypeConflictMessage(stringConcat.InputTerminals[0]);
        }

        private DfirRoot CreateStringIntoStringConcatFunction()
        {
            DfirRoot function = DfirRoot.Create();
            Constant constant = Constant.Create(function.BlockDiagram, string.Empty, DataTypes.StringSliceType.CreateImmutableReference());
            FunctionalNode stringFromSlice = new FunctionalNode(function.BlockDiagram, Signatures.StringFromSliceType);
            Wire.Create(function.BlockDiagram, constant.OutputTerminal, stringFromSlice.InputTerminals[0]);
            FunctionalNode stringConcat = new FunctionalNode(function.BlockDiagram, Signatures.StringConcatType);
            Wire.Create(function.BlockDiagram, stringFromSlice.OutputTerminals[1], stringConcat.InputTerminals[0]);
            return function;
        }
    }
}
