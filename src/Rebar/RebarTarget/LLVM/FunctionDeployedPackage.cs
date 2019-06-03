using NationalInstruments.ExecutionFramework;
using Rebar.RebarTarget.Execution;

namespace Rebar.RebarTarget.LLVM
{
    /// <summary>
    /// Represents the result of deploying a compiled <see cref="Function"/>.
    /// </summary>
    public class FunctionDeployedPackage : IDeployedPackage
    {
        public static FunctionDeployedPackage DeployFunction(FunctionBuiltPackage builtPackage, ExecutionTarget target)
        {
            return new FunctionDeployedPackage(builtPackage.RuntimeEntityIdentity, target);
        }

        private FunctionDeployedPackage(IRuntimeEntityIdentity identity, ExecutionTarget executionTarget)
        {
            DeployedPackageIdentity = identity;
        }

        /// <inheritdoc />
        public IRuntimeEntityIdentity DeployedPackageIdentity { get; }
    }
}
