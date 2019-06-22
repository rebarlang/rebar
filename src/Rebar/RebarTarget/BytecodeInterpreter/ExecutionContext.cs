using System;
using System.Collections.Generic;
using System.Text;

#if LOG_MEMORY_ACCESS
using NationalInstruments.Core;
#endif

namespace Rebar.RebarTarget.BytecodeInterpreter
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

        private int ComputeStaticDataSize(Function function)
        {
            int size = 0;
            foreach (var staticDataItem in function.StaticData)
            {
                int dataItemSize = staticDataItem.Data.Length.RoundUpToNearest(4);
                size += dataItemSize;
            }
            return size;
        }

        public void FinalizeLoad()
        {
            _loading = false;

            int dataSectionSize = _totalStaticDataSize.RoundUpToNearest(1024);
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
                    Array.Copy(staticDataItem, 0, _memory, currentOffset, staticDataItem.Length);
                    int minimumSize = Math.Max(1, staticDataItem.Length);
                    currentOffset += minimumSize.RoundUpToNearest(4);
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
                                nextIP = BitConverter.ToInt32(code, ip + 1);
                            }
                            break;
                        case OpCodes.BranchIfFalse:
                            {
                                nextIP = ip + 5;
                                int value = operandStack.Pop();
                                if (value == 0)
                                {
                                    nextIP = BitConverter.ToInt32(code, ip + 1);
                                }
                            }
                            break;
                        case OpCodes.LoadIntegerImmediate:
                            {
                                int value = BitConverter.ToInt32(code, ip + 1);
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
                                int staticAddress = BitConverter.ToInt32(code, ip + 1);
                                operandStack.Push(staticAddress);
                                nextIP = ip + 5;
                            }
                            break;
                        case OpCodes.StoreInteger:
                        case OpCodes.StorePointer:
                            {
                                int value = operandStack.Pop(),
                                    address = operandStack.Pop();
#if LOG_MEMORY_ACCESS
                                Log.WriteLine($"{opcode} {value} at {address}");
#endif
                                DataHelpers.WriteIntToByteArray(value, _memory, address);
                                string message = $"Stored {value} at {address}";
                            }
                            break;
                        case OpCodes.DerefInteger:
                        case OpCodes.DerefPointer:
                            {
                                int address = operandStack.Pop();
                                int value = BitConverter.ToInt32(_memory, address);
#if LOG_MEMORY_ACCESS
                                Log.WriteLine($"{opcode} {value} from {address}");
#endif
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
                        case OpCodes.OutputString_TEMP:
                            {
                                int size = operandStack.Pop();
                                int stringBufferAddress = operandStack.Pop();
#if LOG_MEMORY_ACCESS
                                Log.WriteLine($"OutputString with {size} bytes from {stringBufferAddress}");
#endif
                                string str = Encoding.UTF8.GetString(_memory, stringBufferAddress, size);
                                _runtimeServices.Output(str);
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
                        case OpCodes.Alloc_TEMP:
                            {
                                int size = operandStack.Pop();
                                size = Math.Max(1, size).RoundUpToNearest(4);
                                if (_heapOffset + size <= _memory.Length)
                                {
                                    operandStack.Push(_heapOffset);
                                    _heapOffset += size;
                                }
                                else
                                {
                                    throw new InvalidOperationException("Ran out of heap memory");
                                }
                            }
                            break;
                        case OpCodes.Output_TEMP:
                            {
                                int value = operandStack.Pop();
                                _runtimeServices.Output(value.ToString());
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
#if LOG_MEMORY_ACCESS
            Log.WriteLine($"Copying {size} bytes from {fromAddress} to {toAddress}");
#endif
            for (int i = 0; i < size; ++i)
            {
                _memory[toAddress] = _memory[fromAddress];
                ++toAddress;
                ++fromAddress;
            }
        }
    }
}
