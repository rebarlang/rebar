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
            var calleesIsYielding = new Dictionary<ExtendedQualifiedName, bool>();
            foreach (DfirRoot otherFunction in otherFunctions)
            {
                FunctionCompileResult otherCompileResult = test.RunSemanticAnalysisUpToLLVMCodeGeneration(
                    otherFunction,
                    FunctionCompileHandler.FunctionLLVMName(otherFunction.SpecAndQName),
                    calleesIsYielding);
                calleesIsYielding[otherFunction.SpecAndQName.QualifiedName] = otherCompileResult.IsYielding;
                _context.LoadFunction(otherCompileResult.Module, otherCompileResult.CommonModuleDependencies);
            }

            const string compiledFunctionName = "test";
            FunctionCompileResult compileResult = test.RunSemanticAnalysisUpToLLVMCodeGeneration(function, compiledFunctionName, calleesIsYielding);
            _context.LoadFunction(compileResult.Module, compileResult.CommonModuleDependencies);
            _context.ExecuteFunctionTopLevel(compiledFunctionName);
        }

        public byte[] GetLastValueFromInspectNode(FunctionalNode inspectNode)
        {
            string globalName = $"inspect_{inspectNode.UniqueId}";
            return _context.ReadGlobalData(globalName);
        }
    }
}
