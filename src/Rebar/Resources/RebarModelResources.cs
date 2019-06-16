using System.ComponentModel.Composition;
using System.Resources;
using NationalInstruments.Core;

namespace Rebar.Resources
{
    [Export(typeof(IAssemblyResources))]
    [ExportMetadata("AssemblyName", "Rebar.Plugin")]
    [ExportMetadata("ProvidedResourcesPrefix", "")]
    public class RebarModelResources : IAssemblyResources
    {
        private static readonly ResourceManager _contextHelpResourceManager = new ResourceManager(
            "Rebar.Resources.ContextHelp.resources",
            typeof(ContextHelp_resources).Assembly);

        /// <inheritdoc />
        public ResourceManager GetResources() => _contextHelpResourceManager;
    }
}
