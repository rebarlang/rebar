namespace Rebar.RebarTarget.Execution
{
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
        OutputString_TEMP = 0xFC,
        CopyBytes_TEMP = 0xFD,
        Alloc_TEMP = 0xFE,
        Output_TEMP = 0xFF
    }
}
