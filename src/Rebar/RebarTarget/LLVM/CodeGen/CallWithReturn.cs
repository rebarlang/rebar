using LLVMSharp;

namespace Rebar.RebarTarget.LLVM.CodeGen
{
    /// <summary>
    /// Represents calling a function with a range of input values and outputing its return value.
    /// </summary>
    internal sealed class CallWithReturn : CodeGenElement
    {
        public CallWithReturn(LLVMValueRef function, int[] argumentIndices, int returnValueIndex)
        {
            Function = function;
            ArgumentIndices = argumentIndices;
            ReturnValueIndex = returnValueIndex;
        }

        public LLVMValueRef Function { get; }

        public int[] ArgumentIndices { get; }

        public int ReturnValueIndex { get; }

        public override string ToString() => $"Call {Function.GetValueName()}({string.Join(", ", ArgumentIndices)}) -> {ReturnValueIndex}";

        public override T AcceptVisitor<T>(ICodeGenElementVisitor<T> visitor)
        {
            return visitor.VisitCallWithReturn(this);
        }
    }
}
