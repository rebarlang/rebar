using Rebar.Common;

namespace Rebar.RebarTarget.LLVM.CodeGen
{
    /// <summary>
    /// Represents getting and outputing the current value of a <see cref="VariableReference"/>.
    /// </summary>
    internal sealed class GetValue : VariableUsage
    {
        public GetValue(VariableReference variable, int outputIndex)
            : base(variable, outputIndex)
        { }

        public int OutputIndex => Index;

        public override string ToString() => $"GetValue v_{Variable.Id} -> {OutputIndex}";

        public override T AcceptVisitor<T>(ICodeGenElementVisitor<T> visitor)
        {
            return visitor.VisitGetValue(this);
        }
    }
}
