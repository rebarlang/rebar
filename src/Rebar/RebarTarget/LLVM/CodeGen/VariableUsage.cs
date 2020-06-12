using Rebar.Common;

namespace Rebar.RebarTarget.LLVM.CodeGen
{
    /// <summary>
    /// Base class for <see cref="CodeGenElement"/> subclasses that represent a usage of a single <see cref="VariableReference"/>.
    /// </summary>
    internal abstract class VariableUsage : CodeGenElement
    {
        protected VariableUsage(VariableReference variable, int index)
        {
            Variable = variable;
            Index = index;
        }

        public VariableReference Variable { get; }

        protected int Index { get; }
    }
}
