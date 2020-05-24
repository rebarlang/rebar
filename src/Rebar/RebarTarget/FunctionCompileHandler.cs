using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NationalInstruments;
using NationalInstruments.CommonModel;
using NationalInstruments.Compiler;
using NationalInstruments.Core;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using NationalInstruments.ExecutionFramework;
using Rebar.Compiler;
using Rebar.Compiler.Nodes;
using static NationalInstruments.Dfir.DfirDependencyNameStagingUtilities;

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
        public override async Task<Tuple<CompileCacheEntry, CompileSignature>> CompileCoreAsync(
            CompileSpecification compileSpecification,
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

            var compileSignatures = new Dictionary<CompilableDefinitionName, CompileSignature>();
            var dependencyIdentities = new HashSet<CompileSpecification>();
            foreach (var dependency in targetDfir.Dependencies.OfType<CompileInvalidationDfirDependency>().ToList())
            {
                dependencyIdentities.Add(ConvertToCompileSpecificationDuringMigration(dependency.DependeeName));
                var compileSignature = await Compiler.GetCompileSignatureAsync(dependency.DependeeName, cancellationToken, progressToken, compileThreadState);
                if (compileSignature != null)
                {
                    targetDfir.AddDependency(
                        targetDfir,
                        new CompileSignatureDependency(dependency.DependeeName, compileSignature));
                    compileSignatures[ConvertToCompilableDefinitionNameDuringMigration(dependency.DependeeName)] = compileSignature;
                }
            }

            var calleesIsYielding = new Dictionary<CompilableDefinitionName, bool>();
            var calleesMayPanic = new Dictionary<CompilableDefinitionName, bool>();
            foreach (var methodCallNode in targetDfir.GetAllNodesIncludingSelf().OfType<MethodCallNode>())
            {
                CompileSignature calleeSignature = compileSignatures[methodCallNode.TargetName];
                var functionCompileSignature = calleeSignature as FunctionCompileSignature;
                bool mayPanic = functionCompileSignature?.MayPanic ?? false;
                calleesIsYielding[methodCallNode.TargetName] = calleeSignature.IsYielding;
                calleesMayPanic[methodCallNode.TargetName] = mayPanic;
            }

            LLVM.FunctionCompileResult functionCompileResult = CompileFunctionForLLVM(
                targetDfir,
                cancellationToken,
                calleesIsYielding,
                calleesMayPanic);
            var builtPackage = new LLVM.FunctionBuiltPackage(
                compileSpecification,
                Compiler.TargetName,
                dependencyIdentities.ToArray(),
                functionCompileResult.Module,
                functionCompileResult.IsYielding);

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

            var topSignature = new FunctionCompileSignature(
                functionName: targetDfir.Name,
                compileSignatureParameters: compileSignatureParameters,
                isYielding: functionCompileResult.IsYielding,
                mayPanic: functionCompileResult.MayPanic);

            return new Tuple<CompileCacheEntry, CompileSignature>(entry, topSignature);
        }

        internal static LLVM.FunctionCompileResult CompileFunctionForLLVM(
            DfirRoot dfirRoot,
            CompileCancellationToken cancellationToken,
            Dictionary<CompilableDefinitionName, bool> calleesIsYielding,
            Dictionary<CompilableDefinitionName, bool> calleesMayPanic,
            string compiledFunctionName = "")
        {
            // TODO: running this here because it needs to know which callee Functions are yielding/panicking.
            new AsyncNodeDecompositionTransform(calleesIsYielding, calleesMayPanic, new NodeInsertionTypeUnificationResultFactory())
                .Execute(dfirRoot, cancellationToken);

            ExecutionOrderSortingVisitor.SortDiagrams(dfirRoot);

            var asyncStateGrouper = new AsyncStateGrouper();
            asyncStateGrouper.Execute(dfirRoot, cancellationToken);
            IEnumerable<AsyncStateGroup> asyncStateGroups = asyncStateGrouper.GetAsyncStateGroups();
#if DEBUG
            string prettyPrintAsyncStateGroups = asyncStateGroups.PrettyPrintAsyncStateGroups();
#endif
            bool isYielding = asyncStateGroups.Select(g => g.FunctionId).Distinct().HasMoreThan(1);
            bool mayPanic = asyncStateGroups.Any(VisitationExtensions.GroupStartsWithPanicOrContinue);

            using (var contextWrapper = new LLVM.ContextWrapper())
            {
                var module = contextWrapper.CreateModule("module");
                var functionImporter = new LLVM.FunctionImporter(contextWrapper, module);

                var variableStorage = new LLVM.FunctionVariableStorage();
                var allocator = new Allocator(contextWrapper, variableStorage, asyncStateGroups);
                allocator.Execute(dfirRoot, cancellationToken);

                compiledFunctionName = string.IsNullOrEmpty(compiledFunctionName) ? FunctionLLVMName(dfirRoot.CompileSpecification.Name) : compiledFunctionName;

                var parameterInfos = dfirRoot.DataItems.OrderBy(d => d.ConnectorPaneIndex).Select(ToParameterInfo).ToArray();
                var sharedData = new LLVM.FunctionCompilerSharedData(
                    contextWrapper,
                    parameterInfos,
                    allocator.AllocationSet,
                    variableStorage,
                    functionImporter);
                var moduleBuilder = isYielding
                    ? new LLVM.AsynchronousFunctionModuleBuilder(module, sharedData, compiledFunctionName, asyncStateGroups)
                    : (LLVM.FunctionModuleBuilder)new LLVM.SynchronousFunctionModuleBuilder(module, sharedData, compiledFunctionName, asyncStateGroups);
                sharedData.VisitationHandler = new LLVM.FunctionCompiler(dfirRoot, moduleBuilder, sharedData, calleesMayPanic);

                moduleBuilder.CompileFunction();
                return new LLVM.FunctionCompileResult(new LLVM.ContextFreeModule(module), isYielding, mayPanic);
            }
        }

        internal static string FunctionLLVMName(CompilableDefinitionName functionCompilableDefinitionName)
        {
            return FunctionLLVMName(functionCompilableDefinitionName.SourceName);
        }

        internal static string FunctionLLVMName(QualifiedName functionAbsoluteQualifiedName)
        {
            // TODO: this used to be the relative qualified name, obtained with AbsoluteQualifiedNameToQualifiedName().
            return string.Join("::", functionAbsoluteQualifiedName.Identifiers.Where(identifier => !string.IsNullOrEmpty(identifier)));
        }

        private static LLVM.ParameterInfo ToParameterInfo(DataItem dataItem)
        {
            Direction direction;
            if (dataItem.ConnectorPaneInputPassingRule == NIParameterPassingRule.Required
                && dataItem.ConnectorPaneOutputPassingRule == NIParameterPassingRule.NotAllowed)
            {
                direction = Direction.Input;
            }
            else if (dataItem.ConnectorPaneInputPassingRule == NIParameterPassingRule.NotAllowed
                && dataItem.ConnectorPaneOutputPassingRule == NIParameterPassingRule.Optional)
            {
                direction = Direction.Output;
            }
            else
            {
                throw new NotImplementedException("Can only handle in and out parameters");
            }
            return new LLVM.ParameterInfo(dataItem.GetVariable(), direction);
        }

        #endregion

        /// <inheritdoc/>
        public override CompileSignature PredictCompileSignatureCore(DfirRoot targetDfir, CompileSignature previousSignature)
        {
            return null;
        }
    }
}
