using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NationalInstruments;
using NationalInstruments.Composition;
using NationalInstruments.Core;
using NationalInstruments.ExecutionFramework;

namespace Rebar.RebarTarget
{
    public class ExecutionTarget : NationalInstruments.ExecutionFramework.ExecutionTarget
    {
        public ExecutionTarget(ICompositionHost host) 
            : base(new RuntimeExecutionTarget(host))
        {
            Host = host;
        }

        public override ICompositionHost Host { get; }

        public override bool IsSimulationTarget => false;

        public override IEnumerable<ITopLevelExecutable> GetAllClones(ITopLevelExecutable masterExecutable)
        {
            return Enumerable.Empty<ITopLevelExecutable>();
        }

        public override Task<IEnumerable<ITopLevelExecutable>> GetAllClonesAsync(ITopLevelExecutable masterExecutable)
        {
            return Task.FromResult(Enumerable.Empty<ITopLevelExecutable>());
        }

        public override IEnumerable<CallStackEntry> GetClonePath(ITopLevelExecutable executable)
        {
            return Enumerable.Empty<CallStackEntry>();
        }

        public override ITopLevelExecutable GetRunningFunctionCloneFromCaller(ITopLevelExecutable caller, ITopLevelExecutable callee, ContentId modelIdentifier, out int? cloneNumberOut)
        {
            cloneNumberOut = null;
            return null;
        }

        public override ITopLevelExecutable GetStatefulFunctionClone(ITopLevelExecutable caller, ITopLevelExecutable callee, string callIdentifier, out int? cloneNumberOut)
        {
            cloneNumberOut = null;
            return null;
        }

        public override ITopLevelExecutable GetStatelessFunctionClone(ITopLevelExecutable executable, int cloneNumber)
        {
            return null;
        }

        /// <inheritdoc />
        public override void UnloadFunction(CompilableDefinitionName executableName)
        {
        }

        /// <inheritdoc />
        protected override IExecutable TryGetExecutable(CompilableDefinitionName executableName, int cloneNumber)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Notifies that an <see cref="ExecutableFunction"/> has been created through deployment.
        /// </summary>
        /// <param name="executableFunction">The created function.</param>
        internal void OnExecutableCreated(ExecutableFunction executableFunction)
        {
            OnExecutableCreated(
                executableFunction,
                executableFunction.CompiledName.ToEnumerable(),
                false);
        }
    }
}
