using Rebar.Common;

namespace Rebar.RebarTarget.LLVM.CodeGen
{
    /// <summary>
    /// Annotation that denotes that two <see cref="VariableReference"/>s will share a value.
    /// </summary>
    internal sealed class ShareValue : CodeGenElement
    {
        public ShareValue(VariableReference provider, VariableReference user)
        {
            Provider = provider;
            User = user;
        }

        public VariableReference Provider { get; }

        public VariableReference User { get; }

        public override string ToString() => $"ShareValue (v_{Provider.Id}, v_{User.Id})";

        public override T AcceptVisitor<T>(ICodeGenElementVisitor<T> visitor)
        {
            return visitor.VisitShareValue(this);
        }
    }
}
