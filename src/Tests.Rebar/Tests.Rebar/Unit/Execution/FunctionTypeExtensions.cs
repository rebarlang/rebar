using NationalInstruments.Compiler;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using NationalInstruments.ExecutionFramework;

namespace Tests.Rebar.Unit.Execution
{
    internal static class FunctionTypeExtensions
    {
        public static NIFunctionBuilder DefineMethodType(this string functionName)
        {
            NIFunctionBuilder functionBuilder = NITypes.Factory.DefineFunction(functionName);
            functionBuilder.IsStatic = true;
            return functionBuilder;
        }

        public static DfirRoot CreateFunctionFromSignature(this NIType functionSignature, CompilableDefinitionName functionDefinitionName)
        {
            DfirRoot function = DfirRoot.Create(new CompileSpecification(functionDefinitionName, null));
            int connectorPaneIndex = 0;
            foreach (NIType parameter in functionSignature.GetParameters())
            {
                function.CreateDataItem(
                    parameter.GetName(),
                    parameter.GetDataType(),
                    null,   // defaultValue
                    parameter.GetInputParameterPassingRule(),
                    parameter.GetOutputParameterPassingRule(),
                    connectorPaneIndex++);
            }
            return function;
        }
    }
}
