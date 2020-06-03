using System;
using LLVMSharp;

namespace Rebar.RebarTarget.LLVM.CodeGen
{
    /// <summary>
    /// Represents building and outputing a constant value.
    /// </summary>
    internal sealed class GetConstant : CodeGenElement
    {
        public GetConstant(Func<FunctionModuleContext, IRBuilder, LLVMValueRef> valueCreator, int outputIndex)
        {
            ValueCreator = valueCreator;
            OutputIndex = outputIndex;
        }

        public Func<FunctionModuleContext, IRBuilder, LLVMValueRef> ValueCreator { get; }

        public int OutputIndex { get; }

        public override string ToString() => $"Constant -> {OutputIndex}";

        public override T AcceptVisitor<T>(ICodeGenElementVisitor<T> visitor)
        {
            return visitor.VisitGetConstant(this);
        }
    }
}
