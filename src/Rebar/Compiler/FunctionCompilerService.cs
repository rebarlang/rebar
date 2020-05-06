using System.ComponentModel.Composition;
using Foundation;
using NationalInstruments.Compiler;
using NationalInstruments.Composition;
using NationalInstruments.Core;
using NationalInstruments.SourceModel.Envoys;
using Rebar.SourceModel;

namespace Rebar.Compiler
{
    internal class FunctionCompilerService : CompilerService, IPartImportsSatisfiedNotification
    {
        private FunctionMocPlugin _mocPlugin;

        protected override MocPlugin MocPlugin => _mocPlugin;

        public void OnImportsSatisfied()
        {
            _mocPlugin = new FunctionMocPlugin(Host, this);
        }
    }

    /// <summary>
    /// Factory / registration class for the <see cref="FunctionCompilerService"/> envoy service.
    /// </summary>
    [Preserve(AllMembers = true)]
    [ExportEnvoyServiceFactory(typeof(CompilerService))]
    [BindsToKeyword(Function.FunctionMocIdentifier)]
    [PartMetadata(ExportIdentifier.ExportIdentifierKey, "{1253CAD1-5874-4BB6-8090-7C3841BB5E21}")]
    [BindOnTargeted]
    public class FunctionCompilerServiceInitialization : CompilerServiceFactory
    {
        /// <summary>
        ///  Called to create the envoy service
        /// </summary>
        /// <returns>the created envoy service</returns>
        protected override CompilerService CreateCompilerService()
        {
            return Host.CreateInstance<FunctionCompilerService>();
        }
    }
}
