using System;
using System.Collections.Generic;
using System.Linq;
using LLVMSharp;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Compiler;

namespace Rebar.RebarTarget.LLVM
{
    internal partial class FunctionCompiler
    {
        internal Dictionary<AsyncStateGroup, AsyncStateGroupData> AsyncStateGroups { get; }

        public void CompileFunction(DfirRoot dfirRoot)
        {
            TargetDfir = dfirRoot;
            _moduleBuilder.CompileFunction();
        }

        internal List<LLVMTypeRef> GetParameterLLVMTypes()
        {
            var parameterTypes = new List<LLVMTypeRef>();
            foreach (var dataItem in _parameterDataItems.OrderBy(d => d.ConnectorPaneIndex))
            {
                if (dataItem.ConnectorPaneInputPassingRule == NIParameterPassingRule.Required
                    && dataItem.ConnectorPaneOutputPassingRule == NIParameterPassingRule.NotAllowed)
                {
                    parameterTypes.Add(dataItem.DataType.AsLLVMType());
                }
                else if (dataItem.ConnectorPaneInputPassingRule == NIParameterPassingRule.NotAllowed
                    && dataItem.ConnectorPaneOutputPassingRule == NIParameterPassingRule.Optional)
                {
                    parameterTypes.Add(LLVMTypeRef.PointerType(dataItem.DataType.AsLLVMType(), 0u));
                }
                else
                {
                    throw new NotImplementedException("Can only handle in and out parameters");
                }
            }
            return parameterTypes;
        }

        internal static string GetSynchronousFunctionName(string functionName)
        {
            return $"{functionName}::sync";
        }

        internal static string GetInitializeStateFunctionName(string runtimeFunctionName)
        {
            return $"{runtimeFunctionName}::InitializeState";
        }

        internal static string GetPollFunctionName(string runtimeFunctionName)
        {
            return $"{runtimeFunctionName}::Poll";
        }

        internal void InitializeParameterAllocations(LLVMValueRef function, IRBuilder builder)
        {
            uint parameterIndex = 0u;
            foreach (var dataItem in _parameterDataItems.OrderBy(d => d.ConnectorPaneIndex))
            {
                LLVMValueRef parameterAllocationPtr = GetAddress(_variableValues[dataItem.GetVariable()], builder);
                builder.CreateStore(function.GetParam(parameterIndex), parameterAllocationPtr);
                ++parameterIndex;
            }
        }
    }
}
