using System.Collections.Generic;
using NationalInstruments.Core;
using NationalInstruments.Dfir;
using NationalInstruments.ExecutionFramework;
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
            var calleesIsYielding = new Dictionary<CompilableDefinitionName, bool>();
            var calleesMayPanic = new Dictionary<CompilableDefinitionName, bool>();
            foreach (DfirRoot otherFunction in otherFunctions)
            {
                FunctionCompileResult otherCompileResult = test.RunSemanticAnalysisUpToLLVMCodeGeneration(
                    otherFunction,
                    FunctionCompileHandler.FunctionLLVMName(otherFunction.CompileSpecification.Name),
                    calleesIsYielding,
                    calleesMayPanic);
                calleesIsYielding[otherFunction.CompileSpecification.Name] = otherCompileResult.IsYielding;
                calleesMayPanic[otherFunction.CompileSpecification.Name] = otherCompileResult.MayPanic;
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
