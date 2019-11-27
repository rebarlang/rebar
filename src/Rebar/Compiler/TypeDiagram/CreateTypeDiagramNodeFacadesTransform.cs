using System;
using System.Linq;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler.Nodes;

namespace Rebar.Compiler.TypeDiagram
{
    internal class CreateTypeDiagramNodeFacadesTransform : VisitorTransformBase
    {
        protected override void VisitDfirRoot(DfirRoot dfirRoot)
        {
            base.VisitDfirRoot(dfirRoot);
            TypeVariableSet typeVariableSet = dfirRoot.GetTypeVariableSet();
            dfirRoot.SetVariableSet(new VariableSet(typeVariableSet));
        }

        protected override void VisitBorderNode(NationalInstruments.Dfir.BorderNode borderNode)
        {
            throw new NotImplementedException();
        }

        protected override void VisitNode(Node node)
        {
            TypeVariableSet typeVariableSet = node.GetTypeVariableSet();
            AutoBorrowNodeFacade nodeFacade = AutoBorrowNodeFacade.GetNodeFacade(node);
            var primitive = node as PrimitiveTypeNode;
            var selfTypeNode = node as SelfTypeNode;
            if (primitive != null)
            {
                nodeFacade[primitive.OutputTerminal] = new SimpleTerminalFacade(
                    primitive.OutputTerminal,
                    typeVariableSet.CreateTypeVariableReferenceFromNIType(primitive.Type));
                return;
            }
            if (selfTypeNode != null)
            {
                TypeVariableReference selfTypeVariable = typeVariableSet.CreateReferenceToNewTypeVariable();
                nodeFacade[selfTypeNode.InputTerminal] = new SimpleTerminalFacade(selfTypeNode.InputTerminal, selfTypeVariable);
                return;
            }
            throw new NotSupportedException($"Unsupported node type: {node.GetType().Name}");
        }

        protected override void VisitWire(Wire wire)
        {
            TypeVariableReference wireTypeVariable = wire.GetTypeVariableSet()
                .CreateReferenceToNewTypeVariable(Enumerable.Empty<Constraint>());

            AutoBorrowNodeFacade wireFacade = AutoBorrowNodeFacade.GetNodeFacade(wire);
            foreach (var terminal in wire.Terminals)
            {
                wireFacade[terminal] = new SimpleTerminalFacade(terminal, wireTypeVariable);
            }
        }
    }
}
