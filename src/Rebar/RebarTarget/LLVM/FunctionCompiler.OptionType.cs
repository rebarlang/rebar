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
            LLVMTypeRef outputType = compiler.Context.AsLLVMType(someConstructorNode.OutputTerminals[0].GetTrueVariable().Type);
            LLVMValueRef innerValue = inputSource.GetValue(compiler.Builder);
            compiler.Initialize(outputSource, compiler.Context.BuildOptionValue(compiler.Builder, outputType, innerValue));
        }

        private static void CompileNoneConstructor(FunctionCompiler compiler, FunctionalNode noneConstructorNode)
        {
            ValueSource outputSource = compiler.GetTerminalValueSource(noneConstructorNode.OutputTerminals[0]);
            LLVMTypeRef outputType = compiler.Context.AsLLVMType(noneConstructorNode.OutputTerminals[0].GetTrueVariable().Type);
            compiler.InitializeIfNecessary(outputSource, builder => compiler.Context.BuildOptionValue(builder, outputType, null));
        }

        internal static void BuildOptionDropFunction(FunctionCompiler compiler, NIType signature, LLVMValueRef optionDropFunction)
        {
            NIType innerType;
            signature.GetGenericParameters().First().TryDestructureOptionType(out innerType);

            LLVMBasicBlockRef entryBlock = optionDropFunction.AppendBasicBlock("entry"),
                isSomeBlock = optionDropFunction.AppendBasicBlock("isSome"),
                endBlock = optionDropFunction.AppendBasicBlock("end");
            var builder = compiler.Context.CreateIRBuilder();

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

        private static void BuildOptionToPanicResultFunction(FunctionCompiler compiler, NIType signature, LLVMValueRef optionToPanicResultFunction)
        {
            LLVMTypeRef elementLLVMType = compiler.Context.AsLLVMType(signature.GetGenericParameters().First());

            LLVMBasicBlockRef entryBlock = optionToPanicResultFunction.AppendBasicBlock("entry"),
                someBlock = optionToPanicResultFunction.AppendBasicBlock("some"),
                noneBlock = optionToPanicResultFunction.AppendBasicBlock("none");
            var builder = compiler.Context.CreateIRBuilder();

            builder.PositionBuilderAtEnd(entryBlock);
            LLVMValueRef option = optionToPanicResultFunction.GetParam(0u),
                isSome = builder.CreateExtractValue(option, 0u, "isSome");
            LLVMValueRef branch = builder.CreateCondBr(isSome, someBlock, noneBlock);
            // TODO: if possible, set metadata that indicates the some branch is more likely to be taken

            LLVMTypeRef panicResultType = compiler.Context.CreateLLVMPanicResultType(elementLLVMType);
            builder.PositionBuilderAtEnd(someBlock);
            LLVMValueRef innerValue = builder.CreateExtractValue(option, 1u, "innerValue");
            LLVMValueRef panicContinueResult = builder.BuildStructValue(panicResultType, new LLVMValueRef[] { compiler.Context.AsLLVMValue(true), innerValue }, "panicContinueResult");
            builder.CreateStore(panicContinueResult, optionToPanicResultFunction.GetParam(1u));
            builder.CreateRetVoid();

            builder.PositionBuilderAtEnd(noneBlock);
            LLVMValueRef panicResult = LLVMSharp.LLVM.ConstNull(panicResultType);
            builder.CreateStore(panicResult, optionToPanicResultFunction.GetParam(1u));
            builder.CreateRetVoid();
        }
    }
}
