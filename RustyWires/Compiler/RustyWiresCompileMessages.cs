using System;
using System.ComponentModel.Composition;
using System.Resources;
using NationalInstruments.CommonModel;
using RustyWires.Resources;

namespace RustyWires.Compiler
{
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
