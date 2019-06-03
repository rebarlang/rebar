namespace Rebar.RebarTarget.Execution
{
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
}
