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
            IReadOnlyList<ParameterInfo> orderedParameters,
            FunctionAllocationSet allocationSet,
            FunctionVariableStorage variableStorage,
            CommonExternalFunctions commonExternalFunctions,
            FunctionImporter functionImporter)
        {
            OrderedParameters = orderedParameters;
            AllocationSet = allocationSet;
            VariableStorage = variableStorage;
            CommonExternalFunctions = commonExternalFunctions;
            FunctionImporter = functionImporter;
        }

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

        public CommonExternalFunctions CommonExternalFunctions { get; }

        public FunctionImporter FunctionImporter { get; }
    }
}
