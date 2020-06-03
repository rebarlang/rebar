using Rebar.Common;

namespace Rebar.RebarTarget.LLVM.CodeGen
{
    /// <summary>
    /// Represents getting and outputing the value pointed to by a <see cref="VariableReference"/>.
    /// </summary>
    internal sealed class GetDereferencedValue : VariableUsage
    {
        public GetDereferencedValue(VariableReference variable, int outputIndex)
            : base(variable, outputIndex)
        { }

        public int OutputIndex => Index;

        public override string ToString() => $"GetDereferencedValue v_{Variable.Id} -> *{OutputIndex}";

        public override T AcceptVisitor<T>(ICodeGenElementVisitor<T> visitor)
        {
            return visitor.VisitGetDereferencedValue(this);
        }
    }
}
