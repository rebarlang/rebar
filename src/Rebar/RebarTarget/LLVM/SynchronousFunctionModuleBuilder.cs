using System.Collections.Generic;
using System.Linq;
using LLVMSharp;

namespace Rebar.RebarTarget.LLVM
{
    internal class SynchronousFunctionModuleBuilder : FunctionModuleBuilder
    {
        private readonly string _functionName;
        private readonly IEnumerable<AsyncStateGroup> _asyncStateGroups;

        public SynchronousFunctionModuleBuilder(
            Module module,
            FunctionCompilerSharedData sharedData,
            string functionName,
            IEnumerable<AsyncStateGroup> asyncStateGroups)
            : base(module, sharedData)
        {
            _functionName = functionName;
            _asyncStateGroups = asyncStateGroups;

            var parameterTypes = GetParameterLLVMTypes();
            LLVMTypeRef syncFunctionType = LLVMSharp.LLVM.FunctionType(LLVMSharp.LLVM.VoidType(), parameterTypes.ToArray(), false);
            SyncFunction = Module.AddFunction(FunctionNames.GetSynchronousFunctionName(functionName), syncFunctionType);
            SyncFunctionEntryBlock = SyncFunction.AppendBasicBlock("entry");

            foreach (AsyncStateGroup asyncStateGroup in asyncStateGroups)
            {
                LLVMBasicBlockRef groupBasicBlock = SyncFunction.AppendBasicBlock($"{asyncStateGroup.Label}_begin");
                LLVMBasicBlockRef continueBasicBlock = asyncStateGroup.IsSkippable
                    ? SyncFunction.AppendBasicBlock($"{asyncStateGroup.Label}_continue")
                    : default(LLVMBasicBlockRef);
                LLVMBasicBlockRef endBasicBlock = asyncStateGroup.IsSkippable
                    ? SyncFunction.AppendBasicBlock($"{asyncStateGroup.Label}_end")
                    : default(LLVMBasicBlockRef);
                AsyncStateGroups[asyncStateGroup] = new AsyncStateGroupData(asyncStateGroup, SyncFunction, groupBasicBlock, continueBasicBlock, endBasicBlock, null);
            }
        }

        private LLVMValueRef SyncFunction { get; }

        private LLVMBasicBlockRef SyncFunctionEntryBlock { get; }

        public override void CompileFunction()
        {
            var syncBuilder = new IRBuilder();
            syncBuilder.PositionBuilderAtEnd(SyncFunctionEntryBlock);
            string singleFunctionName = _asyncStateGroups.First().FunctionId;
            SharedData.AllocationSet.InitializeFunctionLocalAllocations(singleFunctionName, syncBuilder);
            InitializeParameterAllocations(SyncFunction, syncBuilder);
            syncBuilder.CreateBr(AsyncStateGroups[_asyncStateGroups.First()].InitialBasicBlock);

            foreach (AsyncStateGroup asyncStateGroup in _asyncStateGroups)
            {
                AsyncStateGroupData groupData = AsyncStateGroups[asyncStateGroup];
                CompileAsyncStateGroup(asyncStateGroup, new AsyncStateGroupCompilerState(SyncFunction, syncBuilder));
            }
        }
    }
}
