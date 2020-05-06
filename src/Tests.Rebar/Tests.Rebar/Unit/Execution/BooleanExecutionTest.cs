using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.DataTypes;
using Rebar.Common;

namespace Tests.Rebar.Unit.Execution
{
    [TestClass]
    public class BooleanExecutionTest : PrimitiveOpExecutionTest
    {
        [TestMethod]
        public void AndTwoBooleans_Execute_CorrectResultValue()
        {
            TestPureBinaryBooleanOperation(Signatures.DefinePureBinaryFunction("And", NITypes.Boolean, NITypes.Boolean), true, false, false);
        }

        [TestMethod]
        public void OrTwoBooleans_Execute_CorrectResultValue()
        {
            TestPureBinaryBooleanOperation(Signatures.DefinePureBinaryFunction("Or", NITypes.Boolean, NITypes.Boolean), true, false, true);
        }

        [TestMethod]
        public void XorTwoBooleans_Execute_CorrectResultValue()
        {
            TestPureBinaryBooleanOperation(Signatures.DefinePureBinaryFunction("Xor", NITypes.Boolean, NITypes.Boolean), true, false, true);
        }

        [TestMethod]
        public void NotBoolean_Execute_CorrectResultValue()
        {
            TestPureUnaryBooleanOperation(Signatures.DefinePureUnaryFunction("Not", NITypes.Boolean, NITypes.Boolean), true, false);
        }

        [TestMethod]
        public void AccumulateAndTwoBooleans_Execute_CorrectResultValue()
        {
            TestMutatingBinaryBooleanOperation(Signatures.DefineMutatingBinaryFunction("AccumulateAnd", NITypes.Boolean), true, false, false);
        }

        [TestMethod]
        public void AccumulateOrTwoBooleans_Execute_CorrectResultValue()
        {
            TestMutatingBinaryBooleanOperation(Signatures.DefineMutatingBinaryFunction("AccumulateOr", NITypes.Boolean), true, false, true);
        }

        [TestMethod]
        public void AccumulateXorTwoBooleans_Execute_CorrectResultValue()
        {
            TestMutatingBinaryBooleanOperation(Signatures.DefineMutatingBinaryFunction("AccumulateXor", NITypes.Boolean), true, false, true);
        }
        
        [TestMethod]
        public void AccumulateNotBoolean_Execute_CorrectResultValue()
        {
            TestMutatingUnaryBooleanOperation(Signatures.DefineMutatingUnaryFunction("AccumulateNot", NITypes.Boolean), true, false);
        }

        private void TestPureBinaryBooleanOperation(NIType operationSignature, bool leftValue, bool rightValue, bool expectedResult)
        {
            TestPrimitiveOperation(
                operationSignature,
                leftValue,
                rightValue,
                NITypes.Boolean,
                false,
                value => AssertByteArrayIsBoolean(value, expectedResult));
        }

        private void TestPureUnaryBooleanOperation(NIType operationSignature, bool value, bool expectedResult)
        {
            TestPrimitiveOperation(
                operationSignature,
                value,
                null,
                NITypes.Boolean,
                false,
                v => AssertByteArrayIsBoolean(v, expectedResult));
        }

        private void TestMutatingBinaryBooleanOperation(NIType operationSignature, bool leftValue, bool rightValue, bool expectedResult)
        {
            TestPrimitiveOperation(
                operationSignature,
                leftValue,
                rightValue,
                NITypes.Boolean,
                true,
                value => AssertByteArrayIsBoolean(value, expectedResult));
        }

        private void TestMutatingUnaryBooleanOperation(NIType operationSignature, bool value, bool expectedResult)
        {
            TestPrimitiveOperation(
                operationSignature,
                value,
                null,
                NITypes.Boolean,
                true,
                v => AssertByteArrayIsBoolean(v, expectedResult));
        }
    }
}
