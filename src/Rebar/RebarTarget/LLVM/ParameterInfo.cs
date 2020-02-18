using NationalInstruments.Dfir;
using Rebar.Common;

namespace Rebar.RebarTarget.LLVM
{
    internal class ParameterInfo
    {
        public ParameterInfo(VariableReference parameterVariable, Direction direction)
        {
            ParameterVariable = parameterVariable;
            Direction = direction;
        }

        public VariableReference ParameterVariable { get; }

        public Direction Direction { get; }
    }
}
