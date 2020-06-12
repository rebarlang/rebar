using Rebar.Common;

namespace Rebar.RebarTarget.LLVM.CodeGen
{
    /// <summary>
    /// Represents updating the value pointed to by a <see cref="VariableReference"/> to an input value.
    /// </summary>
    internal sealed class UpdateDereferencedValue : VariableUsage
    {
        public UpdateDereferencedValue(VariableReference variable, int inputIndex)
            : base(variable, inputIndex)
        { }

        public int InputIndex => Index;

        public override string ToString() => $"UpdateDereferencedValue {InputIndex} -> *v_{Variable.Id}";

        public override T AcceptVisitor<T>(ICodeGenElementVisitor<T> visitor)
        {
            return visitor.VisitUpdateDereferencedValue(this);
        }
    }
}
