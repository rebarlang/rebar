using System.Collections.Generic;
using System.Linq;
using NationalInstruments;
using NationalInstruments.DataTypes;

namespace Rebar.Common
{
    public static class Signatures
    {
        static Signatures()
        {
            var functionTypeBuilder = PFTypes.Factory.DefineFunction("ImmutPass");
            var genericTypeParameters = functionTypeBuilder.MakeGenericParameters(
                "TData");
            var tDataParameter = genericTypeParameters.ElementAt(0).CreateType();
            var immutableReferenceType = tDataParameter.CreateImmutableReference();
            functionTypeBuilder.DefineParameter(
                immutableReferenceType,
                "valueRef",
                NIParameterPassingRule.Required,
                NIParameterPassingRule.Recommended);
            ImmutablePassthroughType = functionTypeBuilder.CreateType();

            functionTypeBuilder = PFTypes.Factory.DefineFunction("MutPass");
            genericTypeParameters = functionTypeBuilder.MakeGenericParameters(
                "TData");
            tDataParameter = genericTypeParameters.ElementAt(0).CreateType();
            var mutableReferenceType = tDataParameter.CreateMutableReference();
            functionTypeBuilder.DefineParameter(
                mutableReferenceType,
                "valueRef",
                NIParameterPassingRule.Required,
                NIParameterPassingRule.Recommended);
            MutablePassthroughType = functionTypeBuilder.CreateType();

            functionTypeBuilder = PFTypes.Factory.DefineFunction("CreateCopy");
            // TODO: constrain TData to be Copy
            genericTypeParameters = functionTypeBuilder.MakeGenericParameters(
                "TData");
            tDataParameter = genericTypeParameters.ElementAt(0).CreateType();
            immutableReferenceType = tDataParameter.CreateImmutableReference();
            functionTypeBuilder.DefineParameter(
                immutableReferenceType,
                "valueRef",
                NIParameterPassingRule.Required,
                NIParameterPassingRule.Recommended);
            functionTypeBuilder.DefineParameter(
                tDataParameter,
                "copy",
                NIParameterPassingRule.NotAllowed,
                NIParameterPassingRule.Recommended);
            CreateCopyType = functionTypeBuilder.CreateType();

            functionTypeBuilder = PFTypes.Factory.DefineFunction("Output");
            // TODO: allow other types later
            immutableReferenceType = PFTypes.Int32.CreateImmutableReference();
            functionTypeBuilder.DefineParameter(
                immutableReferenceType,
                "valueRef",
                NIParameterPassingRule.Required,
                NIParameterPassingRule.Recommended);
            OutputType = functionTypeBuilder.CreateType();

            functionTypeBuilder = PFTypes.Factory.DefineFunction("Range");
            functionTypeBuilder.DefineParameter(
                PFTypes.Int32,
                "lowValue",
                NIParameterPassingRule.Required,
                NIParameterPassingRule.NotAllowed);
            functionTypeBuilder.DefineParameter(
                PFTypes.Int32,
                "highValue",
                NIParameterPassingRule.Required,
                NIParameterPassingRule.NotAllowed);
            functionTypeBuilder.DefineParameter(
                PFTypes.Int32.CreateIterator(),
                "range",
                NIParameterPassingRule.NotAllowed,
                NIParameterPassingRule.Recommended);
            RangeType = functionTypeBuilder.CreateType();

            functionTypeBuilder = PFTypes.Factory.DefineFunction("VectorCreate");
            // TODO
#if FALSE
            genericTypeParameters = functionTypeBuilder.MakeGenericParameters(
                "TData");
            tDataParameter = genericTypeParameters.ElementAt(0).CreateType();
#endif
            functionTypeBuilder.DefineParameter(
                PFTypes.Int32.CreateVector(),
                "valueRef",
                NIParameterPassingRule.NotAllowed,
                NIParameterPassingRule.Recommended);
            VectorCreateType = functionTypeBuilder.CreateType();

            functionTypeBuilder = PFTypes.Factory.DefineFunction("VectorInsert");
            genericTypeParameters = functionTypeBuilder.MakeGenericParameters(
                "TData");
            tDataParameter = genericTypeParameters.ElementAt(0).CreateType();
            functionTypeBuilder.DefineParameter(
                tDataParameter.CreateVector().CreateMutableReference(),
                "vectorRef",
                NIParameterPassingRule.Required,
                NIParameterPassingRule.Recommended);
            functionTypeBuilder.DefineParameter(
                PFTypes.Int32.CreateImmutableReference(),
                "indexRef",
                NIParameterPassingRule.Required,
                NIParameterPassingRule.Recommended);
            functionTypeBuilder.DefineParameter(
                tDataParameter,
                "element",
                NIParameterPassingRule.Required,
                NIParameterPassingRule.NotAllowed);
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
            var immutableReferenceType = inputType.CreateImmutableReference();
            functionTypeBuilder.DefineParameter(
                immutableReferenceType,
                "operandRef",
                NIParameterPassingRule.Required,
                NIParameterPassingRule.Recommended);
            functionTypeBuilder.DefineParameter(
                outputType,
                "result",
                NIParameterPassingRule.NotAllowed,
                NIParameterPassingRule.Recommended);
            return functionTypeBuilder.CreateType();
        }

        public static NIType DefinePureBinaryFunction(string name, NIType inputType, NIType outputType)
        {
            var functionTypeBuilder = PFTypes.Factory.DefineFunction(name);
            var immutableReferenceType = inputType.CreateImmutableReference();
            functionTypeBuilder.DefineParameter(
                immutableReferenceType,
                "operand1Ref",
                NIParameterPassingRule.Required,
                NIParameterPassingRule.Recommended);
            functionTypeBuilder.DefineParameter(
                immutableReferenceType,
                "operand2Ref",
                NIParameterPassingRule.Required,
                NIParameterPassingRule.Recommended);
            functionTypeBuilder.DefineParameter(
                outputType,
                "result",
                NIParameterPassingRule.NotAllowed,
                NIParameterPassingRule.Recommended);
            return functionTypeBuilder.CreateType();
        }

        public static NIType DefineMutatingUnaryFunction(string name, NIType inputType)
        {
            var functionTypeBuilder = PFTypes.Factory.DefineFunction(name);
            functionTypeBuilder.DefineParameter(
                inputType.CreateMutableReference(),
                "operandRef",
                NIParameterPassingRule.Required,
                NIParameterPassingRule.Recommended);
            return functionTypeBuilder.CreateType();
        }

        public static NIType DefineMutatingBinaryFunction(string name, NIType inputType)
        {
            var functionTypeBuilder = PFTypes.Factory.DefineFunction(name);
            functionTypeBuilder.DefineParameter(
                inputType.CreateMutableReference(),
                "operand1Ref",
                NIParameterPassingRule.Required,
                NIParameterPassingRule.Recommended);
            functionTypeBuilder.DefineParameter(
                inputType.CreateImmutableReference(),
                "operand2Ref",
                NIParameterPassingRule.Required,
                NIParameterPassingRule.Recommended);
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
