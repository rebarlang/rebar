using System;
using System.ComponentModel.Composition;
using System.Resources;
using NationalInstruments.CommonModel;
using Rebar.Resources;

namespace Rebar.Compiler
{
    /// <summary>
    /// Compile error text provider for message specific to the Rebar MoC.
    /// </summary>
    [Export(typeof(IStringResourceProvider))]
    [ExportMetadata(StringResourceProviderMetadata.ResourceDictionaryName, "Rebar.Resources.LocalizedStrings")]
    public class CompileMessages : IStringResourceProvider
    {
        /// <inheritdoc />
        public ResourceManager Descriptions => LocalizedStrings.ResourceManager;

        /// <inheritdoc />
        public ResourceManager AttributeTitles => LocalizedStrings.ResourceManager;

        /// <inheritdoc />
        public Func<string, object, string> AttributeValuesTranslator => null;
    }
}
