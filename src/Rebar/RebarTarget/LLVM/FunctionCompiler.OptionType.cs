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
            LLVMTypeRef outputType = someConstructorNode.OutputTerminals[0].GetTrueVariable().Type.AsLLVMType();
            LLVMValueRef innerValue = inputSource.GetValue(compiler.Builder);
            compiler.Initialize(outputSource, compiler.Builder.BuildOptionValue(outputType, innerValue));
        }

        private static void CompileNoneConstructor(FunctionCompiler compiler, FunctionalNode noneConstructorNode)
        {
            ValueSource outputSource = compiler.GetTerminalValueSource(noneConstructorNode.OutputTerminals[0]);
            LLVMTypeRef outputType = noneConstructorNode
                .OutputTerminals[0].GetTrueVariable()
                .Type.AsLLVMType();
            compiler.InitializeIfNecessary(outputSource, builder => builder.BuildOptionValue(outputType, null));
        }

        internal static void BuildOptionDropFunction(FunctionCompiler compiler, NIType signature, LLVMValueRef optionDropFunction)
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
