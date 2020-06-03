using System;
using LLVMSharp;

namespace Rebar.RebarTarget.LLVM.CodeGen
{
    /// <summary>
    /// Represents a generic code operation that will read and write values from a <see cref="ICodeGenValueStorage"/>.
    /// </summary>
    internal sealed class Op : CodeGenElement
    {
        public Op(Action<IRBuilder, ICodeGenValueStorage> generateOp)
        {
            GenerateOp = generateOp;
        }

        public Action<IRBuilder, ICodeGenValueStorage> GenerateOp { get; }

        public override T AcceptVisitor<T>(ICodeGenElementVisitor<T> visitor)
        {
            return visitor.VisitOp(this);
        }
    }
}
