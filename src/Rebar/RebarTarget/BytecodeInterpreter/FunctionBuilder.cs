using System;
using System.Collections.Generic;
using System.Linq;

namespace Rebar.RebarTarget.Execution
{
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

        public void EmitOutputString_TEMP()
        {
            EmitStandaloneOpcode(OpCodes.OutputString_TEMP);
        }

        public void EmitCopyBytes_TEMP()
        {
            EmitStandaloneOpcode(OpCodes.CopyBytes_TEMP);
        }

        public void EmitAlloc_TEMP()
        {
            EmitStandaloneOpcode(OpCodes.Alloc_TEMP);
        }

        public void EmitOutput_TEMP()
        {
            EmitStandaloneOpcode(OpCodes.Output_TEMP);
        }
    }
}
