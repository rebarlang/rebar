using System.Linq;
using LLVMSharp;
using NationalInstruments.DataTypes;
using Rebar.Common;
using Rebar.Compiler;
using Rebar.Compiler.Nodes;

namespace Rebar.RebarTarget.LLVM
{
    internal partial class FunctionCompiler
    {
        private static void CompileSomeConstructor(FunctionCompiler compiler, FunctionalNode someConstructorNode)
        {
            ValueSource inputSource = compiler.GetTerminalValueSource(someConstructorNode.InputTerminals[0]),
                outputSource = compiler.GetTerminalValueSource(someConstructorNode.OutputTerminals[0]);
            // CreateSomeValueStruct creates a const struct, which isn't allowed since
            // inputSource.GetValue isn't always a constant.

            LLVMValueRef[] someFieldValues = new[]
            {
                true.AsLLVMValue(),
                inputSource.GetValue(compiler._builder)
            };
            ((LocalAllocationValueSource)outputSource).UpdateStructValue(compiler._builder, someFieldValues);
        }

        private static void CompileNoneConstructor(FunctionCompiler compiler, FunctionalNode noneConstructorNode)
        {
            ValueSource outputSource = compiler.GetTerminalValueSource(noneConstructorNode.OutputTerminals[0]);
            LLVMTypeRef outputType = noneConstructorNode
                .OutputTerminals[0].GetTrueVariable()
                .Type.AsLLVMType();
            outputSource.UpdateValue(compiler._builder, LLVMSharp.LLVM.ConstNull(outputType));
        }

        private static void BuildOptionDropFunction(FunctionCompiler compiler, NIType signature, LLVMValueRef optionDropFunction)
        {
            NIType innerType;
            signature.GetGenericParameters().First().TryDestructureOptionType(out innerType);

            LLVMBasicBlockRef entryBlock = optionDropFunction.AppendBasicBlock("entry"),
                isSomeBlock = optionDropFunction.AppendBasicBlock("isSome"),
                endBlock = optionDropFunction.AppendBasicBlock("end");
            var builder = new IRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef optionPtr = optionDropFunction.GetParam(0u),
                isSomePtr = builder.CreateStructGEP(optionPtr, 0u, "isSomePtr"),
                isSome = builder.CreateLoad(isSomePtr, "isSome");
            builder.CreateCondBr(isSome, isSomeBlock, endBlock);

            builder.PositionBuilderAtEnd(isSomeBlock);
            compiler.CreateDropCallIfDropFunctionExists(builder, innerType, b => b.CreateStructGEP(optionPtr, 1u, "innerValuePtr"));
            builder.CreateBr(endBlock);

            builder.PositionBuilderAtEnd(endBlock);
            builder.CreateRetVoid();
        }
    }
}
