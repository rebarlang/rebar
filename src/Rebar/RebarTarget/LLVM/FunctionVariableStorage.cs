using System.Collections.Generic;
using Rebar.Common;

namespace Rebar.RebarTarget.LLVM
{
    internal class FunctionVariableStorage
    {
        private readonly Dictionary<VariableReference, ValueSource> _variableValues;
        private readonly Dictionary<object, ValueSource> _additionalValues;

        public FunctionVariableStorage(Dictionary<VariableReference, ValueSource> variableValues, Dictionary<object, ValueSource> additionalValues)
        {
            _variableValues = variableValues;
            _additionalValues = additionalValues;
        }

        public ValueSource GetValueSourceForVariable(VariableReference variableReference)
        {
            return _variableValues[variableReference];
        }

        public ValueSource GetAdditionalValueSource(object key)
        {
            return _additionalValues[key];
        }
    }
}
