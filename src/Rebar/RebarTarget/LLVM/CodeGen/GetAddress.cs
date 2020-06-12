using Rebar.Common;

namespace Rebar.RebarTarget.LLVM.CodeGen
{
    /// <summary>
    /// Represents getting and outputing the address associated with a <see cref="VariableReference"/>.
    /// </summary>
    internal sealed class GetAddress : VariableUsage
    {
        public GetAddress(VariableReference variable, int outputIndex, bool forInitialize = false)
            : base(variable, outputIndex)
        {
            ForInitialize = forInitialize;
        }

        public int OutputIndex => Index;

        public bool ForInitialize { get; }

        public override string ToString()
        {
            string init = ForInitialize ? "init" : string.Empty;
            return $"GetAddress {init} v_{Variable.Id} -> {OutputIndex}";
        }

        public override T AcceptVisitor<T>(ICodeGenElementVisitor<T> visitor)
        {
            return visitor.VisitGetAddress(this);
        }
    }
}
