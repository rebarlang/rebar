using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using LLVMSharp;
using NationalInstruments.Compiler;
using NationalInstruments.Composition;
using NationalInstruments.Core;
using NationalInstruments.Core.IO;
using NationalInstruments.Dfir;
using NationalInstruments.ExecutionFramework;
using NationalInstruments.MocCommon.Components.Compiler;
using Rebar.RebarTarget.LLVM;

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
            var moduleCombiner = new ModuleCombiner(componentBuildResult.GetComponentMemberBuiltPackagesBottomUp().Cast<FunctionBuiltPackage>());
            var topLevelFunction = componentBuildResult.TopLevelMemberNames.First();
            var wasmModuleBuilder = new WasmApplicationModuleBuilder(moduleCombiner, FunctionCompileHandler.FunctionLLVMName(topLevelFunction));
            return new List<IFileBuilder>() { wasmModuleBuilder };
        }

        private class ModuleCombiner
        {
            private readonly IEnumerable<FunctionBuiltPackage> _builtPackages;

            public ModuleCombiner(IEnumerable<FunctionBuiltPackage> builtPackages)
            {
                _builtPackages = builtPackages;
            }

            public Module CreateCombinedModule()
            {
                var module = new Module("combined");
                foreach (FunctionBuiltPackage builtPackage in _builtPackages)
                {
                    module.LinkInModule(builtPackage.Module.Clone());
                }

                module.LinkInModule(CommonModules.FakeDropModule.Clone());
                module.LinkInModule(CommonModules.SchedulerModule.Clone());
                module.LinkInModule(CommonModules.StringModule.Clone());
                module.LinkInModule(CommonModules.OutputModule.Clone());
                module.LinkInModule(CommonModules.RangeModule.Clone());
                module.LinkInModule(CommonModules.FileModule.Clone());
                return module;
            }
        }

        private class WasmApplicationModuleBuilder : FileBuilder
        {
            private readonly ModuleCombiner _moduleCombiner;
            private readonly string _topLevelFunctionName;

            public WasmApplicationModuleBuilder(ModuleCombiner moduleCombiner, string topLevelFunctionName)
            {
                _moduleCombiner = moduleCombiner;
                _topLevelFunctionName = topLevelFunctionName;
            }

            public override string OutputRelativeFilePath
            {
                get
                {
                    return "module.wasm";
                }
            }

            protected override Task BuildInternalAsync(string outputFolderPath, CompileCancellationToken cancellationToken)
            {
                Module wasiExecutableModule = WasiModuleBuilder.CreateWasiExecutableModule(_moduleCombiner.CreateCombinedModule(), _topLevelFunctionName);
                string wasiModulePath = LongPath.Combine(outputFolderPath, OutputRelativeFilePath);
                WasiModuleBuilder.LinkWasmModule(wasiExecutableModule, wasiModulePath, containsEntryPoint: true);
                wasiExecutableModule.DisposeModule();
                return AsyncHelpers.CompletedTask;
            }
        }

        /// <inheritdoc />
        protected override IBuiltPackage CreateExecutionBuiltPackage(
            SpecAndQName specAndQName,
            string absoluteDirectoryForComponent,
            IEnumerable<IFileBuilder> builders,
            ComponentBuildResult componentBuildResult)
        {
            return new EmptyBuiltPackage(specAndQName, Compiler.TargetName, componentBuildResult.AllDependencies);
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
