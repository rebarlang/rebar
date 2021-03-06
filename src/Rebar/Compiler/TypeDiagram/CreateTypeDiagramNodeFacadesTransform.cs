﻿using System;
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

            // Tech debt: none of this should be here.
            // When I first put in type inference for TypeDiagrams, I reused the VariableSet/TypeVariableSet stuff
            // for Function diagrams because it was convenient, even though TypeDiagrams don't need variables at all.
            // Later, to fix a bug, I required that creating a new VariableReference would require knowing the LifetimeGraphIdentifier
            // of the diagram that defines the VariableReference, which meant that a TypeDiagram's DfirRoot.BlockDiagram
            // would need a LifetimeGraphIdentifier.
            //
            // Removing this would require doing TypeDiagram type inference using just TypeVariableSet, and associating
            // TypeVariableReferences with Terminals without VariableReferences.
            dfirRoot.BlockDiagram.SetLifetimeGraphIdentifier(default(LifetimeGraphIdentifier));
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
                foreach (Terminal inputTerminal in selfTypeNode.InputTerminals)
                {
                    TypeVariableReference typeVariable = typeVariableSet.CreateReferenceToNewTypeVariable();
                    nodeFacade[inputTerminal] = new SimpleTerminalFacade(inputTerminal, typeVariable);
                }
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
