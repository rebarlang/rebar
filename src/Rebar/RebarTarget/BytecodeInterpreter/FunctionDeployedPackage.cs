using NationalInstruments.ExecutionFramework;

namespace Rebar.RebarTarget.BytecodeInterpreter
{
    /// <summary>
    /// Represents the result of deploying a compiled <see cref="Function"/>.
    /// </summary>
    public class FunctionDeployedPackage : IDeployedPackage
    {
        public static FunctionDeployedPackage DeployFunction(FunctionBuiltPackage builtPackage, ExecutionTarget target, ExecutionContext context)
        {
            context.LoadFunction(builtPackage.Function);
            return new FunctionDeployedPackage(builtPackage.RuntimeEntityIdentity, target, context);
        }

        private FunctionDeployedPackage(IRuntimeEntityIdentity identity, ExecutionTarget executionTarget, ExecutionContext context)
        {
            DeployedPackageIdentity = identity;
            Executable = new ExecutableFunction(executionTarget, context, identity);
        }

        /// <inheritdoc />
        public IRuntimeEntityIdentity DeployedPackageIdentity { get; }

        /// <summary>
        /// The <see cref="ExecutableFunction"/> representing the compiled <see cref="Function"/>.
        /// </summary>
        internal ExecutableFunction Executable { get; }
    }
}
