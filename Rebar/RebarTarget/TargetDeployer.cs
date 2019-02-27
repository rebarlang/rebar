using System;
using System.IO;
using System.Threading.Tasks;
using NationalInstruments.ExecutionFramework;

namespace Rebar.RebarTarget
{
    public class TargetDeployer : NationalInstruments.ExecutionFramework.TargetDeployer
    {
        private readonly ExecutionTarget _executionTarget;

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
            // TODO: set up a top-level container module to link everything into
        }

        private void HandleDeploymentFinished(IDeployedPackage topLevelDeployedPackage)
        {
            // TODO: finalize module, set up execution engine
        }

        public override Task<IDeployedPackage> DeploySinglePackageAsync(IBuiltPackage package, bool isTopLevel)
        {
            var deployedPackage = new FunctionDeployedPackage((FunctionBuiltPackage)package, _executionTarget);
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
