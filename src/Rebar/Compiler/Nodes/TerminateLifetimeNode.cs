using System.Linq;
using NationalInstruments.Dfir;
using NationalInstruments.DataTypes;
using Rebar.Common;
using NationalInstruments.CommonModel;

namespace Rebar.Compiler.Nodes
{
    internal class TerminateLifetimeNode : DfirNode
    {
        public TerminateLifetimeNode(Node parentNode, int inputs, int outputs) : base(parentNode)
        {
            var immutableReferenceType = NITypes.Void.CreateImmutableReference();
            for (int i = 0; i < inputs; ++i)
            {
                CreateTerminal(Direction.Input, immutableReferenceType, "inner lifetime");
            }
            for (int i = 0; i < outputs; ++i)
            {
                CreateTerminal(Direction.Output, immutableReferenceType, "outer lifetime");
            }
            UnificationState = new TerminateLifetimeUnificationState(ParentDiagram);
        }

        private TerminateLifetimeNode(Node parentNode, TerminateLifetimeNode nodeToCopy, NodeCopyInfo nodeCopyInfo)
            : base(parentNode, nodeToCopy, nodeCopyInfo)
        {
            UnificationState = nodeToCopy.UnificationState;
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new TerminateLifetimeNode(newParentNode, this, copyInfo);
        }

        public TerminateLifetimeUnificationState UnificationState { get; }

        public TerminateLifetimeErrorState ErrorState { get; set; }

        public int? RequiredInputCount { get; set; }

        public int? RequiredOutputCount { get; set; }

        /// <inheritdoc />
        public override T AcceptVisitor<T>(IDfirNodeVisitor<T> visitor)
        {
            return visitor.VisitTerminateLifetimeNode(this);
        }

        public void UpdateInputTerminals(int inputTerminalCount)
        {
            AutoBorrowNodeFacade nodeFacade = AutoBorrowNodeFacade.GetNodeFacade(this);
            var immutableReferenceType = NITypes.Void.CreateImmutableReference();
            int currentInputTerminalCount = InputTerminals.Count();
            if (currentInputTerminalCount < inputTerminalCount)
            {
                for (; currentInputTerminalCount < inputTerminalCount; ++currentInputTerminalCount)
                {
                    var terminal = CreateTerminal(Direction.Input, immutableReferenceType, "inner lifetime");
                    nodeFacade[terminal] = new SimpleTerminalFacade(terminal, terminal.GetTypeVariableSet().CreateReferenceToNewTypeVariable());
                    MoveTerminalToIndex(terminal, currentInputTerminalCount);
                }
            }
            else if (currentInputTerminalCount > inputTerminalCount)
            {
                int i = currentInputTerminalCount - 1;
                while (i >= 0 && currentInputTerminalCount > inputTerminalCount)
                {
                    Terminal inputTerminal = InputTerminals.ElementAt(i);
                    if (!inputTerminal.IsConnected)
                    {
                        RemoveTerminalAtIndex(inputTerminal.Index);
                        --currentInputTerminalCount;
                    }
                    --i;
                }
            }
        }

        public void UpdateOutputTerminals(int outputTerminalCount)
        {
            AutoBorrowNodeFacade nodeFacade = AutoBorrowNodeFacade.GetNodeFacade(this);
            var immutableReferenceType = NITypes.Void.CreateImmutableReference();
            int currentOutputTerminalCount = OutputTerminals.Count();
            if (currentOutputTerminalCount < outputTerminalCount)
            {
                for (; currentOutputTerminalCount < outputTerminalCount; ++currentOutputTerminalCount)
                {
                    var terminal = CreateTerminal(Direction.Output, immutableReferenceType, "outer lifetime");
                    nodeFacade[terminal] = new SimpleTerminalFacade(terminal, terminal.GetTypeVariableSet().CreateReferenceToNewTypeVariable());
                }
            }
            else if (currentOutputTerminalCount > outputTerminalCount)
            {
                int i = currentOutputTerminalCount - 1;
                while (i >= 0 && currentOutputTerminalCount > outputTerminalCount)
                {
                    Terminal outputTerminal = OutputTerminals.ElementAt(i);
                    if (!outputTerminal.IsConnected)
                    {
                        RemoveTerminalAtIndex(outputTerminal.Index);
                        --currentOutputTerminalCount;
                    }
                    --i;
                }
            }
        }
    }

    internal enum TerminateLifetimeErrorState
    {
        InputLifetimesNotUnique,

        InputLifetimeCannotBeTerminated,

        NotAllVariablesInLifetimeConnected,

        NoError,
    }
}
