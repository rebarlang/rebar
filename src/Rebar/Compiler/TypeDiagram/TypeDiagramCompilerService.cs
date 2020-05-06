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
    /// Factory / registration class for the <see cref="TypeDiagramCompilerService"/> envoy service.
    /// </summary>
    [Preserve(AllMembers = true)]
    [ExportEnvoyServiceFactory(typeof(CompilerService))]
    [BindsToKeyword(TypeDiagramDefinition.TypeDiagramMocIdentifier)]
    [PartMetadata(ExportIdentifier.ExportIdentifierKey, "{62FE669C-9A5D-4DB8-B1FA-44F2ABEF1D23}")]
    [BindOnTargeted]
    public class TypeDiagramCompilerServiceInitialization : CompilerServiceFactory
    {
        /// <summary>
        ///  Called to create the envoy service
        /// </summary>
        /// <returns>the created envoy service</returns>
        protected override CompilerService CreateCompilerService()
        {
            return Host.CreateInstance<TypeDiagramCompilerService>();
        }
    }
}
