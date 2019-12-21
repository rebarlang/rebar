using System;
using System.Collections.Generic;
using System.Linq;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler.Nodes;

namespace Rebar.Compiler
{
    internal static class CreateNodeFacadesHelpers
    {
        public static TerminateLifetimeNode CreateTerminateLifetimeWithFacades(Diagram parentDiagram, int inputs, int outputs)
        {
            var terminateLifetime = new TerminateLifetimeNode(parentDiagram, inputs, outputs);
            AutoBorrowNodeFacade terminateLifetimeFacade = AutoBorrowNodeFacade.GetNodeFacade(terminateLifetime);
            TypeVariableSet typeVariableSet = parentDiagram.GetTypeVariableSet();
            foreach (var terminal in terminateLifetime.Terminals)
            {
                terminateLifetimeFacade[terminal] = new SimpleTerminalFacade(terminal, typeVariableSet.CreateReferenceToNewTypeVariable());
            }
            return terminateLifetime;
        }

        public static void CreateFacadesForFunctionSignatureNode(this Node node, NIType nodeFunctionSignature)
        {
            int inputIndex = 0, outputIndex = 0;
            var genericTypeParameters = new Dictionary<NIType, TypeVariableReference>();
            var lifetimeFacadeGroups = new Dictionary<NIType, ReferenceInputTerminalLifetimeGroup>();
            var lifetimeVariableGroups = new Dictionary<NIType, LifetimeTypeVariableGroup>();

            TypeVariableSet typeVariableSet = node.GetTypeVariableSet();
            AutoBorrowNodeFacade nodeFacade = AutoBorrowNodeFacade.GetNodeFacade(node);
            TypeVariableReference[] signatureTypeParameters;
            if (nodeFunctionSignature.IsOpenGeneric())
            {
                Func<NIType, TypeVariableReference> createLifetimeTypeReference = type =>
                {
                    var group = LifetimeTypeVariableGroup.CreateFromNode(node);
                    lifetimeVariableGroups[type] = group;
                    return group.LifetimeType;
                };
                genericTypeParameters = typeVariableSet.CreateTypeVariablesForGenericParameters(nodeFunctionSignature, createLifetimeTypeReference);

                signatureTypeParameters = nodeFunctionSignature.GetGenericParameters().Select(p => genericTypeParameters[p]).ToArray();
            }
            else
            {
                signatureTypeParameters = new TypeVariableReference[0];
            }
            var functionalNode = node as FunctionalNode;
            if (functionalNode != null)
            {
                functionalNode.FunctionType = new FunctionType(nodeFunctionSignature, signatureTypeParameters);
            }

            foreach (NIType parameter in nodeFunctionSignature.GetParameters())
            {
                NIType parameterDataType = parameter.GetDataType();
                bool isInput = parameter.GetInputParameterPassingRule() != NIParameterPassingRule.NotAllowed,
                    isOutput = parameter.GetOutputParameterPassingRule() != NIParameterPassingRule.NotAllowed;
                Terminal inputTerminal = null, outputTerminal = null;
                if (isInput)
                {
                    inputTerminal = node.InputTerminals[inputIndex];
                    ++inputIndex;
                }
                if (isOutput)
                {
                    outputTerminal = node.OutputTerminals[outputIndex];
                    ++outputIndex;
                }
                if (isInput && isOutput)
                {
                    if (parameterDataType.IsRebarReferenceType())
                    {
                        CreateFacadesForInoutReferenceParameter(
                            typeVariableSet,
                            nodeFacade,
                            parameterDataType,
                            inputTerminal,
                            outputTerminal,
                            genericTypeParameters,
                            lifetimeFacadeGroups,
                            lifetimeVariableGroups);
                    }
                    else
                    {
                        throw new NotSupportedException("Inout parameters must be reference types.");
                    }
                }
                else if (isOutput)
                {
                    TypeVariableReference typeVariableReference = typeVariableSet.CreateTypeVariableReferenceFromNIType(parameterDataType, genericTypeParameters);
                    nodeFacade[outputTerminal] = new SimpleTerminalFacade(outputTerminal, typeVariableReference);
                }
                else if (isInput)
                {
                    if (parameterDataType.IsRebarReferenceType())
                    {
                        CreateFacadesForInoutReferenceParameter(
                            typeVariableSet,
                            nodeFacade,
                            parameterDataType,
                            inputTerminal,
                            null,
                            genericTypeParameters,
                            lifetimeFacadeGroups,
                            lifetimeVariableGroups);
                    }
                    else
                    {
                        TypeVariableReference typeVariableReference = typeVariableSet.CreateTypeVariableReferenceFromNIType(parameterDataType, genericTypeParameters);
                        nodeFacade[inputTerminal] = new SimpleTerminalFacade(inputTerminal, typeVariableReference);
                    }
                }
                else
                {
                    throw new NotSupportedException("Parameter is neither input nor output");
                }
            }
        }

        private static void CreateFacadesForInoutReferenceParameter(
            TypeVariableSet typeVariableSet,
            AutoBorrowNodeFacade nodeFacade,
            NIType parameterDataType,
            Terminal inputTerminal,
            Terminal outputTerminal,
            Dictionary<NIType, TypeVariableReference> genericTypeParameters,
            Dictionary<NIType, ReferenceInputTerminalLifetimeGroup> lifetimeFacadeGroups,
            Dictionary<NIType, LifetimeTypeVariableGroup> lifetimeVariableGroups)
        {
            NIType lifetimeType = parameterDataType.GetReferenceLifetimeType();
            bool isMutable = parameterDataType.IsMutableReferenceType();
            InputReferenceMutability mutability = parameterDataType.GetInputReferenceMutabilityFromType();
            var lifetimeGroup = lifetimeVariableGroups[lifetimeType];
            ReferenceInputTerminalLifetimeGroup facadeGroup;
            if (!lifetimeFacadeGroups.TryGetValue(lifetimeType, out facadeGroup))
            {
                facadeGroup = nodeFacade.CreateInputLifetimeGroup(mutability, lifetimeGroup.LazyNewLifetime, lifetimeGroup.LifetimeType);
            }
            // TODO: should not add outputTerminal here if borrow cannot be auto-terminated
            // i.e., if there are in-only or out-only parameters that share lifetimeType
            TypeVariableReference referentTypeVariableReference = typeVariableSet.CreateTypeVariableReferenceFromNIType(parameterDataType.GetReferentType(), genericTypeParameters);
            TypeVariableReference mutabilityTypeVariableReference = mutability == InputReferenceMutability.Polymorphic
                ? genericTypeParameters[parameterDataType.GetReferenceMutabilityType()]
                : default(TypeVariableReference);
            facadeGroup.AddTerminalFacade(inputTerminal, referentTypeVariableReference, mutabilityTypeVariableReference, outputTerminal);
        }
    }

    internal class LifetimeTypeVariableGroup
    {
        private readonly VariableSet _variableSet;
        private readonly TypeVariableSet _typeVariableSet;
        private readonly List<VariableReference> _interruptedVariables = new List<VariableReference>();

        private LifetimeTypeVariableGroup(Diagram diagram)
        {
            _variableSet = diagram.GetVariableSet();
            _typeVariableSet = _variableSet.TypeVariableSet;
            LifetimeGraphTree lifetimeGraphTree = diagram.DfirRoot.GetLifetimeGraphTree();
            LifetimeGraphIdentifier diagramGraphIdentifier = diagram.GetLifetimeGraphIdentifier();
            LazyNewLifetime = new Lazy<Lifetime>(() => lifetimeGraphTree.CreateLifetimeThatIsBoundedByLifetimeGraph(diagramGraphIdentifier));
            LifetimeType = _typeVariableSet.CreateReferenceToLifetimeType(LazyNewLifetime);
        }

        public static LifetimeTypeVariableGroup CreateFromTerminal(Terminal terminal)
        {
            return new LifetimeTypeVariableGroup(terminal.ParentDiagram);
        }

        public static LifetimeTypeVariableGroup CreateFromNode(Node node)
        {
            return new LifetimeTypeVariableGroup(node.ParentDiagram);
        }

        public Lazy<Lifetime> LazyNewLifetime { get; }

        public TypeVariableReference LifetimeType { get; }
    }
}
