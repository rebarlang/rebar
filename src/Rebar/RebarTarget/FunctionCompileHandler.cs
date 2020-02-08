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
using Rebar.Common;
using Rebar.Compiler.Nodes;
using NationalInstruments;

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

            BuiltPackageToken token = Compiler.AddToBuiltPackagesCache(builtPackage);
            CompileCacheEntry entry = await Compiler.CreateStandardCompileCacheEntryFromDfirRootAsync(
                CompileState.Complete,
                targetDfir,
                compileSignatures,
                token,
                cancellationToken,
                progressToken,
                compileThreadState,
                false);

            CompileSignature topSignature = new CompileSignature(
                functionName: targetDfir.Name,
                parameters: compileSignatureParameters, // GenerateParameters(targetDfir),
                declaringType: targetDfir.GetDeclaringType(),
                reentrancy: targetDfir.Reentrancy,
                isYielding: functionCompileResult.IsYielding,
                isFunctional: true,
                threadAffinity: ThreadAffinity.Standard,
                shouldAlwaysInline: false,
                mayWantToInline: true,
                priority: ExecutionPriority.Normal,
                callingConvention: CallingConvention.StdCall);

            return new Tuple<CompileCacheEntry, CompileSignature>(entry, topSignature);
        }

        internal static LLVM.FunctionCompileResult CompileFunctionForLLVM(
            DfirRoot dfirRoot,
            CompileCancellationToken cancellationToken,
            Dictionary<ExtendedQualifiedName, bool> calleesIsYielding,
            string compiledFunctionName = "")
        {
            // TODO: running this here because it needs to know which callee Functions are yielding.
            new AsyncNodeDecompositionTransform(calleesIsYielding, new NodeInsertionTypeUnificationResultFactory())
                .Execute(dfirRoot, cancellationToken);

            ExecutionOrderSortingVisitor.SortDiagrams(dfirRoot);

            var asyncStateGrouper = new AsyncStateGrouper();
            asyncStateGrouper.Execute(dfirRoot, cancellationToken);
            IEnumerable<AsyncStateGroup> asyncStateGroups = asyncStateGrouper.GetAsyncStateGroups();
#if DEBUG
            string prettyPrintAsyncStateGroups = asyncStateGroups.PrettyPrintAsyncStateGroups();
#endif
            bool isYielding = asyncStateGroups.Select(g => g.FunctionId).Distinct().HasMoreThan(1);

            Dictionary<VariableReference, LLVM.ValueSource> valueSources = VariableReference.CreateDictionaryWithUniqueVariableKeys<LLVM.ValueSource>();
            var additionalSources = new Dictionary<object, LLVM.ValueSource>();
            var allocator = new Allocator(valueSources, additionalSources, asyncStateGroups);
            allocator.Execute(dfirRoot, cancellationToken);

            var module = new Module("module");
            compiledFunctionName = string.IsNullOrEmpty(compiledFunctionName) ? FunctionLLVMName(dfirRoot.SpecAndQName) : compiledFunctionName;
            var functionCompiler = new LLVM.FunctionCompiler(
                module,
                compiledFunctionName,
                dfirRoot.DataItems.ToArray(),
                valueSources,
                additionalSources,
                allocator.AllocationSet,
                asyncStateGroups);
            functionCompiler.CompileFunction(dfirRoot);
            return new LLVM.FunctionCompileResult(module, isYielding);
        }

        internal static string FunctionLLVMName(SpecAndQName functionSpecAndQName)
        {
            return FunctionLLVMName(functionSpecAndQName.QualifiedName);
        }

        internal static string FunctionLLVMName(ExtendedQualifiedName functionExtendedQualifiedName)
        {
            QualifiedName relativeQualifiedName = functionExtendedQualifiedName.Name.AbsoluteQualifiedNameToQualifiedName();
            return string.Join("::", relativeQualifiedName.Identifiers);
        }

        #endregion

        /// <inheritdoc/>
        public override CompileSignature PredictCompileSignatureCore(DfirRoot targetDfir, CompileSignature previousSignature)
        {
            return null;
        }
    }
}
