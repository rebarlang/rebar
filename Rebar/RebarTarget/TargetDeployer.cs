using System;
using System.IO;
using System.Threading.Tasks;
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
            _context = new ExecutionContext(_executionTarget.Host);
        }

        private void HandleDeploymentFinished(IDeployedPackage topLevelDeployedPackage)
        {
            // TODO: finalize module, set up execution engine
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
}
