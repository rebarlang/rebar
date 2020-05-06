using System;
using NationalInstruments.Compiler;
using NationalInstruments.ExecutionFramework;
using Rebar.RebarTarget.SystemModel;

namespace Rebar.RebarTarget
{
    /// <summary>
    /// Service that is created and attached to a target envoy whose definition has a keyword of 
    /// TargetServices provide a compiler, deploy target, and execution target for Rebar's system diagram envoy.
    /// </summary>
    public class TargetCompilerServices : TargetServices
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public TargetCompilerServices()
        {
            _compiler = new Lazy<TargetCompiler>(
                () => new TargetCompiler(
                AssociatedEnvoy.Project,
                Host,
                TargetCompilerIdentity,
                AssociatedEnvoy.Project.GetPersistentCache(),
                SpecializedTargetCompilerFactories,
                AssociatedEnvoy.Project.GetTargetCompilerLookup()));

            _executionTarget = new Lazy<ExecutionTarget>(() => new ExecutionTarget(Host));

            _deployTarget = new Lazy<TargetDeployer>(
                        () => new TargetDeployer(GetBuiltPackage, GetTargetDeployer, _executionTarget.Value));
        }

        private readonly Lazy<TargetCompiler> _compiler;
        private readonly Lazy<TargetDeployer> _deployTarget;
        private readonly Lazy<ExecutionTarget> _executionTarget;

        /// <inheritdoc/>
        public override NationalInstruments.Compiler.TargetCompiler Compiler => _compiler.Value;

        /// <inheritdoc/>
        public override NationalInstruments.ExecutionFramework.TargetDeployer DeployTarget => _deployTarget.Value;

        /// <inheritdoc/>
        public override IExecutionTarget ExecutionTarget => _executionTarget.Value;

        /// <summary>
        /// Keyword for binding to the Rebar target.
        /// </summary>
        public const string TargetModelName = TargetEnvoyCreationServiceBuilder.TargetKeyword;

        private IBuiltPackage GetBuiltPackage(IRuntimeEntityIdentity name) => _compiler.Value.GetBuiltPackage((CompileSpecification)name);
    }
}
