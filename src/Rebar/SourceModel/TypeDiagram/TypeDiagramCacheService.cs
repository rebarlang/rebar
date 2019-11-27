using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NationalInstruments.DataTypes;
using NationalInstruments.Design;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Envoys;

namespace Rebar.SourceModel.TypeDiagram
{
    internal class TypeDiagramCacheService : BasicCacheService, IProvideDataType
    {
        public string DisplayName => AssociatedEnvoy.Name.Last;

        /// <inheritdoc />
        public override NIType Signature => TypeDiagramCache?.DataType ?? NIType.Unset;

        public async Task<NIType> GetDataTypeAsync()
        {
            await InitializeAsync(null);
            return TypeDiagramCache?.DataType ?? NIType.Unset;
        }

        /// <inheritdoc />
        protected override BasicModelCache CreateBasicModelCache() => new TypeDiagramCache();

        private TypeDiagramCache TypeDiagramCache => BasicModelCache as TypeDiagramCache;

        /// <inheritdoc />
        protected override IEnumerable<string> GetExportChangesFromTransaction(TransactionEventArgs e) =>
            TypeDiagramCache?.OnModelEdits(e) ?? Enumerable.Empty<string>();
    }

    /// <summary>
    /// Factory class for <see cref="GTypeDefinitionCacheService"/>
    /// </summary>
    [ProvidedInterface(typeof(IProvideDataType))]
    [ExportEnvoyServiceFactory(typeof(IMethodCallTarget))]
    [ProvidedInterface(typeof(IDependencyTargetExport))]
    [BindsToModelDefinitionType(TypeDiagramDefinition.TypeDiagramDefinitionType)]
    [BindOnTargeted] // TODO: US151337 - This service must be attached on the UI thread (see CAR# 651774 for more info) but there is not a good way to specify this. Use BindOnTargeted to ensure attach occurs on the UI thread. Ideally, this should be BindOnLoaded.
    public class TypeDiagramCacheServiceFactory : EnvoyServiceFactory
    {
        /// <inheritdoc/>
        protected override EnvoyService CreateService()
        {
            return new TypeDiagramCacheService();
        }
    }
}
