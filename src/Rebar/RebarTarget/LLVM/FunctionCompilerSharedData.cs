using System.Collections.Generic;
using LLVMSharp;

namespace Rebar.RebarTarget.LLVM
{
    /// <summary>
    /// Data related to the compilation of a Function shared by <see cref="FunctionCompiler"/> and <see cref="FunctionModuleBuilder"/>.
    /// </summary>
    internal class FunctionCompilerSharedData
    {
        private FunctionCompilerState _currentState;

        public FunctionCompilerSharedData(
            ContextWrapper context,
            Module module,
            IReadOnlyList<ParameterInfo> orderedParameters,
            FunctionAllocationSet allocationSet,
            FunctionVariableStorage variableStorage,
            FunctionImporter functionImporter)
        {
            Context = context;
            Module = module;
            OrderedParameters = orderedParameters;
            AllocationSet = allocationSet;
            VariableStorage = variableStorage;
            FunctionImporter = functionImporter;
            ModuleContext = new FunctionModuleContext(context, module, functionImporter);
        }

        public ContextWrapper Context { get; }

        public Module Module { get; }

        public IReadOnlyList<ParameterInfo> OrderedParameters { get; }

        public FunctionAllocationSet AllocationSet { get; }

        public FunctionVariableStorage VariableStorage { get; }

        public IVisitationHandler<bool> VisitationHandler { get; set; }

        public FunctionCompilerState CurrentState
        {
            get { return _currentState; }
            set
            {
                _currentState = value;
                AllocationSet.CompilerState = value;
            }
        }

        public FunctionImporter FunctionImporter { get; }

        public FunctionModuleContext ModuleContext { get; }
    }
}
