using Rebar.Common;

namespace Rebar.RebarTarget.LLVM.CodeGen
{
    /// <summary>
    /// Represents initializing a <see cref="VariableReference"/> with a copy of another <see cref="VariableReference"/>'s value.
    /// </summary>
    internal sealed class InitializeWithCopy : CodeGenElement
    {
        public InitializeWithCopy(VariableReference copiedVariable, VariableReference initializedVariable)
        {
            CopiedVariable = copiedVariable;
            InitializedVariable = initializedVariable;
        }

        public VariableReference CopiedVariable { get; }

        public VariableReference InitializedVariable { get; }

        public override string ToString() => $"Copy(v_{CopiedVariable.Id}) -> v_{InitializedVariable.Id}";

        public override T AcceptVisitor<T>(ICodeGenElementVisitor<T> visitor)
        {
            return visitor.VisitInitializeWithCopy(this);
        }
    }
}
