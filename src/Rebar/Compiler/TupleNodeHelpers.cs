using System;
using NationalInstruments;
using NationalInstruments.DataTypes;
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
                nodeFacade[outputTerminal] = new SimpleTerminalFacade(outputTerminal, elementType);
                elementTypes[i] = elementType;
            }

            TypeVariableReference tupleType = typeVariableSet.CreateReferenceToTupleType(elementTypes);
            Terminal inputTerminal = decomposeTupleNode.InputTerminals[0];
            nodeFacade[inputTerminal] = new SimpleTerminalFacade(inputTerminal, tupleType);
        }

        public static DecomposeStructNode CreateDecomposeStructNodeWithFacades(Diagram parentDiagram, NIType structType)
        {
            var decomposeStructNode = new DecomposeStructNode(parentDiagram, structType);
            AutoBorrowNodeFacade nodeFacade = AutoBorrowNodeFacade.GetNodeFacade(decomposeStructNode);
            TypeVariableSet typeVariableSet = parentDiagram.GetTypeVariableSet();

            foreach (var pair in decomposeStructNode.OutputTerminals.Zip(structType.GetFields()))
            {
                Terminal outputTerminal = pair.Key;
                NIType elementType = pair.Value.GetDataType();
                TypeVariableReference elementTypeVariable = typeVariableSet.CreateTypeVariableReferenceFromNIType(elementType);
                nodeFacade[outputTerminal] = new SimpleTerminalFacade(outputTerminal, elementTypeVariable);
            }

            TypeVariableReference structTypeVariable = typeVariableSet.CreateTypeVariableReferenceFromNIType(structType);
            Terminal inputTerminal = decomposeStructNode.InputTerminals[0];
            nodeFacade[inputTerminal] = new SimpleTerminalFacade(inputTerminal, structTypeVariable);
            return decomposeStructNode;
        }
    }
}
