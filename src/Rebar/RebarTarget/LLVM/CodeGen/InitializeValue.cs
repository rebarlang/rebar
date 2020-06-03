using Rebar.Common;

namespace Rebar.RebarTarget.LLVM.CodeGen
{
    /// <summary>
    /// Represents initializing a <see cref="VariableReference"/> from an input value.
    /// </summary>
    internal sealed class InitializeValue : VariableUsage
    {
        public InitializeValue(VariableReference variable, int inputIndex)
            : base(variable, inputIndex)
        { }

        public int InputIndex => Index;

        public override string ToString() => $"Initialize {InputIndex} -> v_{Variable.Id}";

        public override T AcceptVisitor<T>(ICodeGenElementVisitor<T> visitor)
        {
            return visitor.VisitInitializeValue(this);
        }
    }
}
