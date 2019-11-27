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
using Rebar.Compiler.TypeDiagram;

namespace Rebar.RebarTarget
{
    internal class TypeDiagramCompileHandler : TargetCompileHandler
    {
        /// <summary>
        /// Creates a new compiler instance
        /// </summary>
        /// <param name="parent">The target compiler for which this is a sub-compiler.</param>
        /// <param name="scheduledActivityManager">The scheduler for any asynchronous tasks that must be executed by the compiler.</param>
        public TypeDiagramCompileHandler(TargetCompiler parent, IScheduledActivityManager scheduledActivityManager)
            : base(parent, scheduledActivityManager)
        {
        }

        #region Overrides

        /// <inheritdoc />
        public override bool CanHandleThis(DfirRootRuntimeType runtimeType) => runtimeType == TypeDiagramMocPlugin.TypeDiagramRuntimeType;

        /// <inheritdoc />
        public override bool IsDefaultBuildSpecEager() => true;

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
                Enumerable.Empty<CompileSignatureParameter>(), // GenerateParameters(targetDfir),
                targetDfir.GetDeclaringType(),
                targetDfir.Reentrancy,
                true,
                true,
                ThreadAffinity.Standard,
                false,
                true,
                ExecutionPriority.Normal,
                CallingConvention.StdCall);
            BuildSpec typeDiagramBuildSpec = specAndQName.BuildSpec;

#if FALSE
            foreach (var dependency in targetDfir.Dependencies.OfType<CompileInvalidationDfirDependency>().ToList())
            {
                var compileSignature = await Compiler.GetCompileSignatureAsync(dependency.SpecAndQName, cancellationToken, progressToken, compileThreadState);
                if (compileSignature != null)
                {
                    targetDfir.AddDependency(
                        targetDfir,
                        new CompileSignatureDependency(dependency.SpecAndQName, compileSignature));
                }
            }
#endif

            IBuiltPackage builtPackage = null;
            // TODO: create TypeDiagramBuiltPackage
            builtPackage = new EmptyBuiltPackage(specAndQName, Compiler.TargetName, Enumerable.Empty<SpecAndQName>());

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

        #endregion
    }
}
