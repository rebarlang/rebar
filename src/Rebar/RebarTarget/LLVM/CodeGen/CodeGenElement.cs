namespace Rebar.RebarTarget.LLVM.CodeGen
{
    /// <summary>
    /// Base class for primitive operations that are used to analyze and generate code for a <see cref="Function"/>.
    /// </summary>
    internal abstract class CodeGenElement : Visitation
    {
        public abstract T AcceptVisitor<T>(ICodeGenElementVisitor<T> visitor);
    }
}
