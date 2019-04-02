using System.ComponentModel.Composition;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Envoys;

namespace Rebar.RebarTarget.SystemModel
{
    /// <summary>
    /// Process service backed envoy service builder for Rebar.
    /// </summary>
    [PartCreationPolicy(CreationPolicy.Shared)]
    [ProcessServiceBuilder(SystemModelNamespaceSchema.ParsableNamespaceName)]
    internal class ProcessServiceBuilder : IProcessServiceBuilder
    {
        /// <inheritdoc/>
        public void Build(IProcessServiceBuilderHelper helper)
        {
            helper.RegisterService<ICreateItemsInScopeRules, TargetKind>(new CreateItemInScopeRulesService());
        }
    }
}
