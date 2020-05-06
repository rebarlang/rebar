using NationalInstruments.ExecutionFramework;

namespace Rebar.Compiler
{
    /// <summary>
    /// Envoy service which implements the IFunctionExecutionService interface for a Function in the project.
    /// </summary>
    /// <remarks>This allows an individual <see cref="Function"/> envoy to connect to the execution service for
    /// the target it is under.</remarks>
    public class FunctionExecutionService : NationalInstruments.MocCommon.FunctionExecutionService
    {
        /// <inheritdoc/>
        protected override void AssureDocumentListeningToDebugEvents(IDebuggableFunction debuggable)
        {
#if FALSE
            ITopLevelExecutable executable = debuggable.Executable;
            ClonePath clonePath = GetClonePathForExecutable(executable);
            var editor = AssociatedEnvoy.Edit(clonePath, typeof(Design.VIDiagramControl));
            if (editor != null)
            {
                var document = editor.Document as Design.VIDocument;
                if (document != null)
                {
                    // Getting the debuggable will make sure it has signed up for debug events.
                    document.GetDebuggableFunction(clonePath);
                }
            }
#endif
        }
    }
}
