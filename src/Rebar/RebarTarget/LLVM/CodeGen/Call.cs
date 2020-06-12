using LLVMSharp;

namespace Rebar.RebarTarget.LLVM.CodeGen
{
    /// <summary>
    /// Represents calling a function with no return value with a range of input values.
    /// </summary>
    internal sealed class Call : CodeGenElement
    {
        public Call(LLVMValueRef function, int[] argumentIndices)
        {
            Function = function;
            ArgumentIndices = argumentIndices;
        }

        public LLVMValueRef Function { get; }

        public int[] ArgumentIndices { get; }

        public override string ToString() => $"Call {Function.GetValueName()}({string.Join(", ", ArgumentIndices)})";

        public override T AcceptVisitor<T>(ICodeGenElementVisitor<T> visitor)
        {
            return visitor.VisitCall(this);
        }
    }
}
