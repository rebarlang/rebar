using System;
using System.IO;
using System.Threading.Tasks;
using NationalInstruments.Composition;
using NationalInstruments.Core;
using NationalInstruments.ExecutionFramework;
using Rebar.RebarTarget.Execution;

namespace Rebar.RebarTarget
{
    public class TargetDeployer : NationalInstruments.ExecutionFramework.TargetDeployer
    {
        private readonly ExecutionTarget _executionTarget;
        private ExecutionContext _context;

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
            _context = new ExecutionContext(new HostExecutionServices(_executionTarget.Host));
        }

        private void HandleDeploymentFinished(IDeployedPackage topLevelDeployedPackage)
        {
            _context.FinalizeLoad();
        }

        public override Task<IDeployedPackage> DeploySinglePackageAsync(IBuiltPackage package, bool isTopLevel)
        {
            var deployedPackage = FunctionDeployedPackage.DeployFunction((FunctionBuiltPackage)package, _executionTarget, _context);
            _executionTarget.OnExecutableCreated(deployedPackage.Executable);
            return Task.FromResult((IDeployedPackage)deployedPackage);
        }

        public override void Store(IBuiltPackage package, Stream stream)
        {
            throw new NotImplementedException();
        }

        public override Task UnloadAllAsync()
        {
            throw new NotImplementedException();
        }

        protected override void Dispose(bool disposing)
        {
        }

        protected override void UnloadCore(string runtimeName, string editorName)
        {
            throw new NotImplementedException();
        }
    }

    internal class HostExecutionServices : IRebarTargetRuntimeServices
    {
        private readonly ICompositionHost _host;
        private readonly IDebugHost _debugHost;

        public HostExecutionServices(ICompositionHost host)
        {
            _host = host;
            _debugHost = host.GetSharedExportedValue<IDebugHost>();
        }

        public void Output(string value)
        {
            _debugHost.LogMessage(new DebugMessage("Rebar runtime", DebugMessageSeverity.Information, value));
        }
    }
}
