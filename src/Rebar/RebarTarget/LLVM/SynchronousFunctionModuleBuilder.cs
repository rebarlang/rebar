using System.Collections.Generic;
using LLVMSharp;

namespace Rebar.RebarTarget.LLVM
{
    internal class SynchronousFunctionModuleBuilder : FunctionModuleBuilder
    {
        public SynchronousFunctionModuleBuilder(
            Module module,
            FunctionCompiler functionCompiler,
            string functionName,
            IEnumerable<AsyncStateGroup> asyncStateGroups)
            : base(module)
        {
            var parameterTypes = functionCompiler.GetParameterLLVMTypes();
            LLVMTypeRef syncFunctionType = LLVMSharp.LLVM.FunctionType(LLVMSharp.LLVM.VoidType(), parameterTypes.ToArray(), false);
            SyncFunction = Module.AddFunction(FunctionCompiler.GetSynchronousFunctionName(functionName), syncFunctionType);
            SyncFunctionEntryBlock = SyncFunction.AppendBasicBlock("entry");

            var functions = new Dictionary<string, LLVMValueRef>();
            foreach (AsyncStateGroup asyncStateGroup in asyncStateGroups)
            {
                LLVMValueRef groupFunction = SyncFunction;

                LLVMBasicBlockRef groupBasicBlock = groupFunction.AppendBasicBlock(asyncStateGroup.Label);
                functionCompiler.AsyncStateGroups[asyncStateGroup] = new FunctionCompiler.AsyncStateGroupData(asyncStateGroup, groupFunction, groupBasicBlock, null);
            }
        }

        public LLVMValueRef SyncFunction { get; }

        public LLVMBasicBlockRef SyncFunctionEntryBlock { get; }
    }
}
