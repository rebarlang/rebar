using System.Collections.Generic;
using Rebar.Common;

namespace Rebar.RebarTarget.LLVM
{
    internal class FunctionVariableStorage
    {
        private readonly Dictionary<VariableReference, ValueSource> _variableValues = VariableReference.CreateDictionaryWithUniqueVariableKeys<ValueSource>();
        private readonly Dictionary<object, ValueSource> _additionalValues = new Dictionary<object, ValueSource>();
        private readonly Dictionary<AsyncStateGroup, LocalAllocationValueSource> _continuationConditionVariables = new Dictionary<AsyncStateGroup, LocalAllocationValueSource>();

        public void AddValueSourceForVariable(VariableReference variableReference, ValueSource valueSource)
        {
            _variableValues[variableReference] = valueSource;
        }

        public ValueSource GetValueSourceForVariable(VariableReference variableReference)
        {
            return _variableValues[variableReference];
        }

        public void AddAdditionalValueSource(object key, ValueSource valueSource)
        {
            _additionalValues[key] = valueSource;
        }

        public ValueSource GetAdditionalValueSource(object key)
        {
            return _additionalValues[key];
        }

        public void AddContinuationConditionVariable(AsyncStateGroup group, LocalAllocationValueSource allocation)
        {
            _continuationConditionVariables[group] = allocation;
        }

        public LocalAllocationValueSource GetContinuationConditionVariable(AsyncStateGroup group)
        {
            return _continuationConditionVariables[group];
        }
    }
}
