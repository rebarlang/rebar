using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using LLVMSharp;
using NationalInstruments.Compiler;
using NationalInstruments.Core;
using NationalInstruments.Dfir;
using NationalInstruments.ExecutionFramework;
using NationalInstruments.Linking;
using Rebar.Compiler;
using Rebar.RebarTarget.Execution;
using Rebar.Common;

namespace Rebar.RebarTarget
{
    /// <summary>
    /// A compiler for building Rebar functions on the Rebar target. Not a standalone compiler, but rather a sub-compiler for 
    /// the <see cref="TargetCompiler"/> to delegate function compilation work to.
    /// </summary>
    public class FunctionCompileHandler : TargetCompileHandler
    {
        /// <summary>
        /// Creates a new compiler instance
        /// </summary>
        /// <param name="parent">The target compiler for which this is a sub-compiler.</param>
        /// <param name="scheduledActivityManager">The scheduler for any asynchronous tasks that must be executed by the compiler.</param>
        public FunctionCompileHandler(TargetCompiler parent, IScheduledActivityManager scheduledActivityManager)
            : base(parent, scheduledActivityManager)
        {
        }

        #region Overrides

        /// <inheritdoc />
        public override bool CanHandleThis(DfirRootRuntimeType runtimeType)
        {
            return runtimeType == FunctionMocPlugin.FunctionRuntimeType ||
             runtimeType == DfirRootRuntimeType.TypeType;
             // runtimeType == SharedLibraryMocPlugin.SharedLibraryType;
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
            BuildSpec htmlVIBuildSpec = specAndQName.BuildSpec;

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

            IBuiltPackage builtPackage = null;
            if (!RebarFeatureToggles.IsLLVMCompilerEnabled)
            {
                Function compiledFunction = CompileFunction(targetDfir, cancellationToken);
                builtPackage = new FunctionBuiltPackage(specAndQName, Compiler.TargetName, compiledFunction);
            }
            else
            {
                Module module = new Module("module");
                LLVM.FunctionCompiler functionCompiler = new LLVM.FunctionCompiler(module, specAndQName.RuntimeName);
                functionCompiler.Execute(targetDfir, cancellationToken);
                builtPackage = new LLVM.FunctionBuiltPackage(specAndQName, Compiler.TargetName, module);
            }

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

        internal static Function CompileFunction(DfirRoot dfirRoot, CompileCancellationToken cancellationToken)
        {
            ExecutionOrderSortingVisitor.SortDiagrams(dfirRoot);

            var variableAllocations = VariableReference.CreateDictionaryWithUniqueVariableKeys<ValueSource>();
            var allocator = new Allocator(variableAllocations);
            allocator.Execute(dfirRoot, cancellationToken);

            IEnumerable<LocalAllocationValueSource> localAllocations = variableAllocations.Values.OfType<LocalAllocationValueSource>();
            int[] localSizes = new int[localAllocations.Count()];
            foreach (var allocation in localAllocations)
            {
                localSizes[allocation.Index] = allocation.Size;
            }

            var functionBuilder = new FunctionBuilder()
            {
                Name = dfirRoot.SpecAndQName.EditorName,
                LocalSizes = localSizes
            };
            new FunctionCompiler(functionBuilder, variableAllocations).Execute(dfirRoot, cancellationToken);

            // TODO: need to be able to put this in FunctionCompiler:
            functionBuilder.EmitReturn();

            return functionBuilder.CreateFunction();
        }

        #endregion

        /// <inheritdoc/>
        public override CompileSignature PredictCompileSignatureCore(DfirRoot targetDfir, CompileSignature previousSignature)
        {
            return null;
        }
    }
}
