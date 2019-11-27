using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NationalInstruments.Compiler;
using NationalInstruments.Core;
using NationalInstruments.Dfir;
using NationalInstruments.ExecutionFramework;
using NationalInstruments.Linking;
using NationalInstruments.NativeTarget.Compiler;
using NationalInstruments.SourceModel.Envoys;

namespace Rebar.Compiler
{
    internal class DefaultNativeTargetCompileHandler : TargetCompileHandler
    {
        /// <summary>
        /// Creates a new compiler instance
        /// </summary>
        /// <param name="parent">The target compiler for which this is a sub-compiler.</param>
        /// <param name="scheduledActivityManager">The scheduler for any asynchronous tasks that must be executed by the compiler.</param>
        public DefaultNativeTargetCompileHandler(DelegatingTargetCompiler parent, IScheduledActivityManager scheduledActivityManager)
            : base(parent, scheduledActivityManager)
        {
        }

        /// <inheritdoc />
        public override bool CanHandleThis(DfirRootRuntimeType runtimeType)
        {
            return runtimeType == FunctionMocPlugin.FunctionRuntimeType;
        }

        /// <inheritdoc />
        public override bool IsDefaultBuildSpecEager() => false;

        /// <inheritdoc />
        public override async Task<Tuple<CompileCacheEntry, CompileSignature>> BackEndCompileAsyncCore(
            SpecAndQName specAndQName,
            DfirRoot targetDfir,
            CompileCancellationToken cancellationToken,
            ProgressToken progressToken,
            CompileThreadState compileThreadState)
        {
            CompileSignature topSignature = new CompileSignature(
                targetDfir.Name,
                Enumerable.Empty<CompileSignatureParameter>(),
                targetDfir.GetDeclaringType(),
                targetDfir.Reentrancy,
                true,
                true,
                ThreadAffinity.Standard,
                false,
                true,
                ExecutionPriority.Normal,
                CallingConvention.StdCall);

            var builtPackage = new EmptyBuiltPackage(specAndQName, Compiler.TargetName, Enumerable.Empty<SpecAndQName>());

            BuiltPackageToken token = Compiler.AddToBuiltPackagesCache(builtPackage);
            CompileCacheEntry entry = await Compiler.CreateStandardCompileCacheEntryFromDfirRootAsync(
                CompileState.Complete,
                targetDfir,
                new Dictionary<ExtendedQualifiedName, CompileSignature>(),
                token,
                cancellationToken,
                progressToken,
                compileThreadState,
                false);

            return new Tuple<CompileCacheEntry, CompileSignature>(entry, topSignature);
        }

        /// <inheritdoc/>
        public override CompileSignature PredictCompileSignatureCore(DfirRoot targetDfir, CompileSignature previousSignature) => null;
    }

    /// <summary>
    /// A factory for creating <see cref="DefaultNativeTargetCompileHandler"/>s.
    /// </summary>
    internal sealed class DefaultNativeTargetCompileHandlerFactory : EnvoyService, ITargetCompileHandlerFactory
    {
        /// <inheritdoc />
        public TargetCompileHandler Create(DelegatingTargetCompiler parent, IScheduledActivityManager scheduledActivityManager)
            => new DefaultNativeTargetCompileHandler(parent, scheduledActivityManager);
    }

    /// <summary>
    /// Envoy service factory for <see cref="FunctionCompilerHandlerFactory"/>. Binds to Rebar targets as an envoy service.
    /// </summary>
    [ExportEnvoyServiceFactory(typeof(ITargetCompileHandlerFactory))]
    [BindsToKeyword(NativeTargetCompilerServices.TargetModelName)]
    public class FunctionCompileHandlerFactoryFactory : EnvoyServiceFactory
    {
        /// <inheritdoc />
        protected override EnvoyService CreateService()
        {
            return new DefaultNativeTargetCompileHandlerFactory();
        }
    }
}
