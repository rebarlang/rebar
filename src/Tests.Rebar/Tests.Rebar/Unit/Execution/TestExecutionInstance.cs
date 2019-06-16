#define LLVM_TEST

using System;
using NationalInstruments.Core;
using NationalInstruments.Dfir;
using NationalInstruments.Linking;
using Rebar.Compiler.Nodes;
#if LLVM_TEST
using Rebar.RebarTarget.LLVM;
#else
using Rebar.RebarTarget.Execution;
#endif
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
#if LLVM_TEST
            const string compiledFunctionName = "test";
            LLVMSharp.Module compiledFunction = test.RunSemanticAnalysisUpToLLVMCodeGeneration(function, compiledFunctionName);
            _context.LoadFunction(compiledFunction);
            _context.ExecuteFunctionTopLevel(compiledFunctionName);
#else
            Function compiledFunction = test.RunSemanticAnalysisUpToCodeGeneration(function);
            _context.LoadFunction(compiledFunction);
            _context.FinalizeLoad();
            _context.ExecuteFunctionTopLevel(compiledFunction.Name);
#endif
        }

        public byte[] GetLastValueFromInspectNode(FunctionalNode inspectNode)
        {
#if LLVM_TEST
            string globalName = $"inspect_{inspectNode.UniqueId}";
            return _context.ReadGlobalData(globalName);
#else
            return _context.ReadStaticData(StaticDataIdentifier.CreateFromNode(inspectNode));
#endif
        }
    }
}
