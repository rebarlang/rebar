using NationalInstruments.Dfir;
using Rebar.Compiler.Nodes;
using Rebar.RebarTarget.Execution;
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

        public void CompileAndExecuteFunction(CompilerTestBase test, DfirRoot function)
        {
            Function compiledFunction = test.RunSemanticAnalysisUpToCodeGeneration(function);
            _context.LoadFunction(compiledFunction);
            _context.FinalizeLoad();
            _context.ExecuteFunctionTopLevel(compiledFunction.Name);
        }

        public byte[] GetLastValueFromInspectNode(FunctionalNode inspectNode)
        {
            return _context.ReadStaticData(StaticDataIdentifier.CreateFromNode(inspectNode));
        }
    }
}
