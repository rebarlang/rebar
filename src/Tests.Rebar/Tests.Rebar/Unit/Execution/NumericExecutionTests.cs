using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.DataTypes;
using Rebar.Common;

namespace Tests.Rebar.Unit.Execution
{
    [TestClass]
    public class NumericExecutionTests : PrimitiveOpExecutionTest
    {
        [TestMethod]
        public void AddTwoI32s_Execute_CorrectResultValue()
        {
            TestPureBinaryI32Operation(Signatures.DefinePureBinaryFunction("Add", NITypes.Int32, NITypes.Int32), 6, 5, 11);
        }

        [TestMethod]
        public void SubtractTwoI32s_Execute_CorrectResultValue()
        {
            TestPureBinaryI32Operation(Signatures.DefinePureBinaryFunction("Subtract", NITypes.Int32, NITypes.Int32), 6, 5, 1);
        }

        [TestMethod]
        public void MultiplyTwoI32s_Execute_CorrectResultValue()
        {
            TestPureBinaryI32Operation(Signatures.DefinePureBinaryFunction("Multiply", NITypes.Int32, NITypes.Int32), 6, 5, 30);
        }

        [TestMethod]
        public void DivideTwoI32s_Execute_CorrectResultValue()
        {
            TestPureBinaryI32Operation(Signatures.DefinePureBinaryFunction("Divide", NITypes.Int32, NITypes.Int32), 6, 5, 1);
        }

        [TestMethod]
        public void ModulusOfTwoI32s_Execute_CorrectResultValue()
        {
            TestPureBinaryI32Operation(Signatures.DefinePureBinaryFunction("Modulus", NITypes.Int32, NITypes.Int32), 7, 5, 2);
        }

        [TestMethod]
        public void IncrementI32_Execute_CorrectResultValue()
        {
            TestPureUnaryI32Operation(Signatures.DefinePureUnaryFunction("Increment", NITypes.Int32, NITypes.Int32), 6, 7);
        }

        [TestMethod]
        public void AccumulateAddTwoI32s_Execute_CorrectResultValue()
        {
            TestMutatingBinaryI32Operation(Signatures.DefineMutatingBinaryFunction("AccumulateAdd", NITypes.Int32), 6, 5, 11);
        }

        [TestMethod]
        public void AccumulateSubtractTwoI32s_Execute_CorrectResultValue()
        {
            TestMutatingBinaryI32Operation(Signatures.DefineMutatingBinaryFunction("AccumulateSubtract", NITypes.Int32), 6, 5, 1);
        }

        [TestMethod]
        public void AccumulateMultiplyTwoI32s_Execute_CorrectResultValue()
        {
            TestMutatingBinaryI32Operation(Signatures.DefineMutatingBinaryFunction("AccumulateMultiply", NITypes.Int32), 6, 5, 30);
        }

        [TestMethod]
        public void AccumulateDivideTwoI32s_Execute_CorrectResultValue()
        {
            TestMutatingBinaryI32Operation(Signatures.DefineMutatingBinaryFunction("AccumulateDivide", NITypes.Int32), 6, 5, 1);
        }

        [TestMethod]
        public void AccumulateIncrementI32_Execute_CorrectResultValue()
        {
            TestMutatingUnaryI32Operation(Signatures.DefineMutatingUnaryFunction("AccumulateIncrement", NITypes.Int32), 6, 7);
        }

        [TestMethod]
        public void EqualI32_Execute_CorrectResultValue()
        {
            TestI32ComparisonOperation(Signatures.DefineComparisonFunction("Equal"), 6, 5, false);
        }

        [TestMethod]
        public void NotEqualI32_Execute_CorrectResultValue()
        {
            TestI32ComparisonOperation(Signatures.DefineComparisonFunction("NotEqual"), 6, 5, true);
        }

        [TestMethod]
        public void LessThanI32_Execute_CorrectResultValue()
        {
            TestI32ComparisonOperation(Signatures.DefineComparisonFunction("LessThan"), 6, 5, false);
        }

        [TestMethod]
        public void LessEqualI32_Execute_CorrectResultValue()
        {
            TestI32ComparisonOperation(Signatures.DefineComparisonFunction("LessEqual"), 6, 5, false);
        }

        [TestMethod]
        public void GreaterThanI32_Execute_CorrectResultValue()
        {
            TestI32ComparisonOperation(Signatures.DefineComparisonFunction("GreaterThan"), 6, 5, true);
        }

        [TestMethod]
        public void GreaterEqualI32_Execute_CorrectResultValue()
        {
            TestI32ComparisonOperation(Signatures.DefineComparisonFunction("GreaterEqual"), 6, 5, true);
        }

        private void TestPureBinaryI32Operation(NIType operationSignature, int leftValue, int rightValue, int expectedResult)
        {
            TestPrimitiveOperation(
                operationSignature,
                leftValue,
                rightValue,
                NITypes.Int32,
                false,
                value => AssertByteArrayIsInt32(value, expectedResult));
        }

        private void TestPureUnaryI32Operation(NIType operationSignature, int value, int expectedResult)
        {
            TestPrimitiveOperation(
                operationSignature,
                value,
                null,
                NITypes.Int32,
                false,
                v => AssertByteArrayIsInt32(v, expectedResult));
        }

        private void TestMutatingBinaryI32Operation(NIType operationSignature, int leftValue, int rightValue, int expectedResult)
        {
            TestPrimitiveOperation(
                operationSignature,
                leftValue,
                rightValue,
                NITypes.Int32,
                true,
                value => AssertByteArrayIsInt32(value, expectedResult));
        }

        private void TestMutatingUnaryI32Operation(NIType operationSignature, int value, int expectedResult)
        {
            TestPrimitiveOperation(
                operationSignature,
                value,
                null,
                NITypes.Int32,
                true,
                v => AssertByteArrayIsInt32(v, expectedResult));
        }

        private void TestI32ComparisonOperation(NIType operationSignature, int leftValue, int rightValue, bool expectedResult)
        {
            TestPrimitiveOperation(
                operationSignature,
                leftValue,
                rightValue,
                NITypes.Int32,
                false,
                v => AssertByteArrayIsBoolean(v, expectedResult));
        }
    }
}
