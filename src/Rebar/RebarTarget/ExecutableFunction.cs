using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using NationalInstruments.Compiler;
using NationalInstruments.Composition;
using NationalInstruments.DataValues;
using NationalInstruments.ExecutionFramework;

namespace Rebar.RebarTarget
{
    /// <summary>
    /// Represents an executable Rebar function.
    /// </summary>
    /// <remarks>This implements <see cref="ITopLevelPanelExecutable"/> instead of just <see cref="ITopLevelExecutable"/>
    /// because that's what <see cref="DataflowDocument"/>, the parent class of <see cref="FunctionDocument"/>, seems
    /// to require. These executable functions do not actually provide a panel.</remarks>
    internal class ExecutableFunction : ITopLevelPanelExecutable
    {
        private ISimpleExecutionState _currentSimpleExecutionState = DefaultExecutionState.Idle;
        private readonly LLVM.ExecutionContext _llvmContext;

        public ExecutableFunction(ExecutionTarget target, LLVM.ExecutionContext context, IRuntimeEntityIdentity runtimeIdentity)
        {
            CreatedDate = DateTime.Now;
            ExecutionTarget = target;
            CompiledName = runtimeIdentity.EditorName;
            RuntimeName = FunctionCompileHandler.FunctionLLVMName((SpecAndQName)runtimeIdentity);
            _llvmContext = context;
        }

        /// <inheritdoc />
        public string CompiledComponentName => RuntimeNameHelper.GetComponentNameFromRuntimeString(RuntimeName);

        /// <inheritdoc />
        public string CompiledName { get; }

        /// <inheritdoc />
        public DateTime CreatedDate { get; }

        /// <inheritdoc />
        public ISimpleExecutionState CurrentSimpleExecutionState
        {
            get
            {
                return _currentSimpleExecutionState;
            }
            private set
            {
                CurrentSimpleExecutionStateChanging?.Invoke(
                    this,
                    new CurrentStateChangedEventArgs<ISimpleExecutionState>(
                        ExecutionTarget, 
                        this, 
                        _currentSimpleExecutionState, 
                        value));
                var previousState = _currentSimpleExecutionState;
                _currentSimpleExecutionState = value;
                CurrentSimpleExecutionStateChanged?.Invoke(
                    this,
                    new CurrentStateChangedEventArgs<ISimpleExecutionState>(
                        ExecutionTarget, 
                        this, 
                        previousState, 
                        _currentSimpleExecutionState));
            }
        }

        /// <inheritdoc />
        public IExecutionTarget ExecutionTarget { get; }

        /// <inheritdoc />
        public ICompositionHost Host => ExecutionTarget.Host;

        /// <inheritdoc />
        public bool IsInitialized => true;

        /// <inheritdoc />
        public bool IsRunInProgressAsTopLevel => (DefaultExecutionState)_currentSimpleExecutionState == DefaultExecutionState.RunningTopLevel;

        /// <inheritdoc />
        public bool MatchesUnmodifiedSource => true;

        /// <inheritdoc />
        public IRuntimeExecutionTarget RuntimeExecutionTarget
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <inheritdoc />
        public string RuntimeName { get; }

        public event EventHandler<CurrentStateChangedEventArgs<ISimpleExecutionState>> CurrentSimpleExecutionStateChanged;
        public event EventHandler<CurrentStateChangedEventArgs<ISimpleExecutionState>> CurrentSimpleExecutionStateChanging;
        public event EventHandler<NationalInstruments.ExecutionFramework.ErrorReportedEventArgs> ErrorReported;
        public event EventHandler<ExecutionStoppedEventArgs> ExecutionStopped;
        public event PropertyChangedEventHandler PropertyChanged;

        /// <inheritdoc />
        public void Abort()
        {
        }

        /// <inheritdoc />
        public void StartRun()
        {
            CurrentSimpleExecutionState = DefaultExecutionState.RunningTopLevel;
            _llvmContext.ExecuteFunctionTopLevel(RuntimeName);
            ExecutionStopped?.Invoke(this, new ExecutionStoppedEventArgs(this));
            CurrentSimpleExecutionState = DefaultExecutionState.Idle;
        }

        #region IPanelExecutable implementation

        IRuntimePanel IPanelExecutable.ActivePanel { get; set; }

        bool IPanelExecutable.CanTakeOwnershipOfPanel => false;

        IDataspace IPanelExecutable.Dataspace => null;

        private EventHandler<PanelOwnershipChangedEventArgs> _panelOwnershipChanged;

        event EventHandler<PanelOwnershipChangedEventArgs> IPanelExecutable.PanelOwnershipChanged
        {
            add
            {
                _panelOwnershipChanged += value;
            }
            remove
            {
                _panelOwnershipChanged -= value;
            }
        }

        void IPanelExecutable.DataspacePropertyChanged(string name, object oldValue, object newValue)
        {
        }

        IEnumerable<IRuntimePanel> IPanelExecutable.FindCallerPanels()
        {
            return Enumerable.Empty<IRuntimePanel>();
        }

        IRuntimePanel IPanelExecutable.FindOrCreatePanel(IRuntimePanelOwner owner)
        {
            return null;
        }

        Task<ICluster> IPanelExecutable.OccurEventAsync(uint instanceId, uint eventTypeId, object eventData, string runtimeDataType, bool mustCompletedBeforeAbort)
        {
            return Task.FromResult((ICluster)null);
        }

        #endregion
    }
}
