using System;
using System.Collections.Generic;
using System.Linq;
using NationalInstruments.Composition;
using NationalInstruments.Core;
using System.Runtime.Serialization;

namespace Rebar.RebarTarget.Execution
{
    public class ExecutionContext
    {
        private Dictionary<string, Function> _loadedFunctions = new Dictionary<string, Function>();

        public ExecutionContext(ICompositionHost host)
        {
            Host = host;
        }

        public ICompositionHost Host { get; }

        public void LoadFunction(Function function)
        {
            _loadedFunctions[function.Name] = function;
        }

        public void ExecuteFunctionTopLevel(string functionName)
        {
            try
            {
                Function function = _loadedFunctions[functionName];
                byte[] code = function.Code;
                int ip = 0;
                bool executing = true;
                var operandStack = new Stack<int>();
                var memory = new byte[4 * 256];
                int stackTop = 0;

                while (executing)
                {
                    int nextIP = ip + 1;
                    OpCodes opcode = (OpCodes)code[ip];
                    switch (opcode)
                    {
                        case OpCodes.Ret:
                            executing = false;
                            break;
                        case OpCodes.Branch:
                            {
                                nextIP = DataHelpers.ReadIntFromByteArray(code, ip + 1);
                            }
                            break;
                        case OpCodes.BranchIfFalse:
                            {
                                nextIP = ip + 5;
                                int value = operandStack.Pop();
                                if (value == 0)
                                {
                                    nextIP = DataHelpers.ReadIntFromByteArray(code, ip + 1);
                                }
                            }
                            break;
                        case OpCodes.LoadIntegerImmediate:
                            {
                                int value = DataHelpers.ReadIntFromByteArray(code, ip + 1);
                                operandStack.Push(value);
                                nextIP = ip + 5;
                            }
                            break;
                        case OpCodes.LoadLocalAddress:
                            {
                                byte localIndex = code[ip + 1];
                                int localAddress = stackTop + function.LocalOffsets[localIndex];
                                operandStack.Push(localAddress);
                                nextIP = ip + 2;
                            }
                            break;
                        case OpCodes.StoreInteger:
                        case OpCodes.StorePointer:
                            {
                                int value = operandStack.Pop(),
                                    address = operandStack.Pop();
                                DataHelpers.WriteIntToByteArray(value, memory, address);
                                string message = $"Stored {value} at {address}";
                                Host.GetSharedExportedValue<IDebugHost>().LogMessage(new DebugMessage("Rebar runtime", DebugMessageSeverity.Information, message));
                            }
                            break;
                        case OpCodes.DerefInteger:
                        case OpCodes.DerefPointer:
                            {
                                int address = operandStack.Pop();
                                int value = DataHelpers.ReadIntFromByteArray(memory, address);
                                operandStack.Push(value);
                            }
                            break;
                        case OpCodes.Add:
                        case OpCodes.Subtract:
                        case OpCodes.Multiply:
                        case OpCodes.Divide:
                        case OpCodes.And:
                        case OpCodes.Or:
                        case OpCodes.Xor:
                        case OpCodes.Gt:
                            {
                                int rhs = operandStack.Pop();
                                int lhs = operandStack.Pop();
                                int result = 0;
                                switch (opcode)
                                {
                                    case OpCodes.Add:
                                        result = lhs + rhs;
                                        break;
                                    case OpCodes.Subtract:
                                        result = lhs - rhs;
                                        break;
                                    case OpCodes.Multiply:
                                        result = lhs * rhs;
                                        break;
                                    case OpCodes.Divide:
                                        result = lhs / rhs;
                                        break;
                                    case OpCodes.And:
                                        result = lhs & rhs;
                                        break;
                                    case OpCodes.Or:
                                        result = lhs | rhs;
                                        break;
                                    case OpCodes.Xor:
                                        result = lhs ^ rhs;
                                        break;
                                    case OpCodes.Gt:
                                        result = (lhs > rhs) ? 1 : 0;
                                        break;
                                }
                                operandStack.Push(result);
                            }
                            break;
                        case OpCodes.Dup:
                            {
                                int value = operandStack.Pop();
                                operandStack.Push(value);
                                operandStack.Push(value);
                            }
                            break;
                        case OpCodes.Swap:
                            {
                                int top = operandStack.Pop(),
                                    next = operandStack.Pop();
                                operandStack.Push(top);
                                operandStack.Push(next);
                            }
                            break;
                        default:
                            throw new NotSupportedException("Invalid opcode: " + opcode);
                    }
                    ip = nextIP;
                }
            }
            catch (Exception)
            {

            }
        }
    }

    internal static class DataHelpers
    {
        public static int ReadIntFromByteArray(byte[] array, int index)
        {
            int value = 0;
            for (int i = 3; i >= 0; --i)
            {
                value <<= 8;
                value |= array[index + i];
            }
            return value;
        }

        public static void WriteIntToByteArray(int value, byte[] array, int index)
        {
            for (int i = 0; i < 4; ++i)
            {
                array[index + i] = (byte)value;
                value >>= 8;
            }
        }
    }

    internal enum OpCodes : byte
    {
        Ret = 0x00,
        Branch = 0x01,
        BranchIfFalse = 0x02,
        // BranchIfTrue = 0x03,
        LoadIntegerImmediate = 0x10,
        LoadLocalAddress = 0x11,
        StoreInteger = 0x20,
        StorePointer = 0x21,
        DerefInteger = 0x30,
        DerefPointer = 0x31,
        Add = 0x40,
        Subtract = 0x41,
        Multiply = 0x42,
        Divide = 0x43,
        And = 0x44,
        Or = 0x45,
        Xor = 0x46,
        Gt = 0x48,
        // Gte = 0x49,
        // Lt = 0x4A,
        // Lte = 0x4B,
        // Eq = 0x4C,
        // Neq = 0x4D,
        Dup = 0x50,
        Swap = 0x51,
    }

    [Serializable]
    public class Function : ISerializable
    {
        internal Function(string name, int[] localOffsets, byte[] code)
        {
            Name = name;
            LocalOffsets = localOffsets;
            Code = code;
        }

        /// <inheritdoc />
        protected Function(SerializationInfo info, StreamingContext context)
        {
            Name = info.GetString(nameof(Name));
            LocalOffsets = (int[])info.GetValue(nameof(LocalOffsets), typeof(int[]));
            Code = (byte[])info.GetValue(nameof(Code), typeof(byte[]));
        }

        public string Name { get; }

        public int[] LocalOffsets { get; }

        public byte[] Code { get; }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Name), Name);
            info.AddValue(nameof(LocalOffsets), LocalOffsets);
            info.AddValue(nameof(Code), Code);
        }
    } 

    public sealed class FunctionBuilder
    {
        private int[] _localSizes;
        private readonly List<byte[]> _code = new List<byte[]>();
        private readonly Dictionary<int, LabelBuilder> _branches = new Dictionary<int, LabelBuilder>();
        private readonly Dictionary<LabelBuilder, int> _labels = new Dictionary<LabelBuilder, int>();

        public Function CreateFunction()
        {
            int position = 0, instructionIndex = 0;
            var finalPositions = new int[_code.Count];
            for (instructionIndex = 0; instructionIndex < _code.Count; ++instructionIndex)
            {
                finalPositions[instructionIndex] = position;
                position += _code[instructionIndex].Length;
            }

            foreach (var pair in _branches)
            {
                int targetPosition = finalPositions[_labels[pair.Value]];
                byte[] instruction = _code[pair.Key];
                DataHelpers.WriteIntToByteArray(targetPosition, instruction, 1);
            }

            int[] localOffsets;
            if (LocalSizes != null)
            {
                int offset = 0;
                localOffsets = LocalSizes.Select(size =>
                {
                    int previousOffset = offset;
                    offset += size;
                    return previousOffset;
                }).ToArray();
            }
            else
            {
                localOffsets = new int[0];
            }
            return new Function(Name, localOffsets, _code.SelectMany(i => i).ToArray());
        }

        public string Name { get; set; }

        public int[] LocalSizes { get; set; }

        private void EmitStandaloneOpcode(OpCodes opcode)
        {
            _code.Add(new byte[] { (byte)opcode });
        }

        public LabelBuilder CreateLabel()
        {
            return new LabelBuilder();
        }

        public void SetLabel(LabelBuilder label)
        {
            if (!_labels.ContainsKey(label))
            {
                _labels.Add(label, _code.Count);
            }
            else
            {
                throw new InvalidOperationException("Label has already been set");
            }
        }

        public void EmitReturn()
        {
            EmitStandaloneOpcode(OpCodes.Ret);
        }

        private void EmitBranchPlaceholder(OpCodes branchOperation, LabelBuilder target)
        {
            byte[] code = new byte[5];
            code[0] = (byte)branchOperation;
            _code.Add(code);
            _branches[_code.Count - 1] = target;
        }

        public void EmitBranch(LabelBuilder target)
        {
            EmitBranchPlaceholder(OpCodes.Branch, target);
        }

        public void EmitBranchIfFalse(LabelBuilder target)
        {
            EmitBranchPlaceholder(OpCodes.BranchIfFalse, target);
        }

        public void EmitLoadIntegerImmediate(int constant)
        {
            byte[] code = new byte[5];
            code[0] = (byte)OpCodes.LoadIntegerImmediate;
            DataHelpers.WriteIntToByteArray(constant, code, 1);
            _code.Add(code);
        }

        public void EmitLoadLocalAddress(byte localIndex)
        {
            _code.Add(new byte[] { (byte)OpCodes.LoadLocalAddress, localIndex });
        }

        public void EmitStoreInteger()
        {
            EmitStandaloneOpcode(OpCodes.StoreInteger);
        }

        public void EmitStorePointer()
        {
            EmitStandaloneOpcode(OpCodes.StorePointer);
        }

        public void EmitDerefInteger()
        {
            EmitStandaloneOpcode(OpCodes.DerefInteger);
        }

        public void EmitDerefPointer()
        {
            EmitStandaloneOpcode(OpCodes.DerefPointer);
        }

        public void EmitAdd()
        {
            EmitStandaloneOpcode(OpCodes.Add);
        }

        public void EmitSubtract()
        {
            EmitStandaloneOpcode(OpCodes.Subtract);
        }

        public void EmitMultiply()
        {
            EmitStandaloneOpcode(OpCodes.Multiply);
        }

        public void EmitDivide()
        {
            EmitStandaloneOpcode(OpCodes.Divide);
        }

        public void EmitAnd()
        {
            EmitStandaloneOpcode(OpCodes.And);
        }

        public void EmitOr()
        {
            EmitStandaloneOpcode(OpCodes.Or);
        }

        public void EmitXor()
        {
            EmitStandaloneOpcode(OpCodes.Xor);
        }

        public void EmitGreaterThan()
        {
            EmitStandaloneOpcode(OpCodes.Gt);
        }

        public void EmitDuplicate()
        {
            EmitStandaloneOpcode(OpCodes.Dup);
        }
        
        public void EmitSwap()
        {
            EmitStandaloneOpcode(OpCodes.Swap);
        }
    }

    public class LabelBuilder
    {
    }
}
