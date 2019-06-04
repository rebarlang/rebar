using System;
using System.Collections.Generic;
using LLVMSharp;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler;

namespace Rebar.RebarTarget.LLVM
{
    internal class FunctionCompiler : VisitorTransformBase
    {
        private readonly IRBuilder _builder;
        private readonly LLVMValueRef _topLevelFunction;
        private readonly Dictionary<VariableReference, LLVMValueRef> _variableValues = VariableReference.CreateDictionaryWithUniqueVariableKeys<LLVMValueRef>();

        public FunctionCompiler(Module module, string functionName)
        {
            Module = module;

            LLVMTypeRef functionType = LLVMSharp.LLVM.FunctionType(LLVMSharp.LLVM.VoidType(), new LLVMTypeRef[] { }, false);
            _topLevelFunction = Module.AddFunction(functionName, functionType);
            LLVMBasicBlockRef entryBlock = _topLevelFunction.AppendBasicBlock("entry");
            _builder = new IRBuilder();
            _builder.PositionBuilderAtEnd(entryBlock);

            LLVMTypeRef outputIntFunctionType = LLVMSharp.LLVM.FunctionType(LLVMSharp.LLVM.VoidType(), new LLVMTypeRef[] { LLVMTypeRef.Int32Type() }, false);
            LLVMValueRef outputIntFunction = Module.AddFunction("outputInt", outputIntFunctionType);
            outputIntFunction.SetLinkage(LLVMLinkage.LLVMExternalLinkage);
            LLVMValueRef constValue = LLVMSharp.LLVM.ConstInt(
                    LLVMTypeRef.Int32Type(),
                    5,
                    new LLVMBool(0));
            _builder.CreateCall(outputIntFunction, new LLVMValueRef[] { constValue }, "nothing");
            _builder.CreateRetVoid();
        }

        public Module Module { get; }

        protected override void VisitBorderNode(NationalInstruments.Dfir.BorderNode borderNode)
        {
            throw new NotImplementedException();
        }

        protected override void VisitNode(Node node)
        {
            throw new NotImplementedException();
        }

        protected override void VisitWire(Wire wire)
        {
            throw new NotImplementedException();
        }
    }
}
