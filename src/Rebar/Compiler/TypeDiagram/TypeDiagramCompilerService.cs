using System.ComponentModel.Composition;
using Foundation;
using NationalInstruments.Compiler;
using NationalInstruments.Composition;
using NationalInstruments.Core;
using NationalInstruments.SourceModel.Envoys;
using Rebar.SourceModel.TypeDiagram;

namespace Rebar.Compiler.TypeDiagram
{
    internal class TypeDiagramCompilerService : CompilerService, IPartImportsSatisfiedNotification
    {
        private TypeDiagramMocPlugin _mocPlugin;

        /// <inheritdoc />
        protected override MocPlugin MocPlugin => _mocPlugin;

        public void OnImportsSatisfied()
        {
            _mocPlugin = new TypeDiagramMocPlugin(Host, this);
        }
    }

    /// <summary>
    /// Factory / registration class for the <see cref="FunctionCompilerService"/> envoy service.
    /// </summary>
    [Preserve(AllMembers = true)]
    [ExportEnvoyServiceFactory(typeof(CompilerService))]
    [BindsToKeyword(TypeDiagramDefinition.TypeDiagramMocIdentifier)]
    [PartMetadata(ExportIdentifier.ExportIdentifierKey, "{62FE669C-9A5D-4DB8-B1FA-44F2ABEF1D23}")]
    [BindOnTargeted]
    public class TypeDiagramCompilerServiceInitialization : EnvoyServiceFactory
    {
        /// <summary>
        ///  Called to create the envoy service
        /// </summary>
        /// <returns>the created envoy service</returns>
        protected override EnvoyService CreateService()
        {
            return Host.CreateInstance<TypeDiagramCompilerService>();
        }
    }
}
