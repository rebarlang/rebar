using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Rebar.RebarTarget.Execution
{
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
}
