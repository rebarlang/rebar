using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Rebar.RebarTarget.Execution
{
    public class ExecutionContext
    {
        private bool _loading = true;
        private readonly IRebarTargetRuntimeServices _runtimeServices;
        private Dictionary<string, Function> _loadedFunctions = new Dictionary<string, Function>();
        private readonly Dictionary<StaticDataIdentifier, Tuple<int, int>> _staticDataLocations = new Dictionary<StaticDataIdentifier, Tuple<int, int>>();
        private int _totalStaticDataSize = 0;
        private byte[] _memory;
        private int _stackOffset;
        private int _heapOffset;

        public ExecutionContext(IRebarTargetRuntimeServices runtimeServices)
        {
            _runtimeServices = runtimeServices;
        }

        internal byte[] ReadStaticData(StaticDataIdentifier staticDataIdentifier)
        {
            if (_loading)
            {
                throw new InvalidOperationException("Cannot call ReadStaticData before calling FinalizeLoad");
            }
            Tuple<int, int> staticDataLocation;
            if (_staticDataLocations.TryGetValue(staticDataIdentifier, out staticDataLocation))
            {
                byte[] staticData = new byte[staticDataLocation.Item2];
                Array.Copy(_memory, staticDataLocation.Item1, staticData, 0, staticDataLocation.Item2);
                return staticData;
            }
            throw new ArgumentException("Static data not found", "staticDataIdentifier");
        }

        public void LoadFunction(Function function)
        {
            if (!_loading)
            {
                throw new InvalidOperationException("Cannot call LoadFunction after calling FinalizeLoad");
            }
            _loadedFunctions[function.Name] = function;
            _totalStaticDataSize += ComputeStaticDataSize(function);
        }

        private int RoundUpToNearest(int toRound, int multiplicand)
        {
            int remainder = toRound % multiplicand;
            return remainder == 0 ? toRound : (toRound + multiplicand - remainder);
        }

        private int ComputeStaticDataSize(Function function)
        {
            int size = 0;
            foreach (var staticDataItem in function.StaticData)
            {
                int dataItemSize = RoundUpToNearest(staticDataItem.Data.Length, 4);
                size += dataItemSize;
            }
            return size;
        }

        public void FinalizeLoad()
        {
            _loading = false;

            int dataSectionSize = RoundUpToNearest(_totalStaticDataSize, 1024);
            int stackSize = 1024;
            int heapSize = 2048;
            _memory = new byte[dataSectionSize + stackSize + heapSize];
            _stackOffset = dataSectionSize;
            _heapOffset = dataSectionSize + stackSize;

            int currentOffset = 0;
            foreach (Function loadedFunction in _loadedFunctions.Values)
            {
                Dictionary<StaticDataInformation, int> staticOffsets = new Dictionary<StaticDataInformation, int>();
                foreach (var staticDataInformation in loadedFunction.StaticData)
                {
                    staticOffsets[staticDataInformation] = currentOffset;
                    byte[] staticDataItem = staticDataInformation.Data;
                    if (staticDataInformation.Identifier != null)
                    {
                        _staticDataLocations[staticDataInformation.Identifier] = new Tuple<int, int>(currentOffset, staticDataItem.Length);
                    }
                    Array.Copy(staticDataItem, _memory, staticDataItem.Length);
                    currentOffset += RoundUpToNearest(staticDataItem.Length, 4);
                }
                loadedFunction.PatchStaticDataOffsets(staticOffsets);
            }
        }

        private void GrowStack(ref int stackTop, int size)
        {
            int newStackTop = stackTop + size;
            if (newStackTop >= _heapOffset)
            {
                throw new InvalidOperationException("Ran out of stack space");
            }
            stackTop = newStackTop;
        }

        public void ExecuteFunctionTopLevel(string functionName)
        {
            if (_loading)
            {
                throw new InvalidOperationException("Cannot call ExecuteFunctionTopLevel before calling FinalizeLoad");
            }
            try
            {
                Function function = _loadedFunctions[functionName];
                byte[] code = function.Code;
                int ip = 0;
                bool executing = true;
                var operandStack = new Stack<int>();
                int stackBottom = _stackOffset;
                int stackTop = stackBottom;
                GrowStack(ref stackTop, function.LocalSize);

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
                                int localAddress = stackBottom + function.LocalOffsets[localIndex];
                                operandStack.Push(localAddress);
                                nextIP = ip + 2;
                            }
                            break;
                        case OpCodes.LoadStaticAddress:
                            {
                                int staticAddress = DataHelpers.ReadIntFromByteArray(code, ip + 1);
                                operandStack.Push(staticAddress);
                                nextIP = ip + 5;
                            }
                            break;
                        case OpCodes.StoreInteger:
                        case OpCodes.StorePointer:
                            {
                                int value = operandStack.Pop(),
                                    address = operandStack.Pop();
                                DataHelpers.WriteIntToByteArray(value, _memory, address);
                                string message = $"Stored {value} at {address}";
                            }
                            break;
                        case OpCodes.DerefInteger:
                        case OpCodes.DerefPointer:
                            {
                                int address = operandStack.Pop();
                                int value = DataHelpers.ReadIntFromByteArray(_memory, address);
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
                        case OpCodes.Gte:
                        case OpCodes.Lt:
                        case OpCodes.Lte:
                        case OpCodes.Eq:
                        case OpCodes.Neq:
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
                                    case OpCodes.Gte:
                                        result = (lhs >= rhs) ? 1 : 0;
                                        break;
                                    case OpCodes.Lt:
                                        result = (lhs < rhs) ? 1 : 0;
                                        break;
                                    case OpCodes.Lte:
                                        result = (lhs <= rhs) ? 1 : 0;
                                        break;
                                    case OpCodes.Eq:
                                        result = (lhs == rhs) ? 1 : 0;
                                        break;
                                    case OpCodes.Neq:
                                        result = (lhs != rhs) ? 1 : 0;
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
                        case OpCodes.ExchangeBytes_TEMP:
                            {
                                int size = operandStack.Pop(),
                                    address1 = operandStack.Pop(),
                                    address2 = operandStack.Pop();
                                int tempAddress = stackTop;
                                GrowStack(ref stackTop, size);
                                CopyBytes(address1, tempAddress, size);
                                CopyBytes(address2, address1, size);
                                CopyBytes(tempAddress, address2, size);
                                GrowStack(ref stackTop, -size);
                            }
                            break;
                        case OpCodes.CopyBytes_TEMP:
                            {
                                int size = operandStack.Pop(),
                                    toAddress = operandStack.Pop(),
                                    fromAddress = operandStack.Pop();
                                CopyBytes(fromAddress, toAddress, size);
                            }
                            break;
                        case OpCodes.Output_TEMP:
                            {
                                int value = operandStack.Pop();
                                string message = $"Output: {value}";
                                _runtimeServices.Output(message);
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

        private void CopyBytes(int fromAddress, int toAddress, int size)
        {
            for (int i = 0; i < size; ++i)
            {
                _memory[toAddress] = _memory[fromAddress];
                ++toAddress;
                ++fromAddress;
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
        LoadStaticAddress = 0x12,
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
        Gte = 0x49,
        Lt = 0x4A,
        Lte = 0x4B,
        Eq = 0x4C,
        Neq = 0x4D,
        Dup = 0x50,
        Swap = 0x51,

        ExchangeBytes_TEMP = 0xFA,
        CopyBytes_TEMP = 0xFD,
        Output_TEMP = 0xFF
    }

    public sealed class StaticDataInformation
    {
        public byte[] Data { get; }

        // TODO: shouldn't need this if we can just traverse the bytecode
        public int[] LoadOffsets { get; }

        public StaticDataIdentifier Identifier { get; }

        public StaticDataInformation(byte[] data, int[] loadOffsets, StaticDataIdentifier identifier)
        {
            Data = data;
            LoadOffsets = loadOffsets;
            Identifier = identifier;
        }
    }

    [Serializable]
    public class Function : ISerializable
    {
        internal Function(
            string name, 
            int[] localOffsets, 
            int localSize,
            byte[] code, 
            StaticDataInformation[] staticData)
        {
            Name = name;
            LocalOffsets = localOffsets;
            LocalSize = localSize;
            Code = code;
            StaticData = staticData;
        }

        /// <inheritdoc />
        protected Function(SerializationInfo info, StreamingContext context)
        {
            Name = info.GetString(nameof(Name));
            LocalOffsets = (int[])info.GetValue(nameof(LocalOffsets), typeof(int[]));
            LocalSize = info.GetInt32(nameof(LocalSize));
            Code = (byte[])info.GetValue(nameof(Code), typeof(byte[]));
        }

        public string Name { get; }

        public int[] LocalOffsets { get; }

        public int LocalSize { get; }

        public byte[] Code { get; }

        public StaticDataInformation[] StaticData { get; }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Name), Name);
            info.AddValue(nameof(LocalOffsets), LocalOffsets);
            info.AddValue(nameof(Code), Code);
        }

        public void PatchStaticDataOffsets(Dictionary<StaticDataInformation, int> staticDataOffsets)
        {
            foreach (var staticDataInformation in StaticData)
            {
                int staticDataAddress = staticDataOffsets[staticDataInformation];
                foreach (int instructionOffset in staticDataInformation.LoadOffsets)
                {
                    DataHelpers.WriteIntToByteArray(staticDataAddress, Code, instructionOffset + 1);
                }
            }
        }
    } 

    public sealed class FunctionBuilder
    {
        private readonly List<byte[]> _code = new List<byte[]>();
        private readonly Dictionary<int, LabelBuilder> _branches = new Dictionary<int, LabelBuilder>();
        private readonly Dictionary<LabelBuilder, int> _labels = new Dictionary<LabelBuilder, int>();
        private readonly Dictionary<StaticDataBuilder, int> _staticData = new Dictionary<StaticDataBuilder, int>();
        private readonly Dictionary<int, StaticDataBuilder> _loadStaticDatas = new Dictionary<int, StaticDataBuilder>();

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
            int offset = 0;
            if (LocalSizes != null)
            {
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

            List<Tuple<StaticDataBuilder, List<int>>> staticDataTuples = new List<Tuple<StaticDataBuilder, List<int>>>();
            foreach (var staticDataBuilderPair in _staticData)
            {
                staticDataTuples.Add(new Tuple<StaticDataBuilder, List<int>>(staticDataBuilderPair.Key, new List<int>()));
            }

            var loadStaticDataOffsets = new Dictionary<int, int>();
            foreach (var loadStaticDataPair in _loadStaticDatas)
            {
                int loadStaticDataPosition = loadStaticDataPair.Key;
                StaticDataBuilder staticDataBuilder = loadStaticDataPair.Value;
                var staticDataTuple = staticDataTuples.First(tuple => tuple.Item1 == staticDataBuilder);
                staticDataTuple.Item2.Add(finalPositions[loadStaticDataPosition]);
            }
            StaticDataInformation[] staticDataInformations = staticDataTuples.Select(
                tuple => new StaticDataInformation(tuple.Item1.Data, tuple.Item2.ToArray(), tuple.Item1.Identifier)
            )
            .ToArray();
            return new Function(Name, localOffsets, offset, _code.SelectMany(i => i).ToArray(), staticDataInformations);
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

        public StaticDataBuilder DefineStaticData()
        {
            var staticDataBuilder = new StaticDataBuilder();
            int index = _staticData.Count;
            _staticData.Add(staticDataBuilder, index);
            return staticDataBuilder;
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

        public void EmitLoadStaticDataAddress(StaticDataBuilder staticData)
        {
            byte[] code = new byte[5];
            code[0] = (byte)OpCodes.LoadStaticAddress;
            DataHelpers.WriteIntToByteArray(_staticData[staticData], code, 1);
            _code.Add(code);
            _loadStaticDatas[_code.Count - 1] = staticData;
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

        public void EmitGreaterThanOrEqual()
        {
            EmitStandaloneOpcode(OpCodes.Gte);
        }

        public void EmitLessThan()
        {
            EmitStandaloneOpcode(OpCodes.Lt);
        }

        public void EmitLessThanOrEqual()
        {
            EmitStandaloneOpcode(OpCodes.Lte);
        }

        public void EmitEquals()
        {
            EmitStandaloneOpcode(OpCodes.Eq);
        }

        public void EmitNotEquals()
        {
            EmitStandaloneOpcode(OpCodes.Neq);
        }

        public void EmitDuplicate()
        {
            EmitStandaloneOpcode(OpCodes.Dup);
        }
        
        public void EmitSwap()
        {
            EmitStandaloneOpcode(OpCodes.Swap);
        }

        public void EmitExchangeBytes_TEMP()
        {
            EmitStandaloneOpcode(OpCodes.ExchangeBytes_TEMP);
        }

        public void EmitCopyBytes_TEMP()
        {
            EmitStandaloneOpcode(OpCodes.CopyBytes_TEMP);
        }

        public void EmitOutput_TEMP()
        {
            EmitStandaloneOpcode(OpCodes.Output_TEMP);
        }
    }

    public class LabelBuilder
    {
    }

    public class StaticDataBuilder
    {
        public byte[] Data;

        public StaticDataIdentifier Identifier;
    }
}
