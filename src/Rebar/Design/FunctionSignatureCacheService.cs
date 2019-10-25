using System;
using NationalInstruments.MocCommon.SourceModel;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Envoys;
using Rebar.SourceModel;

namespace Rebar.Design
{
    /// <summary>
    /// Factory class for <see cref="FunctionSignatureCacheService"/>
    /// </summary>
    [ExportEnvoyServiceFactory(typeof(IMethodCallTarget))]
    [ProvidedInterface(typeof(IDependencyTargetExport))]
    [BindsToModelDefinitionType(Function.FunctionDefinitionType)]
    [BindOnLoaded]
    public class FunctionSignatureCacheServiceFactory : EnvoyServiceFactory
    {
        /// <inheritdoc/>
        protected override EnvoyService CreateService()
        {
            return Host.CreateInstance<FunctionSignatureCacheService>();
        }
    }

    /// <summary>
    /// Envoy service that is attached to a Rebar function source file reference.
    /// This service provides information about the function either through cache or definition.
    /// </summary>
    public sealed class FunctionSignatureCacheService : MocCommonFunctionSignatureCacheService
    {
        protected override BasicModelCache CreateBasicModelCache()
        {
            return FunctionDefinitionSignatureCache.FunctionDefinitionSignatureCacheFactory(new ElementCreateInfo(Host));
        }

        public override bool TryGetDefaultValue(string parameterName, out object defaultValue)
        {
            defaultValue = null;
            return false;
        }

        public override bool TryGetDefaultValueText(string parameterName, out string defaultValueText)
        {
            defaultValueText = "none";
            return true;
        }

        public override bool TryGetCurrentValue(string parameterName, out object currentValue)
        {
            currentValue = null;
            return false;
        }
    }
}
