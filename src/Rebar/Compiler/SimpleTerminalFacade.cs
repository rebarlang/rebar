using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler.Nodes;

namespace Rebar.Compiler
{
    /// <summary>
    /// <see cref="TerminalFacade"/> implementation for a non-reference terminal, i.e., one that does not expect to be
    /// auto-borrowed. Its true and facade variables are always identical.
    /// </summary>
    internal class SimpleTerminalFacade : TerminalFacade
    {
        public SimpleTerminalFacade(Terminal terminal, TypeVariableReference terminalTypeReference)
            : base(terminal)
        {
            bool terminalIsWireFirst = terminal.IsOutput && !(terminal.ParentNode is TerminateLifetimeNode);
            bool mutableVariable = false;
            var parentWire = terminal.ParentNode as Wire;
            if (parentWire != null)
            {
                mutableVariable = parentWire.GetWireBeginsMutableVariable();
            }
            else if (terminal.IsConnected && terminalIsWireFirst)
            {
                var connectedWire = (Wire)terminal.ConnectedTerminal.ParentNode;
                connectedWire.SetIsFirstVariableWire(true);
                mutableVariable = connectedWire.GetWireBeginsMutableVariable();
            }
            TrueVariable = terminal.GetVariableSet().CreateNewVariable(terminalTypeReference, mutableVariable);
        }

        public override VariableReference FacadeVariable => TrueVariable;

        public override VariableReference TrueVariable { get; }

        public override void UnifyWithConnectedWireTypeAsNodeInput(VariableReference wireFacadeVariable, TerminalTypeUnificationResults unificationResults)
        {
            ITypeUnificationResult unificationResult = unificationResults.GetTypeUnificationResult(
                Terminal,
                FacadeVariable.TypeVariableReference,
                wireFacadeVariable.TypeVariableReference);
            FacadeVariable.UnifyTypeVariableInto(wireFacadeVariable, unificationResult);
            FacadeVariable.MergeInto(wireFacadeVariable);
        }
    }
}
