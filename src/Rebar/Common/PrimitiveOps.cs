using System;
using NationalInstruments.DataTypes;

namespace Rebar.Common
{
    public enum BinaryPrimitiveOps
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Modulus,

        And,
        Or,
        Xor
    }

    public enum UnaryPrimitiveOps
    {
        Increment,

        Not
    }

    public static class PrimitiveExtensions
    {
        public static NIType GetExpectedInputType(this BinaryPrimitiveOps binaryOp)
        {
            switch (binaryOp)
            {
                case BinaryPrimitiveOps.Add:
                case BinaryPrimitiveOps.Subtract:
                case BinaryPrimitiveOps.Multiply:
                case BinaryPrimitiveOps.Divide:
                case BinaryPrimitiveOps.Modulus:
                    return NITypes.Int32;
                case BinaryPrimitiveOps.And:
                case BinaryPrimitiveOps.Or:
                case BinaryPrimitiveOps.Xor:
                    return NITypes.Boolean;
                default:
                    throw new NotImplementedException();
            }
        }

        public static NIType GetExpectedInputType(this UnaryPrimitiveOps unaryOp)
        {
            switch (unaryOp)
            {
                case UnaryPrimitiveOps.Increment:
                    return NITypes.Int32;
                case UnaryPrimitiveOps.Not:
                    return NITypes.Boolean;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
