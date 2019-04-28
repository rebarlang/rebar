using System.Collections.Generic;
using System.Linq;
using NationalInstruments;
using NationalInstruments.DataTypes;

namespace Rebar.Common
{
    public static class Signatures
    {
        private static NIType AddGenericDataTypeParameter(NIFunctionBuilder functionBuilder, string name)
        {
            var genericTypeParameters = functionBuilder.MakeGenericParameters(name);
            return genericTypeParameters.ElementAt(0).CreateType();
        }

        private static void AddInputParameter(NIFunctionBuilder functionBuilder, NIType parameterType, string name)
        {
            functionBuilder.DefineParameter(parameterType, name, NIParameterPassingRule.Required, NIParameterPassingRule.NotAllowed);
        }

        private static void AddOutputParameter(NIFunctionBuilder functionBuilder, NIType parameterType, string name)
        {
            functionBuilder.DefineParameter(parameterType, name, NIParameterPassingRule.NotAllowed, NIParameterPassingRule.Recommended);
        }

        private static void AddInputOutputParameter(NIFunctionBuilder functionBuilder, NIType parameterType, string name)
        {
            functionBuilder.DefineParameter(parameterType, name, NIParameterPassingRule.Required, NIParameterPassingRule.Recommended);
        }

        static Signatures()
        {
            var functionTypeBuilder = PFTypes.Factory.DefineFunction("ImmutPass");
            var tDataParameter = AddGenericDataTypeParameter(functionTypeBuilder, "TData");
            AddInputOutputParameter(
                functionTypeBuilder,
                tDataParameter.CreateImmutableReference(),
                "valueRef");
            ImmutablePassthroughType = functionTypeBuilder.CreateType();

            functionTypeBuilder = PFTypes.Factory.DefineFunction("MutPass");
            tDataParameter = AddGenericDataTypeParameter(functionTypeBuilder, "TData");
            AddInputOutputParameter(
                functionTypeBuilder,
                tDataParameter.CreateMutableReference(),
                "valueRef");
            MutablePassthroughType = functionTypeBuilder.CreateType();

            functionTypeBuilder = PFTypes.Factory.DefineFunction("CreateCopy");
            // TODO: constrain TData to be Copy
            tDataParameter = AddGenericDataTypeParameter(functionTypeBuilder, "TData");
            AddInputOutputParameter(
                functionTypeBuilder,
                tDataParameter.CreateImmutableReference(),
                "valueRef");
            AddOutputParameter(
                functionTypeBuilder,
                tDataParameter,
                "copy");
            CreateCopyType = functionTypeBuilder.CreateType();

            functionTypeBuilder = PFTypes.Factory.DefineFunction("Output");
            // TODO: allow other types later
            AddInputOutputParameter(
                functionTypeBuilder,
                PFTypes.Int32.CreateImmutableReference(),
                "valueRef");
            OutputType = functionTypeBuilder.CreateType();

            functionTypeBuilder = PFTypes.Factory.DefineFunction("Range");
            AddInputParameter(
                functionTypeBuilder,
                PFTypes.Int32,
                "lowValue");
            AddInputParameter(
                functionTypeBuilder,
                PFTypes.Int32,
                "highValue");
            AddOutputParameter(
                functionTypeBuilder,
                PFTypes.Int32.CreateIterator(),
                "range");
            RangeType = functionTypeBuilder.CreateType();

            functionTypeBuilder = PFTypes.Factory.DefineFunction("VectorCreate");
            // TODO
#if FALSE
            genericTypeParameters = functionTypeBuilder.MakeGenericParameters(
                "TData");
            tDataParameter = genericTypeParameters.ElementAt(0).CreateType();
#endif
            AddOutputParameter(
                functionTypeBuilder,
                PFTypes.Int32.CreateVector(),
                "valueRef");
            VectorCreateType = functionTypeBuilder.CreateType();

            functionTypeBuilder = PFTypes.Factory.DefineFunction("VectorInsert");
            tDataParameter = AddGenericDataTypeParameter(functionTypeBuilder, "TData");
            AddInputOutputParameter(
                functionTypeBuilder,
                tDataParameter.CreateVector().CreateMutableReference(),
                "vectorRef");
            AddInputOutputParameter(
                functionTypeBuilder,
                PFTypes.Int32.CreateImmutableReference(),
                "indexRef");
            AddInputParameter(
                functionTypeBuilder,
                tDataParameter,
                "element");
            VectorInsertType = functionTypeBuilder.CreateType();
        }

        public static NIType ImmutablePassthroughType { get; }

        public static NIType MutablePassthroughType { get; }

        public static NIType CreateCopyType { get; }

        public static NIType OutputType { get; }

        public static NIType RangeType { get; }

        public static NIType VectorCreateType { get; }

        public static NIType VectorInsertType { get; }

        public static NIType DefinePureUnaryFunction(string name, NIType inputType, NIType outputType)
        {
            var functionTypeBuilder = PFTypes.Factory.DefineFunction(name);
            AddInputOutputParameter(
                functionTypeBuilder,
                inputType.CreateImmutableReference(),
                "operandRef");
            AddOutputParameter(
                functionTypeBuilder,
                outputType,
                "result");
            return functionTypeBuilder.CreateType();
        }

        public static NIType DefinePureBinaryFunction(string name, NIType inputType, NIType outputType)
        {
            var functionTypeBuilder = PFTypes.Factory.DefineFunction(name);
            AddInputOutputParameter(
                functionTypeBuilder,
                inputType.CreateImmutableReference(),
                "operand1Ref");
            AddInputOutputParameter(
                functionTypeBuilder,
                inputType.CreateImmutableReference(),
                "operand2Ref");
            AddOutputParameter(
                functionTypeBuilder,
                outputType,
                "result");
            return functionTypeBuilder.CreateType();
        }

        public static NIType DefineMutatingUnaryFunction(string name, NIType inputType)
        {
            var functionTypeBuilder = PFTypes.Factory.DefineFunction(name);
            AddInputOutputParameter(
                functionTypeBuilder,
                inputType.CreateMutableReference(),
                "operandRef");
            return functionTypeBuilder.CreateType();
        }

        public static NIType DefineMutatingBinaryFunction(string name, NIType inputType)
        {
            var functionTypeBuilder = PFTypes.Factory.DefineFunction(name);
            AddInputOutputParameter(
                functionTypeBuilder,
                inputType.CreateMutableReference(),
                "operand1Ref");
            AddInputOutputParameter(
                functionTypeBuilder,
                inputType.CreateImmutableReference(),
                "operand2Ref");
            return functionTypeBuilder.CreateType();
        }

        public static NIType DefineComparisonFunction(string name)
        {
            var functionTypeBuilder = PFTypes.Factory.DefineFunction(name);
            NIType inputType = PFTypes.Int32, outputType = PFTypes.Boolean;
            AddInputOutputParameter(
                functionTypeBuilder,
                inputType.CreateImmutableReference(),
                "operand1Ref");
            AddInputOutputParameter(
                functionTypeBuilder,
                inputType.CreateImmutableReference(),
                "operand2Ref");
            AddOutputParameter(
                functionTypeBuilder,
                outputType,
                "result");
            return functionTypeBuilder.CreateType();
        }

        internal static Signature GetSignatureForNIType(NIType functionSignature)
        {
            List<SignatureTerminal> inputs = new List<SignatureTerminal>(),
                outputs = new List<SignatureTerminal>();
            NIType nonGenericSignature = functionSignature;
            if (nonGenericSignature.IsOpenGeneric())
            {
                NIType[] typeArguments = Enumerable.Range(0, functionSignature.GetGenericParameters().Count)
                    .Select(i => PFTypes.Void)
                    .ToArray();
                nonGenericSignature = nonGenericSignature.ReplaceGenericParameters(typeArguments);
            }
            foreach (var parameterPair in nonGenericSignature.GetParameters().Zip(functionSignature.GetParameters()))
            {
                NIType displayParameter = parameterPair.Key, signatureParameter = parameterPair.Value;
                bool isInput = displayParameter.GetInputParameterPassingRule() != NIParameterPassingRule.NotAllowed,
                    isOutput = displayParameter.GetOutputParameterPassingRule() != NIParameterPassingRule.NotAllowed,
                    isPassthrough = isInput && isOutput;                
                NIType parameterType = displayParameter.GetDataType();
                if (isInput)
                {
                    string name = isOutput ? $"{displayParameter.GetName()}_in" : displayParameter.GetName();
                    inputs.Add(new SignatureTerminal(name, parameterType, signatureParameter.GetDataType(), isPassthrough));
                }
                if (isOutput)
                {
                    string name = isInput ? $"{displayParameter.GetName()}_out" : displayParameter.GetName();
                    outputs.Add(new SignatureTerminal(name, parameterType, signatureParameter.GetDataType(), isPassthrough));
                }
            }

            return new Signature(inputs.ToArray(), outputs.ToArray());
        }
    }

    public struct Signature
    {
        public Signature(SignatureTerminal[] inputs, SignatureTerminal[] outputs)
        {
            Inputs = inputs;
            Outputs = outputs;
        }

        public SignatureTerminal[] Inputs { get; }

        public SignatureTerminal[] Outputs { get; }
    }

    public struct SignatureTerminal
    {
        public SignatureTerminal(string name, NIType displayType, NIType signatureType, bool isPassthrough)
        {
            Name = name;
            DisplayType = displayType;
            SignatureType = signatureType;
            IsPassthrough = isPassthrough;
        }

        public string Name { get; }

        public NIType DisplayType { get; }

        public NIType SignatureType { get; }

        public bool IsPassthrough { get; }
    }
}
