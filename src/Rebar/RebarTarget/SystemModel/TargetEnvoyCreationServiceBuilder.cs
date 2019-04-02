using System.ComponentModel.Composition;
using NationalInstruments.SourceModel;
using NationalInstruments.SystemModel.Envoys;

namespace Rebar.RebarTarget.SystemModel
{
    /// <summary>
    /// The <see cref="IProcessServiceBuilder"/> for the Rebar target. 
    /// This associates the Rebar system diagram target with the RebarTarget (compiler, palette, etc.).
    /// </summary>
    [PartCreationPolicy(CreationPolicy.Shared)]
    [ProcessServiceBuilder(SystemModelNamespaceSchema.ParsableNamespaceName)]
    internal class TargetEnvoyCreationServiceBuilder : IProcessServiceBuilder
    {
        /// <summary>
        /// Keyword for binding to a Rebar target.
        /// </summary>
        public const string TargetKeyword = "RebarTarget";

        /// <inheritdoc/>
        public void Build(IProcessServiceBuilderHelper helper)
        {
            helper.RegisterEnvoyBindingKeywordService<TargetKind>(new EnvoyBindingKeywordService(
                TargetDefinition.TargetDefinitionString,
                TargetKeyword));
        }
    }
}
