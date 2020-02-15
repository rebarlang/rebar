using System.Collections.Generic;
using System.Threading.Tasks;
using NationalInstruments.Core;
using NationalInstruments.ExternalCode.SourceModel;
using NationalInstruments.MocCommon.SourceModel;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Envoys;
using Rebar.SourceModel;

namespace Rebar.Design
{
    /// <summary>
    /// Envoy Service Factory that produces instances of a merge script data service for derived classed of <see cref="ExternalLibraryEntryPoint"/>
    /// </summary>
    [ExportEnvoyServiceFactory(typeof(IProvideMergeScriptData))]
    [BindsToKeyword(ExternalLibraryBindingKeywords.EntryPointPaletteSupport)]
    internal class ExternalLibraryEntryPointMergeScriptDataServiceFactory : EnvoyServiceFactory
    {
        /// <inheritdoc/>
        protected override EnvoyService CreateService()
        {
            return new ExternalLibraryEntryPointMergeScriptDataService();
        }

        private class ExternalLibraryEntryPointMergeScriptDataService : EnvoyService, IProvideMergeScriptData
        {
            /// <inheritdoc/>
            public IEnumerable<MergeScriptData> MergeScriptData
            {
                get
                {
                    if (AssociatedEnvoy != null)
                    {
                        MergeScriptBuilder builder = new MergeScriptBuilder(Host);
                        var createInfo = new ElementCreateInfo(Host, null, null, null, null);
                        var methodCall = MocCommonMethodCall.Create(createInfo);
                        methodCall.Target = AssociatedEnvoy.MakeRelativeDependencyName();
                        builder.AddElement(new MergeElementInfo(methodCall));
                        var mergeText = builder.ToString();
                        yield return new MergeScriptData(mergeText, Function.FunctionClipboardDataFormat, FunctionDiagramPaletteLoader.DiagramPaletteIdentifier);
                    }
                }
            }

            /// <inheritdoc/>
            public Task<IEnumerable<MergeScriptData>> GetFilteredMergeScriptsAsync(IMergeScriptFilter filter)
            {
                return ProvideMergeScriptDataHelpers.GetFilteredMergeScriptsAsync(MergeScriptData, filter);
            }
        }
    }
}
