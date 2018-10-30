using System.Collections.Generic;
using System.Linq;
using NationalInstruments.Dfir;
using NationalInstruments.DataTypes;
using NationalInstruments;

namespace RustyWires.Compiler.Nodes
{
    internal class TerminateLifetimeNode : RustyWiresDfirNode
    {
        private enum ErrorState
        {
            InputLifetimesNotUnique,

            InputLifetimeCannotBeTerminated,

            NotAllVariablesInLifetimeConnected,

            NoError,
        }

        private ErrorState _errorState;

        public TerminateLifetimeNode(Node parentNode, int inputs, int outputs) : base(parentNode)
        {
            var immutableReferenceType = PFTypes.Void.CreateImmutableReference();
            for (int i = 0; i < inputs; ++i)
            {
                CreateTerminal(Direction.Input, immutableReferenceType, "inner lifetime");
            }
            for (int i = 0; i < outputs; ++i)
            {
                CreateTerminal(Direction.Output, immutableReferenceType, "outer lifetime");
            }
        }

        private TerminateLifetimeNode(Node parentNode, TerminateLifetimeNode nodeToCopy, NodeCopyInfo nodeCopyInfo)
            : base(parentNode, nodeToCopy, nodeCopyInfo)
        {
        }

        protected override Node CopyNodeInto(Node newParentNode, NodeCopyInfo copyInfo)
        {
            return new TerminateLifetimeNode(newParentNode, this, copyInfo);
        }

        public int? RequiredInputCount { get; private set; }

        public int? RequiredOutputCount { get; private set; }

        /// <inheritdoc />
        public override IEnumerable<PassthroughTerminalPair> PassthroughTerminalPairs => Enumerable.Empty<PassthroughTerminalPair>();

        /// <inheritdoc />
        public override void SetOutputVariableTypesAndLifetimes()
        {
            VariableSet variableSet = ParentDiagram.GetVariableSet();
            IEnumerable<Variable> inputVariables = InputTerminals.Select(VariableSetExtensions.GetVariable).Where(v => v != null);
            IEnumerable<Lifetime> inputLifetimes = inputVariables.Select(v => v.Lifetime).Distinct();
            Lifetime singleLifetime;

            IEnumerable<Variable> decomposedVariables = Enumerable.Empty<Variable>();
            if (inputLifetimes.HasMoreThan(1))
            {
                _errorState = ErrorState.InputLifetimesNotUnique;
            }
            else if ((singleLifetime = inputLifetimes.FirstOrDefault()) == null)
            {
                // this means no inputs were wired, which is an error, but we should report it as unwired inputs
                // in CheckVariableUsages below
                _errorState = ErrorState.NoError;
            }
            else if (singleLifetime.DoesOutlastDiagram || !singleLifetime.IsBounded)
            {
                _errorState = ErrorState.InputLifetimeCannotBeTerminated;
            }
            else
            {
                _errorState = ErrorState.NoError;
                IEnumerable<Variable> variablesMatchingLifetime = variableSet.Variables.Where(v => v.Lifetime == singleLifetime);
                RequiredInputCount = variablesMatchingLifetime.Count();
                if (inputVariables.Count() != RequiredInputCount)
                {
                    _errorState = ErrorState.NotAllVariablesInLifetimeConnected;
                }
                decomposedVariables = variableSet.GetVariablesInterruptedByLifetime(singleLifetime);
                RequiredOutputCount = decomposedVariables.Count();
            }

            var decomposedVariablesConcat = decomposedVariables.Concat(Enumerable.Repeat<Variable>(null, int.MaxValue));
            foreach (var outputTerminalPair in OutputTerminals.Zip(decomposedVariablesConcat))
            {
                Terminal outputTerminal = outputTerminalPair.Key;
                Variable decomposedVariable = outputTerminalPair.Value;
                if (decomposedVariable != null)
                {
                    Variable originalOutputVariable = variableSet.GetVariableForTerminal(outputTerminal);
                    variableSet.MergeVariables(originalOutputVariable, decomposedVariable);
                }
                else
                {
                    variableSet.AddTerminalToNewVariable(outputTerminal);
                }
            }
        }

        /// <inheritdoc />
        public override void CheckVariableUsages()
        {
            foreach (var inputTerminal in InputTerminals)
            {
                VariableUsageValidator validator = new VariableUsageValidator(inputTerminal.GetVariable(), inputTerminal, false);
            }

            switch (_errorState)
            {
                case ErrorState.InputLifetimesNotUnique:
                    this.SetDfirMessage(RustyWiresMessages.TerminateLifetimeInputLifetimesNotUnique);
                    break;
                case ErrorState.InputLifetimeCannotBeTerminated:
                    this.SetDfirMessage(RustyWiresMessages.TerminateLifetimeInputLifetimeCannotBeTerminated);
                    break;
                case ErrorState.NotAllVariablesInLifetimeConnected:
                    this.SetDfirMessage(RustyWiresMessages.TerminateLifetimeNotAllVariablesInLifetimeConnected);
                    break;
            }
        }
    }
}
