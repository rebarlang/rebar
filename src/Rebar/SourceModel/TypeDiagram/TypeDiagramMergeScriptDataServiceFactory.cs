using System.Collections.Generic;
using System.Threading.Tasks;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Envoys;
using Rebar.Design;

namespace Rebar.SourceModel.TypeDiagram
{
    /// <summary>
    /// Service factory for <see cref="TypeDiagramDefinition"/> <see cref="Envoy"/>s that creates an 
    /// <see cref="IProvideMergeScriptData"/> service.
    /// </summary>
    [ExportEnvoyServiceFactory(typeof(IProvideMergeScriptData))]
    [BindsToModelDefinitionType(TypeDiagramDefinition.TypeDiagramDefinitionType)]
    internal class TypeDiagramMergeScriptDataServiceFactory : EnvoyServiceFactory
    {
        /// <inheritdoc />
        protected override EnvoyService CreateService() => new TypeDiagramMergeScriptDataService();

        /// <summary>
        /// Envoy service that provides TypeDiagram merge script data.
        /// </summary>
        private class TypeDiagramMergeScriptDataService : EnvoyService, IProvideMergeScriptData
        {
            #region IProvideMergeScriptData

            /// <inheritdoc/>
            public virtual IEnumerable<MergeScriptData> MergeScriptData
            {
                get
                {
                    if (AssociatedEnvoy != null)
                    {
                        var preferredEnvoy = AssociatedEnvoy.TryGetPreferredEnvoy() ?? AssociatedEnvoy;

                        MergeScriptBuilder functionDiagramMergeScriptBuilder = new MergeScriptBuilder(Host);
                        functionDiagramMergeScriptBuilder.AddElement(new MergeElementInfo(CreateConstructor(preferredEnvoy)));
                        var mergeText = functionDiagramMergeScriptBuilder.ToString();

                        yield return new MergeScriptData(
                            mergeText,
                            Function.FunctionClipboardDataFormat,
                            FunctionDiagramPaletteLoader.DiagramPaletteIdentifier);

                        // TODO: create a MergeScriptData for the TypeDiagram format that drops a node that
                        // allows referencing a .td type on the TypeDiagram
                    }
                }
            }

            /// <inheritdoc/>
            public virtual Task<IEnumerable<MergeScriptData>> GetFilteredMergeScriptsAsync(IMergeScriptFilter filter)
            {
                return ProvideMergeScriptDataHelpers.GetFilteredMergeScriptsAsync(MergeScriptData, filter);
            }

            private Constructor CreateConstructor(Envoy typeDiagramEnvoy)
            {
                var constructor = Constructor.CreateConstructor(new ElementCreateInfo(Host));
                constructor.TypeName = typeDiagramEnvoy.MakeRelativeDependencyName();
                return constructor;
            }

            #endregion
        }
    }
}
