using System.Linq;
using NationalInstruments.DataTypes;

namespace Rebar.Common
{
    internal sealed class FunctionType
    {
        private readonly NIType _signatureType;
        private readonly TypeVariableReference[] _signatureTypeParameters;

        public FunctionType(NIType signatureType, TypeVariableReference[] signatureTypeParameters)
        {
            _signatureType = signatureType;
            _signatureTypeParameters = signatureTypeParameters;
        }

        public NIType FunctionNIType
        {
            get
            {
                if (_signatureType.IsGenericType())
                {
                    NIType genericTypeDefinition = _signatureType.IsGenericTypeDefinition()
                        ? _signatureType
                        : _signatureType.GetGenericTypeDefinition();
                    var builder = genericTypeDefinition.DefineFunctionFromExisting();
                    builder.ReplaceGenericParameters(_signatureTypeParameters.Select(t => t.RenderNIType()).ToArray());
                    return builder.CreateType();
                }
                return _signatureType;
            }
        }
    }
}
