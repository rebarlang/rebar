using NationalInstruments.ExecutionFramework;

namespace Rebar.RebarTarget
{
    /// <summary>
    /// Represents the result of deploying a compiled <see cref="Function"/>.
    /// </summary>
    public class FunctionDeployedPackage : IDeployedPackage
    {
        public FunctionDeployedPackage(FunctionBuiltPackage builtPackage, ExecutionTarget executionTarget)
        {
            DeployedPackageIdentity = builtPackage.RuntimeEntityIdentity;
            Executable = new ExecutableFunction(executionTarget, builtPackage);
        }

        /// <inheritdoc />
        public IRuntimeEntityIdentity DeployedPackageIdentity { get; }

        /// <summary>
        /// The <see cref="ExecutableFunction"/> representing the compiled <see cref="Function"/>.
        /// </summary>
        internal ExecutableFunction Executable { get; }
    }
}
