using Rebar.Common;

namespace Rebar.RebarTarget.LLVM.CodeGen
{
    /// <summary>
    /// Represents initializing a <see cref="VariableReference"/> as a reference to another <see cref="VariableReference"/>.
    /// </summary>
    internal sealed class InitializeAsReference : CodeGenElement
    {
        public InitializeAsReference(VariableReference referencedVariable, VariableReference initializedVariable)
        {
            ReferencedVariable = referencedVariable;
            InitializedVariable = initializedVariable;
        }

        public VariableReference ReferencedVariable { get; }

        public VariableReference InitializedVariable { get; }

        public override string ToString() => $"Reference(v_{ReferencedVariable.Id}) -> v_{InitializedVariable.Id}";

        public override T AcceptVisitor<T>(ICodeGenElementVisitor<T> visitor)
        {
            return visitor.VisitInitializeAsReference(this);
        }
    }
}
