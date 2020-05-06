using System;
using System.IO;
using System.Threading.Tasks;
using NationalInstruments.Composition;
using NationalInstruments.Core;
using NationalInstruments.ExecutionFramework;
using NationalInstruments.Linking;

namespace Rebar.RebarTarget
{
    public class TargetDeployer : NationalInstruments.ExecutionFramework.TargetDeployer
    {
        private readonly ExecutionTarget _executionTarget;
        private LLVM.ExecutionContext _llvmExecutionContext;

        public TargetDeployer(GetBuiltPackage getBuildPackageDelegate, GetTargetDeployer getTargetDeployer, ExecutionTarget executionTarget)
            : base(getBuildPackageDelegate, getTargetDeployer)
        {
            _executionTarget = executionTarget;
            AddDeploymentStartingHandler(HandleDeploymentStarting);
            AddDeploymentFinishedHandler(HandleDeploymentFinished);
        }

        public override IDeployedPackage DeployFrom(Stream stream)
        {
            throw new NotImplementedException();
        }

        public override Task PrepareToDeploySinglePackageAsync(IBuiltPackage package, bool isTopLevel)
        {
            return base.PrepareToDeploySinglePackageAsync(package, isTopLevel);
        }

        private void HandleDeploymentStarting(IBuiltPackage topLevelPackage)
        {
            var executionServices = new HostExecutionServices(_executionTarget.Host);
            if (_llvmExecutionContext != null)
            {
                _llvmExecutionContext.Dispose();
            }
            _llvmExecutionContext = new LLVM.ExecutionContext(executionServices);
        }

        private void HandleDeploymentFinished(IDeployedPackage topLevelDeployedPackage)
        {
        }

        public override Task<IDeployedPackage> DeploySinglePackageAsync(IBuiltPackage package, bool isTopLevel)
        {
            var functionDeployedPackage = LLVM.FunctionDeployedPackage.DeployFunction((LLVM.FunctionBuiltPackage)package, _executionTarget, _llvmExecutionContext);
            _executionTarget.OnExecutableCreated(functionDeployedPackage.Executable);
            return Task.FromResult<IDeployedPackage>(functionDeployedPackage);
        }

        public override void Store(IBuiltPackage package, Stream stream)
        {
            throw new NotImplementedException();
        }

        public override Task UnloadAllAsync()
        {
            _llvmExecutionContext.Dispose();
            _llvmExecutionContext = null;
            return Task.CompletedTask;
        }

        protected override void Dispose(bool disposing)
        {
        }

        /// <inheritdoc />
        protected override void UnloadCore(string runtimeName, CompilableDefinitionName editorName)
        {
            throw new NotImplementedException();
        }
    }

    internal class HostExecutionServices : IRebarTargetRuntimeServices
    {
        private readonly IDebugHost _debugHost;
        private static readonly string _messageSource;
        private bool _panicOccurred = false;

        static HostExecutionServices()
        {
            _messageSource = ExtendedQualifiedName.CreateContentName(new QualifiedName("Rebar runtime"), string.Empty, ContentId.EmptyId).ToEncodedString();
        }

        public HostExecutionServices(ICompositionHost host)
        {
            _debugHost = host.GetSharedExportedValue<IDebugHost>();
        }

        public void Output(string value)
        {
            string message = $"Output: {value}";
            _debugHost.LogMessage(new DebugMessage(_messageSource, DebugMessageSeverity.Information, message));
        }

        void IRebarTargetRuntimeServices.FakeDrop(int id)
        {
            throw new NotImplementedException("FakeDrop not supported");
        }

        public bool PanicOccurred
        {
            get { return _panicOccurred; }
            set
            {
                _panicOccurred |= value;
                if (value)
                {
                    _debugHost.LogMessage(new DebugMessage("Rebar runtime", DebugMessageSeverity.Information, "Panic occurred!"));
                }
            }
        }
    }
}
