using System.Threading.Tasks;
using NationalInstruments.Core;
using NationalInstruments.Linking;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Envoys;

namespace Rebar.SourceModel
{
    /// <summary>
    /// <see cref="Dependency"/> subclass that targets <see cref="TypeDiagramDefinition"/>s.
    /// </summary>
    public class TypeDiagramDependency : Dependency, IDependencyTargetExportChanged
    {
        public TypeDiagramDependency(IQualifiedSource owningElement, QualifiedName targetName)
            : base(owningElement, targetName)
        {
        }

        public Task OnExportsChangedAsync(Envoy envoy, ExportsChangedData data)
        {
            // TODO
            return AsyncHelpers.CompletedTask;
        }
    }
}
