using NationalInstruments.Dfir;
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
            const string compiledFunctionName = "test";
            _context.LoadFunction(test.RunSemanticAnalysisUpToLLVMCodeGeneration(function, compiledFunctionName));
            foreach (DfirRoot otherFunction in otherFunctions)
            {
                _context.LoadFunction(test.RunSemanticAnalysisUpToLLVMCodeGeneration(
                    otherFunction,
                    FunctionCompileHandler.FunctionLLVMName(otherFunction.SpecAndQName)));
            }
            _context.ExecuteFunctionTopLevel(compiledFunctionName);
        }

        public byte[] GetLastValueFromInspectNode(FunctionalNode inspectNode)
        {
            string globalName = $"inspect_{inspectNode.UniqueId}";
            return _context.ReadGlobalData(globalName);
        }
    }
}
