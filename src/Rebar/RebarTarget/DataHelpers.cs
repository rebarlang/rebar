namespace Rebar.RebarTarget
{
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

        public static int RoundUpToNearest(this int toRound, int multiplicand)
        {
            int remainder = toRound % multiplicand;
            return remainder == 0 ? toRound : (toRound + multiplicand - remainder);
        }
    }
}
