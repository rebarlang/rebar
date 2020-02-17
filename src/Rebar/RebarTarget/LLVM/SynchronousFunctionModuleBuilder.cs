using System.Collections.Generic;
using System.Linq;
using LLVMSharp;

namespace Rebar.RebarTarget.LLVM
{
    internal class SynchronousFunctionModuleBuilder : FunctionModuleBuilder
    {
        private readonly FunctionCompiler _functionCompiler;
        private readonly string _functionName;
        private readonly IEnumerable<AsyncStateGroup> _asyncStateGroups;
        private readonly FunctionAllocationSet _allocationSet;

        public SynchronousFunctionModuleBuilder(
            Module module,
            FunctionCompiler functionCompiler,
            string functionName,
            IEnumerable<AsyncStateGroup> asyncStateGroups)
            : base(module, functionCompiler)
        {
            _functionCompiler = functionCompiler;
            _functionName = functionName;
            _asyncStateGroups = asyncStateGroups;
            _allocationSet = functionCompiler.AllocationSet;

            var parameterTypes = functionCompiler.GetParameterLLVMTypes();
            LLVMTypeRef syncFunctionType = LLVMSharp.LLVM.FunctionType(LLVMSharp.LLVM.VoidType(), parameterTypes.ToArray(), false);
            SyncFunction = Module.AddFunction(FunctionCompiler.GetSynchronousFunctionName(functionName), syncFunctionType);
            SyncFunctionEntryBlock = SyncFunction.AppendBasicBlock("entry");

            foreach (AsyncStateGroup asyncStateGroup in asyncStateGroups)
            {
                LLVMBasicBlockRef groupBasicBlock = SyncFunction.AppendBasicBlock(asyncStateGroup.Label);
                AsyncStateGroups[asyncStateGroup] = new AsyncStateGroupData(asyncStateGroup, SyncFunction, groupBasicBlock, null);
            }
        }

        private LLVMValueRef SyncFunction { get; }

        private LLVMBasicBlockRef SyncFunctionEntryBlock { get; }

        public override void CompileFunction()
        {
            LLVMValueRef outerFunction = InitializeOuterFunction(_functionName, _functionCompiler.GetParameterLLVMTypes());
            var builder = new IRBuilder();
            var outerFunctionCompilerState = new OuterFunctionCompilerState(outerFunction, builder);
            _functionCompiler.CurrentState = outerFunctionCompilerState;
            LLVMBasicBlockRef outerEntryBlock = outerFunction.AppendBasicBlock("entry");
            builder.PositionBuilderAtEnd(outerEntryBlock);

            builder.CreateCall(SyncFunction, outerFunction.GetParams().Skip(2).ToArray(), string.Empty);
            // activate the caller waker
            // TODO: invoke caller waker directly, or schedule?
            builder.CreateCall(outerFunction.GetParam(0u), new LLVMValueRef[] { outerFunction.GetParam(1u) }, string.Empty);
            builder.CreateRetVoid();

            var syncBuilder = new IRBuilder();
            syncBuilder.PositionBuilderAtEnd(SyncFunctionEntryBlock);
            string singleFunctionName = _asyncStateGroups.First().FunctionId;
            _allocationSet.InitializeFunctionLocalAllocations(singleFunctionName, syncBuilder);
            _functionCompiler.InitializeParameterAllocations(SyncFunction, syncBuilder);
            syncBuilder.CreateBr(AsyncStateGroups[_asyncStateGroups.First()].InitialBasicBlock);

            foreach (AsyncStateGroup asyncStateGroup in _asyncStateGroups)
            {
                AsyncStateGroupData groupData = AsyncStateGroups[asyncStateGroup];
                CompileAsyncStateGroup(asyncStateGroup, new AsyncStateGroupCompilerState(SyncFunction, syncBuilder));
            }
        }
    }
}
