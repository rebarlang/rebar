using System.Collections.Generic;

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
            IReadOnlyList<ParameterInfo> orderedParameters,
            FunctionAllocationSet allocationSet,
            FunctionVariableStorage variableStorage,
            FunctionImporter functionImporter)
        {
            Context = context;
            OrderedParameters = orderedParameters;
            AllocationSet = allocationSet;
            VariableStorage = variableStorage;
            FunctionImporter = functionImporter;
        }

        public ContextWrapper Context { get; }

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
    }
}
