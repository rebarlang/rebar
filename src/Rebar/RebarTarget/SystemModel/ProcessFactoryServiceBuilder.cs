using System.Collections.Generic;
using System.ComponentModel.Composition;
using NationalInstruments;
using NationalInstruments.SourceModel;
using NationalInstruments.SystemModel;

namespace Rebar.RebarTarget.SystemModel
{
    /// <summary>
    /// The <see cref="IProcessServiceBuilder"/> instance for Rebar targets
    /// </summary>
    [PartCreationPolicy(CreationPolicy.Shared)]
    [ProcessServiceBuilder(SystemModelNamespaceSchema.ParsableNamespaceName)]
    internal class ProcessFactoryServiceBuilder : IProcessServiceBuilder
    {
        private static IEnumerable<KindInfo> GetTargetAdoptableKinds(IProcessServiceBuilderHelper helper)
        {
            // This allows all software kinds in the target, then RebarCreateItemInScopeRulesService restricts it further
            return helper.GetKind<SoftwareKind>().ToEnumerable();
        }

        /// <inheritdoc />
        public void Build(IProcessServiceBuilderHelper helper)
        {
            helper.RegisterService<IProcessFactoryService, TargetKind>(
                new ProcessFactoryService(new TargetFactorySoftwareFilter(), GetTargetAdoptableKinds(helper)));
        }
    }
}
