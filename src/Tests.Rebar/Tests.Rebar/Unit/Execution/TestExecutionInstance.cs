using System.Collections.Generic;
using NationalInstruments.Dfir;
using NationalInstruments.Linking;
using Rebar.Compiler.Nodes;
using Rebar.RebarTarget;
using Rebar.RebarTarget.LLVM;
using Tests.Rebar.Unit.Compiler;

namespace Tests.Rebar.Unit.Execution
{
    internal class TestExecutionInstance
    {
        private readonly ExecutionContext _context;

        public TestExecutionInstance()
        {
            RuntimeServices = new TestRuntimeServices();
            _context = new ExecutionContext(RuntimeServices);
        }

        public TestRuntimeServices RuntimeServices { get; }

        public void CompileAndExecuteFunction(CompilerTestBase test, DfirRoot function, DfirRoot[] otherFunctions)
        {
            ExecuteFunction(CompileAndLoadFunction(test, function, otherFunctions));
        }

        public void ExecuteFunction(CompileLoadResult loadResult)
        {
            _context.ExecuteFunctionTopLevel(loadResult.TopLevelCompiledFunctionName, loadResult.TopLevelFunctionIsYielding);
        }

        public CompileLoadResult CompileAndLoadFunction(CompilerTestBase test, DfirRoot function, DfirRoot[] otherFunctions)
        {
            var calleesIsYielding = new Dictionary<ExtendedQualifiedName, bool>();
            var calleesMayPanic = new Dictionary<ExtendedQualifiedName, bool>();
            foreach (DfirRoot otherFunction in otherFunctions)
            {
                FunctionCompileResult otherCompileResult = test.RunSemanticAnalysisUpToLLVMCodeGeneration(
                    otherFunction,
                    FunctionCompileHandler.FunctionLLVMName(otherFunction.SpecAndQName),
                    calleesIsYielding,
                    calleesMayPanic);
                calleesIsYielding[otherFunction.SpecAndQName.QualifiedName] = otherCompileResult.IsYielding;
                calleesMayPanic[otherFunction.SpecAndQName.QualifiedName] = otherCompileResult.MayPanic;
                _context.LoadFunction(otherCompileResult.Module);
            }

            const string compiledFunctionName = "test";
            FunctionCompileResult compileResult = test.RunSemanticAnalysisUpToLLVMCodeGeneration(function, compiledFunctionName, calleesIsYielding, calleesMayPanic);
            _context.LoadFunction(compileResult.Module);
            return new CompileLoadResult(compiledFunctionName, compileResult.IsYielding);
        }

        public byte[] GetLastValueFromInspectNode(FunctionalNode inspectNode)
        {
            string globalName = $"inspect_{inspectNode.UniqueId}";
            return _context.ReadGlobalData(globalName);
        }
    }

    internal sealed class CompileLoadResult
    {
        public CompileLoadResult(string topLevelCompiledFunctionName, bool topLevelFunctionIsYielding)
        {
            TopLevelCompiledFunctionName = topLevelCompiledFunctionName;
            TopLevelFunctionIsYielding = topLevelFunctionIsYielding;
        }

        public string TopLevelCompiledFunctionName { get; }

        public bool TopLevelFunctionIsYielding { get; }
    }
}
