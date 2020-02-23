#define LLVM_TEST

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler.Nodes;
using Rebar.RebarTarget;
using Tests.Rebar.Unit.Compiler;

namespace Tests.Rebar.Unit.Execution
{
    public abstract class ExecutionTestBase : CompilerTestBase
    {
        internal TestExecutionInstance CompileAndExecuteFunction(DfirRoot function, params DfirRoot[] otherFunctions)
        {
            var testExecutionInstance = new TestExecutionInstance();
            testExecutionInstance.CompileAndExecuteFunction(this, function, otherFunctions);
            return testExecutionInstance;
        }

        internal Constant ConnectConstantToInputTerminal(Terminal inputTerminal, NIType variableType, object value, bool mutable)
        {
            Constant constant = ConnectConstantToInputTerminal(inputTerminal, variableType, mutable);
            constant.Value = value;
            return constant;
        }

        protected Constant ConnectStringConstantToInputTerminal(Terminal inputTerminal, string value, bool mutable = false)
        {
            return ConnectConstantToInputTerminal(inputTerminal, DataTypes.StringSliceType.CreateImmutableReference(), value, mutable);
        }

        internal FunctionalNode ConnectInspectToOutputTerminal(Terminal outputTerminal)
        {
            FunctionalNode inspect = new FunctionalNode(outputTerminal.ParentDiagram, Signatures.InspectType);
            Wire.Create(outputTerminal.ParentDiagram, outputTerminal, inspect.InputTerminals[0]);
            return inspect;
        }

        internal FunctionalNode CreateFakeDropWithId(Diagram parentDiagram, int id)
        {
            FunctionalNode fakeDropCreate = new FunctionalNode(parentDiagram, Signatures.FakeDropCreateType);
            ConnectConstantToInputTerminal(fakeDropCreate.InputTerminals[0], PFTypes.Int32, id, false);
            return fakeDropCreate;
        }

        protected void AssertByteArrayIsBoolean(byte[] region, bool value)
        {
#if LLVM_TEST
            Assert.AreEqual(1, region.Length);
            Assert.AreEqual(value ? 1 : 0, region[0]);
#else
            Assert.AreEqual(4, region.Length);
            Assert.AreEqual(value ? 1 : 0, BitConverter.ToInt32(region, 0));
#endif
        }

        protected void AssertByteArrayIsInt8(byte[] region, sbyte value)
        {
            Assert.AreEqual(1, region.Length);
            Assert.AreEqual(value, (sbyte)region[0]);
        }

        protected void AssertByteArrayIsUInt8(byte[] region, byte value)
        {
            Assert.AreEqual(1, region.Length);
            Assert.AreEqual(value, region[0]);
        }

        protected void AssertByteArrayIsInt16(byte[] region, short value)
        {
            Assert.AreEqual(2, region.Length);
            Assert.AreEqual(value, BitConverter.ToInt16(region, 0));
        }

        protected void AssertByteArrayIsUInt16(byte[] region, ushort value)
        {
            Assert.AreEqual(2, region.Length);
            Assert.AreEqual(value, BitConverter.ToUInt16(region, 0));
        }

        protected void AssertByteArrayIsInt32(byte[] region, int value)
        {
            Assert.AreEqual(4, region.Length);
            Assert.AreEqual(value, BitConverter.ToInt32(region, 0));
        }

        protected void AssertByteArrayIsUInt32(byte[] region, uint value)
        {
            Assert.AreEqual(4, region.Length);
            Assert.AreEqual(value, BitConverter.ToUInt32(region, 0));
        }

        protected void AssertByteArrayIsInt64(byte[] region, long value)
        {
            Assert.AreEqual(8, region.Length);
            Assert.AreEqual(value, BitConverter.ToInt64(region, 0));
        }

        protected void AssertByteArrayIsUInt64(byte[] region, ulong value)
        {
            Assert.AreEqual(8, region.Length);
            Assert.AreEqual(value, BitConverter.ToUInt64(region, 0));
        }

        protected void AssertByteArrayIsSomeInteger(byte[] value, int intValue)
        {
            Assert.AreEqual(8, value.Length);
            Assert.AreEqual(1, BitConverter.ToInt32(value, 0));
            Assert.AreEqual(intValue, BitConverter.ToInt32(value, 4));
        }

        protected void AssertByteArrayIsNoneInteger(byte[] value)
        {
            Assert.AreEqual(8, value.Length);
            Assert.AreEqual(0, BitConverter.ToInt32(value, 0));
        }
    }

    internal class TestRuntimeServices : IRebarTargetRuntimeServices
    {
        public void Output(string value)
        {
            LastOutputValue = value;
        }

        public void FakeDrop(int id)
        {
            DroppedFakeDropIds.Add(id);
        }

        public string LastOutputValue { get; private set; }

        public HashSet<int> DroppedFakeDropIds { get; } = new HashSet<int>();

        public bool PanicOccurred { get; } = false;
    }
}
