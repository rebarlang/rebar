using System.Collections.Generic;
using NationalInstruments.Dfir;
using Rebar.Common;

namespace Rebar.Compiler
{
    internal class TunnelTerminalFacade : TerminalFacade
    {
        private readonly TerminalFacade _outputTerminalFacade;

        public TunnelTerminalFacade(Terminal terminal, TerminalFacade outputTerminalFacade) : base(terminal)
        {
            LifetimeGraphIdentifier innerDiagramLifetimeGraph = Terminal.ParentDiagram.GetLifetimeGraphIdentifier();
            var constraint = new OutlastsLifetimeGraphConstraint(innerDiagramLifetimeGraph);
            TypeVariableReference inputTypeReference = terminal.GetTypeVariableSet().CreateReferenceToNewTypeVariable(new List<Constraint>() { constraint });
            TrueVariable = terminal.CreateNewVariable(inputTypeReference);
            _outputTerminalFacade = outputTerminalFacade;
        }

        public override VariableReference FacadeVariable => TrueVariable;

        public override VariableReference TrueVariable { get; }

        public override void UnifyWithConnectedWireTypeAsNodeInput(VariableReference wireFacadeVariable, ITypeUnificationResultFactory unificationResultFactory)
        {
            TypeVariableSet typeVariableSet = Terminal.GetTypeVariableSet();
            Terminal.UnifyTerminalTypeWith(TrueVariable.TypeVariableReference, wireFacadeVariable.TypeVariableReference, unificationResultFactory);
            TrueVariable.MergeInto(wireFacadeVariable);

            TypeVariableReference optionType;
            if (typeVariableSet.GetTypeName(TrueVariable.TypeVariableReference) == "Option")
            {
                optionType = TrueVariable.TypeVariableReference;
            }
            else
            {
                optionType = typeVariableSet.CreateReferenceToOptionType(TrueVariable.TypeVariableReference);
            }
            TypeVariableReference outputTypeReference = _outputTerminalFacade.TrueVariable.TypeVariableReference;
            _outputTerminalFacade.Terminal.UnifyTerminalTypeWith(outputTypeReference, optionType, unificationResultFactory);
        }
    }
}
