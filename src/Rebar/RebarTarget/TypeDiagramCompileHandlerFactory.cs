using NationalInstruments.Compiler;
using NationalInstruments.Core;
using NationalInstruments.SourceModel.Envoys;

namespace Rebar.RebarTarget
{
    /// <summary>
    /// A factory for creating <see cref="FunctionCompileHandler"/>s.
    /// </summary>
    internal sealed class TypeDiagramCompileHandlerFactory : EnvoyService, ITargetCompileHandlerFactory
    {
        /// <inheritdoc />
        public TargetCompileHandler Create(DelegatingTargetCompiler parent, IScheduledActivityManager scheduledActivityManager)
        {
            return new TypeDiagramCompileHandler((TargetCompiler)parent, scheduledActivityManager);
        }
    }
}
