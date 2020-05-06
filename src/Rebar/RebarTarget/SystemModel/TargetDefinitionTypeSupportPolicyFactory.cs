using NationalInstruments.Core;
using NationalInstruments.SourceModel.Envoys;

namespace Rebar.RebarTarget.SystemModel
{
    [ExportEnvoyServiceFactory(typeof(ITargetDefinitionTypeSupportPolicy))]
    [BindsToKeyword(TargetDefinition.TargetDefinitionString)]
    internal sealed class TargetDefinitionTypeSupportPolicyFactory : EnvoyServiceFactory
    {
        protected override EnvoyService CreateService() => new TargetDefinitionTypeSupportPolicy();
    }
}
