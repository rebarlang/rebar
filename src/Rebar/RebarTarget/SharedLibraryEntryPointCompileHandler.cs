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
using NationalInstruments.SourceModel.Envoys;

namespace Rebar.RebarTarget
{
    internal class SharedLibraryEntryPointCompileHandler : TargetCompileHandler
    {
        /// <summary>
        /// Creates a new compiler instance
        /// </summary>
        /// <param name="parent">The target compiler for which this is a sub-compiler.</param>
        /// <param name="scheduledActivityManager">The scheduler for any asynchronous tasks that must be executed by the compiler.</param>
        public SharedLibraryEntryPointCompileHandler(TargetCompiler parent, IScheduledActivityManager scheduledActivityManager)
            : base(parent, scheduledActivityManager)
        {
        }

        #region Overrides

        /// <inheritdoc />
        public override bool CanHandleThis(DfirRootRuntimeType runtimeType)
        {
            // Unfortunately, this runtime type is also used by VI and other kinds of DfirRoot in addition to SLI entry points.
            // In BackEndCompileAsyncCore, we'll try to determine whether the DfirRoot actually came from an SLI entry point
            // and return an error built package otherwise.
            return runtimeType == DfirRootRuntimeType.FunctionType;
        }

        /// <inheritdoc />
        public override bool IsDefaultBuildSpecEager()
        {
            return true;
        }

        /// <inheritdoc />
        public override async Task<Tuple<CompileCacheEntry, CompileSignature>> BackEndCompileAsyncCore(
            SpecAndQName specAndQName,
            DfirRoot targetDfir,
            CompileCancellationToken cancellationToken,
            ProgressToken progressToken,
            CompileThreadState compileThreadState)
        {
#if FALSE
            var compileSignatureParameters = new List<CompileSignatureParameter>();
            foreach (DataItem dataItem in targetDfir.DataItems.OrderBy(dataItem => dataItem.ConnectorPaneIndex))
            {
                var compileSignatureParameter = new CompileSignatureParameter(
                    dataItem.Name,
                    dataItem.Name,
                    dataItem.DataType,
                    dataItem.ConnectorPaneInputPassingRule,
                    dataItem.ConnectorPaneOutputPassingRule,
                    dataItem.ConnectorPaneIndex);
                compileSignatureParameters.Add(compileSignatureParameter);
            }

            var compileSignatures = new Dictionary<ExtendedQualifiedName, CompileSignature>();
            var dependencyIdentities = new HashSet<SpecAndQName>();
            foreach (var dependency in targetDfir.Dependencies.OfType<CompileInvalidationDfirDependency>().ToList())
            {
                dependencyIdentities.Add(dependency.SpecAndQName);
                var compileSignature = await Compiler.GetCompileSignatureAsync(dependency.SpecAndQName, cancellationToken, progressToken, compileThreadState);
                if (compileSignature != null)
                {
                    targetDfir.AddDependency(
                        targetDfir,
                        new CompileSignatureDependency(dependency.SpecAndQName, compileSignature));
                    compileSignatures[dependency.SpecAndQName.QualifiedName] = compileSignature;
                }
            }

            var calleesIsYielding = new Dictionary<ExtendedQualifiedName, bool>();
            foreach (var methodCallNode in targetDfir.GetAllNodesIncludingSelf().OfType<MethodCallNode>())
            {
                CompileSignature calleeSignature = compileSignatures[methodCallNode.TargetName];
                calleesIsYielding[methodCallNode.TargetName] = calleeSignature.IsYielding;
            }

            LLVM.FunctionCompileResult functionCompileResult = CompileFunctionForLLVM(targetDfir, cancellationToken, calleesIsYielding);
            var builtPackage = new LLVM.FunctionBuiltPackage(
                specAndQName,
                Compiler.TargetName,
                dependencyIdentities.ToArray(),
                functionCompileResult.Module);
#endif
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

            CompileSignature topSignature = new CompileSignature(
                functionName: targetDfir.Name,
                parameters: Enumerable.Empty<CompileSignatureParameter>(), // GenerateParameters(targetDfir),
                declaringType: targetDfir.GetDeclaringType(),
                reentrancy: targetDfir.Reentrancy,
                isYielding: false,
                isFunctional: true,
                threadAffinity: ThreadAffinity.Standard,
                shouldAlwaysInline: false,
                mayWantToInline: true,
                priority: ExecutionPriority.Normal,
                callingConvention: CallingConvention.StdCall);

            return new Tuple<CompileCacheEntry, CompileSignature>(entry, topSignature);
        }

        #endregion

        /// <inheritdoc/>
        public override CompileSignature PredictCompileSignatureCore(DfirRoot targetDfir, CompileSignature previousSignature)
        {
            return null;
        }
    }

    /// <summary>
    /// A factory for creating <see cref="SharedLibraryEntryPointCompileHandler"/>s.
    /// </summary>
    internal sealed class SharedLibraryEntryPointCompileHandlerFactory : EnvoyService, ITargetCompileHandlerFactory
    {
        /// <inheritdoc />
        public TargetCompileHandler Create(DelegatingTargetCompiler parent, IScheduledActivityManager scheduledActivityManager)
        {
            return new SharedLibraryEntryPointCompileHandler((TargetCompiler)parent, scheduledActivityManager);
        }
    }

    /// <summary>
    /// Envoy service factory for <see cref="SharedLibraryEntryPointCompilerHandlerFactory"/>. Binds to Rebar targets as an envoy service.
    /// </summary>
    [ExportEnvoyServiceFactory(typeof(ITargetCompileHandlerFactory))]
    [BindsToKeyword(TargetDefinition.TargetDefinitionString)]
    public class SharedLibraryEntryPointCompileHandlerFactoryFactory : EnvoyServiceFactory
    {
        /// <inheritdoc />
        protected override EnvoyService CreateService()
        {
            return new SharedLibraryEntryPointCompileHandlerFactory();
        }
    }
}
