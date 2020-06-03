using System.Collections.Generic;
using Rebar.Common;

namespace Rebar.RebarTarget.LLVM
{
    internal class FunctionVariableStorage
    {
        private readonly Dictionary<VariableReference, ValueSource> _variableValues = VariableReference.CreateDictionaryWithUniqueVariableKeys<ValueSource>();

        public void AddValueSourceForVariable(VariableReference variableReference, ValueSource valueSource)
        {
            _variableValues[variableReference] = valueSource;
        }

        public ValueSource GetValueSourceForVariable(VariableReference variableReference)
        {
            return _variableValues[variableReference];
        }
    }
}
