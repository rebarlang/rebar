using System;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler.Nodes;

namespace Rebar.Compiler
{
    internal static class TupleNodeHelpers
    {
        public static DecomposeTupleNode CreateDecomposeTupleNodeWithFacades(Diagram parentDiagram, int elementCount, DecomposeMode decomposeMode)
        {
            var decomposeTupleNode = new DecomposeTupleNode(parentDiagram, elementCount, decomposeMode);
            decomposeTupleNode.CreateFacadesForDecomposeTupleNode(parentDiagram.GetTypeVariableSet());
            return decomposeTupleNode;
        }

        private static void CreateFacadesForDecomposeTupleNode(this DecomposeTupleNode decomposeTupleNode, TypeVariableSet typeVariableSet)
        {
            if (decomposeTupleNode.DecomposeMode == DecomposeMode.Borrow)
            {
                throw new NotSupportedException();
            }
            TypeVariableReference[] elementTypes = new TypeVariableReference[decomposeTupleNode.OutputTerminals.Count];
            AutoBorrowNodeFacade nodeFacade = AutoBorrowNodeFacade.GetNodeFacade(decomposeTupleNode);

            for (int i = 0; i < decomposeTupleNode.OutputTerminals.Count; ++i)
            {
                Terminal outputTerminal = decomposeTupleNode.OutputTerminals[i];
                TypeVariableReference elementType = typeVariableSet.CreateReferenceToNewTypeVariable();
                TypeVariableReference outputTerminalType = elementType;
                nodeFacade[outputTerminal] = new SimpleTerminalFacade(outputTerminal, outputTerminalType);
                elementTypes[i] = elementType;
            }

            TypeVariableReference tupleType = typeVariableSet.CreateReferenceToTupleType(elementTypes);
            Terminal inputTerminal = decomposeTupleNode.InputTerminals[0];
            nodeFacade[inputTerminal] = new SimpleTerminalFacade(inputTerminal, tupleType);
        }
    }
}
