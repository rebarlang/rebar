using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Resources;
using NationalInstruments.CommonModel;
using NationalInstruments.Compiler;
using NationalInstruments.SourceModel;
using RustyWires.Resources;

namespace RustyWires
{
#if FALSE
    [Export(typeof(IAssemblyResources))]
    [ExportMetadata("AssemblyName", "RustyWires")]
    [ExportMetadata("ProvidedResourcesPrefix", "")]
    public class RustyWiresModelResources : IAssemblyResources
    {
        private static readonly ResourceManager _resourceManager = new ResourceManager("RustyWires.");
        public ResourceManager GetResources()
        {
            throw new NotImplementedException();
        }
    }
#endif

    /// <summary>
    /// Compile error text provider for message specific to the RustyWires MoC.
    /// </summary>
    [Export(typeof(IStringResourceProvider))]
    [ExportMetadata(StringResourceProviderMetadata.ResourceDictionaryName, "RustyWires.Resources.LocalizedStrings")]
    public class RustyWiresCompileMessages : IStringResourceProvider
    {
        /// <inheritdoc />
        public ResourceManager Descriptions => LocalizedStrings.ResourceManager;

        /// <inheritdoc />
        public ResourceManager AttributeTitles => LocalizedStrings.ResourceManager;

        /// <inheritdoc />
        public Func<string, object, string> AttributeValuesTranslator => null;
    }
}
