using Rebar.Common;

namespace Rebar.RebarTarget.LLVM.CodeGen
{
    /// <summary>
    /// Represents updating the value of a <see cref="VariableReference"/> to an input value.
    /// </summary>
    internal sealed class UpdateValue : VariableUsage
    {
        public UpdateValue(VariableReference variable, int inputIndex)
            : base(variable, inputIndex)
        { }

        public int InputIndex => Index;

        public override string ToString() => $"UpdateValue {InputIndex} -> v_{Variable.Id}";

        public override T AcceptVisitor<T>(ICodeGenElementVisitor<T> visitor)
        {
            return visitor.VisitUpdateValue(this);
        }
    }
}
