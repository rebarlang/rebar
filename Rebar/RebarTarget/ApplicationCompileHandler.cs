using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NationalInstruments.Compiler;
using NationalInstruments.Composition;
using NationalInstruments.Core;
using NationalInstruments.Dfir;
using NationalInstruments.ExecutionFramework;
using NationalInstruments.MocCommon.Components.Compiler;

namespace Rebar.RebarTarget
{
    public class ApplicationCompileHandler : ComponentCompileHandler
    {
        /// <summary>
        /// Standard constructor.
        /// </summary>
        /// <param name="parent">Target compiler that owns this compile handler.</param>
        /// <param name="scheduledActivityManager">The <see cref="IScheduledActivityManager"/> associated with this compile handler.</param>
        /// <param name="host">The <see cref="ICompositionHost"/> associated with this compile handler.</param>
        /// <param name="owningComponentInformationRetriever"><see cref="OwningComponentInformationRetriever"/> instance that will be used for fetching <see cref="OwningComponentInformation"/>s during Member-building.</param>
        public ApplicationCompileHandler(
            DelegatingTargetCompiler parent,
            IScheduledActivityManager scheduledActivityManager,
            ICompositionHost host,
            OwningComponentInformationRetriever owningComponentInformationRetriever)
            : base(parent, scheduledActivityManager, host, owningComponentInformationRetriever)
        {
        }

        /// <inheritdoc />
        public override bool CanHandleThis(DfirRootRuntimeType runtimeType)
        {
            return runtimeType == ApplicationComponentMocPluginPlugin.ApplicationRuntimeType;
        }

        /// <inheritdoc />
        protected override IEnumerable<IFileBuilder> CreateFileBuilders(DfirRoot targetDfir, ComponentBuildResult componentBuildResult)
        {
            return Enumerable.Empty<IFileBuilder>();
        }

        /// <inheritdoc />
        protected override IBuiltPackage CreateExecutionBuiltPackage(SpecAndQName specAndQName, string absoluteDirectoryForComponent,
            IEnumerable<IFileBuilder> builders, ComponentBuildResult componentBuildResult)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        protected override IMemberBuilderBehavior CreateMemberBuilderBehavior()
        {
            return new ApplicationMemberBuilderBehavior(Compiler);
        }

        /// <summary>
        /// Rebar application-specific implementation of <see cref="IMemberBuilderBehavior"/>.
        /// </summary>
        internal class ApplicationMemberBuilderBehavior : IMemberBuilderBehavior
        {
            /// <summary>
            /// Create an instance of <see cref="ApplicationMemberBuilderBehavior"/>.
            /// </summary>
            /// <param name="compiler"><see cref="DelegatingTargetCompiler"/> used by <see cref="GetCompileInformation"/>.</param>
            public ApplicationMemberBuilderBehavior(DelegatingTargetCompiler compiler)
            {
                Compiler = compiler;
            }

            /// <summary>
            /// <see cref="DelegatingTargetCompiler"/> used by <see cref="GetCompileInformation"/>.
            /// </summary>
            private DelegatingTargetCompiler Compiler { get; }

            #region IMemberBuilderBehavior Implementation

            /// <inheritdoc />
            public Task<CompileInformation> GetCompileInformation(SpecAndQName itemSpecAndQName, CompileCancellationToken cancellationToken,
                ProgressToken progressToken, CompileThreadState compileThreadState)
            {
                return Compiler.CompileAsTopLevel(itemSpecAndQName, cancellationToken, progressToken, compileThreadState);
            }

            #endregion IMemberBuilderBehavior Implementation
        }
    }
}
