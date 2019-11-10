using System;
using System.Collections.Generic;
using System.Linq;
using NationalInstruments;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler.Nodes;

namespace Rebar.Compiler
{
    internal class CreateNodeFacadesTransform : VisitorTransformBase, IDfirNodeVisitor<bool>
    {
        private AutoBorrowNodeFacade _nodeFacade;
        private TypeVariableSet _typeVariableSet;

        protected override void VisitDfirRoot(DfirRoot dfirRoot)
        {
            base.VisitDfirRoot(dfirRoot);
            _typeVariableSet = dfirRoot.GetTypeVariableSet();
            dfirRoot.SetVariableSet(new VariableSet(_typeVariableSet));
        }

        protected override void VisitDiagram(Diagram diagram)
        {
            LifetimeGraphIdentifier diagramGraphIdentifier = new LifetimeGraphIdentifier(diagram.UniqueId);
            diagram.SetLifetimeGraphIdentifier(diagramGraphIdentifier);
            Diagram parentDiagram = diagram.ParentNode?.ParentDiagram;
            LifetimeGraphIdentifier parentGraphIdentifier = parentDiagram != null 
                ? new LifetimeGraphIdentifier(parentDiagram.UniqueId) 
                : default(LifetimeGraphIdentifier);
            diagram.DfirRoot.GetLifetimeGraphTree().EstablishLifetimeGraph(diagramGraphIdentifier, parentGraphIdentifier);
        }

        protected override void VisitWire(Wire wire)
        {
            TypeVariableReference wireTypeVariable;
            var constraints = new List<Constraint>();
            if (wire.SinkTerminals.HasMoreThan(1))
            {
                constraints.Add(new CopyTraitConstraint());
            }
            wireTypeVariable = _typeVariableSet.CreateReferenceToNewTypeVariable(constraints);

            AutoBorrowNodeFacade wireFacade = AutoBorrowNodeFacade.GetNodeFacade(wire);
            foreach (var terminal in wire.Terminals)
            {
                wireFacade[terminal] = new SimpleTerminalFacade(terminal, wireTypeVariable);
            }
            Terminal sourceTerminal, firstSinkTerminal;
            if (wire.TryGetSourceTerminal(out sourceTerminal) && (firstSinkTerminal = wire.SinkTerminals.FirstOrDefault()) != null)
            {
                wireFacade[firstSinkTerminal].FacadeVariable.MergeInto(wireFacade[sourceTerminal].FacadeVariable);
            }
        }

        protected override void VisitNode(Node node)
        {
            _nodeFacade = AutoBorrowNodeFacade.GetNodeFacade(node);
            this.VisitRebarNode(node);
            _nodeFacade = null;
        }

        protected override void VisitBorderNode(NationalInstruments.Dfir.BorderNode borderNode)
        {
            _nodeFacade = AutoBorrowNodeFacade.GetNodeFacade(borderNode);
            this.VisitRebarNode(borderNode);
            _nodeFacade = null;
        }

        private class LifetimeTypeVariableGroup
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

        bool IDfirNodeVisitor<bool>.VisitConstant(Constant constant)
        {
            Terminal valueOutput = constant.OutputTerminals.ElementAt(0);
            TypeVariableReference constantTypeReference;
            if (constant.DataType.IsRebarReferenceType())
            {
                constantTypeReference = _typeVariableSet.CreateReferenceToReferenceType(
                    false,
                    // TODO: this is not always correct; need a more general way of turning NITypes into TypeVariableReferences
                    _typeVariableSet.CreateTypeVariableReferenceFromNIType(constant.DataType.GetReferentType()),
                    // Assume for now that the reference will be in Lifetime.Static
                    _typeVariableSet.CreateReferenceToLifetimeType(Lifetime.Static));
            }
            else
            {
                constantTypeReference = _typeVariableSet.CreateTypeVariableReferenceFromNIType(constant.DataType);
            }
            _nodeFacade[valueOutput] = new SimpleTerminalFacade(valueOutput, constantTypeReference);
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitDataAccessor(DataAccessor dataAccessor)
        {
            if (dataAccessor.Terminal.Direction == Direction.Output
                || dataAccessor.Terminal.Direction == Direction.Input)
            {
                TypeVariableReference dataTypeVariable = _typeVariableSet.CreateTypeVariableReferenceFromNIType(dataAccessor.DataItem.DataType);
                _nodeFacade[dataAccessor.Terminal] = new SimpleTerminalFacade(dataAccessor.Terminal, dataTypeVariable);
            }
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitDropNode(DropNode dropNode)
        {
            Terminal valueInput = dropNode.InputTerminals.ElementAt(0);
            TypeVariableReference dataTypeVariable = _typeVariableSet.CreateReferenceToNewTypeVariable();
            _nodeFacade[valueInput] = new SimpleTerminalFacade(valueInput, dataTypeVariable);
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitExplicitBorrowNode(ExplicitBorrowNode explicitBorrowNode)
        {
            if (explicitBorrowNode.AlwaysCreateReference && explicitBorrowNode.AlwaysBeginLifetime)
            {
                bool mutable = explicitBorrowNode.BorrowMode == BorrowMode.Mutable;
                Lifetime borrowLifetime = explicitBorrowNode.OutputTerminals.First().DefineLifetimeThatIsBoundedByDiagram();
                TypeVariableReference borrowLifetimeType = _typeVariableSet.CreateReferenceToLifetimeType(borrowLifetime);

                foreach (var terminalPair in explicitBorrowNode.InputTerminals.Zip(explicitBorrowNode.OutputTerminals))
                {
                    Terminal inputTerminal = terminalPair.Key, outputTerminal = terminalPair.Value;
                    TypeVariableReference inputTypeVariable = _typeVariableSet.CreateReferenceToNewTypeVariable();
                    _nodeFacade[inputTerminal] = new SimpleTerminalFacade(inputTerminal, inputTypeVariable);
                    TypeVariableReference outputReferenceType = _typeVariableSet.CreateReferenceToReferenceType(mutable, inputTypeVariable, borrowLifetimeType);
                    _nodeFacade[outputTerminal] = new SimpleTerminalFacade(outputTerminal, outputReferenceType);
                }
            }
            else
            {
                // TODO
                throw new NotImplementedException();
            }

            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitFunctionalNode(FunctionalNode functionalNode)
        {
            VisitFunctionSignatureNode(functionalNode, functionalNode.Signature);
            return true;
        }

        private void CreateFacadesForInoutReferenceParameter(
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
                facadeGroup = _nodeFacade.CreateInputLifetimeGroup(mutability, lifetimeGroup.LazyNewLifetime, lifetimeGroup.LifetimeType);
            }
            // TODO: should not add outputTerminal here if borrow cannot be auto-terminated
            // i.e., if there are in-only or out-only parameters that share lifetimeType
            TypeVariableReference referentTypeVariableReference = _typeVariableSet.CreateTypeVariableReferenceFromNIType(parameterDataType.GetReferentType(), genericTypeParameters);
            TypeVariableReference mutabilityTypeVariableReference = mutability == InputReferenceMutability.Polymorphic
                ? genericTypeParameters[parameterDataType.GetReferenceMutabilityType()]
                : default(TypeVariableReference);
            facadeGroup.AddTerminalFacade(inputTerminal, referentTypeVariableReference, mutabilityTypeVariableReference, outputTerminal);
        }

        bool IDfirNodeVisitor<bool>.VisitTerminateLifetimeNode(TerminateLifetimeNode terminateLifetimeNode)
        {
            foreach (var terminal in terminateLifetimeNode.Terminals)
            {
                // TODO: when updating terminals during SA, also update the TerminalFacades
                _nodeFacade[terminal] = new TerminateLifetimeInputTerminalFacade(terminal, terminateLifetimeNode.UnificationState);
            }
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitBorrowTunnel(BorrowTunnel borrowTunnel)
        {
            // T -> &'a (mode) T
            Terminal valueInput = borrowTunnel.InputTerminals.ElementAt(0),
                borrowOutput = borrowTunnel.OutputTerminals.ElementAt(0);
            TypeVariableReference dataTypeVariable = _typeVariableSet.CreateReferenceToNewTypeVariable();
            Lifetime innerLifetime = borrowOutput.GetDiagramLifetime();
            TypeVariableReference referenceType = _typeVariableSet.CreateReferenceToReferenceType(
                borrowTunnel.BorrowMode == BorrowMode.Mutable,
                dataTypeVariable,
                _typeVariableSet.CreateReferenceToLifetimeType(innerLifetime));
            _nodeFacade[valueInput] = new SimpleTerminalFacade(valueInput, dataTypeVariable);
            _nodeFacade[borrowOutput] = new SimpleTerminalFacade(borrowOutput, referenceType);
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitIterateTunnel(IterateTunnel iterateTunnel)
        {
            Terminal iteratorInput = iterateTunnel.InputTerminals.ElementAt(0),
                itemOutput = iterateTunnel.OutputTerminals.ElementAt(0);
            LifetimeTypeVariableGroup lifetimeTypeVariableGroup = LifetimeTypeVariableGroup.CreateFromTerminal(iteratorInput);

            TypeVariableReference loopLifetimeReference = _typeVariableSet.CreateReferenceToLifetimeType(itemOutput.GetDiagramLifetime());
            TypeVariableReference itemTypeVariable = _typeVariableSet.CreateReferenceToNewTypeVariable();
            TypeVariableReference implementsIteratorTypeVariable = _typeVariableSet.CreateReferenceToNewTypeVariable(
                new Constraint[] { new IteratorTraitConstraint(itemTypeVariable) });
            _nodeFacade
                .CreateInputLifetimeGroup(InputReferenceMutability.RequireMutable, lifetimeTypeVariableGroup.LazyNewLifetime, lifetimeTypeVariableGroup.LifetimeType)
                .AddTerminalFacade(iteratorInput, implementsIteratorTypeVariable, default(TypeVariableReference));

            _nodeFacade[itemOutput] = new SimpleTerminalFacade(itemOutput, itemTypeVariable);
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitLockTunnel(LockTunnel lockTunnel)
        {
            Terminal lockInput = lockTunnel.InputTerminals.ElementAt(0),
                referenceOutput = lockTunnel.OutputTerminals.ElementAt(0);
            LifetimeTypeVariableGroup lifetimeTypeVariableGroup = LifetimeTypeVariableGroup.CreateFromTerminal(lockInput);
            TypeVariableReference dataVariableType = _typeVariableSet.CreateReferenceToNewTypeVariable();
            TypeVariableReference lockType = _typeVariableSet.CreateReferenceToLockingCellType(dataVariableType);
            _nodeFacade
                .CreateInputLifetimeGroup(InputReferenceMutability.AllowImmutable, lifetimeTypeVariableGroup.LazyNewLifetime, lifetimeTypeVariableGroup.LifetimeType)
                .AddTerminalFacade(lockInput, lockType, default(TypeVariableReference));

            Lifetime innerLifetime = referenceOutput.GetDiagramLifetime();
            TypeVariableReference referenceType = _typeVariableSet.CreateReferenceToReferenceType(
                true,
                lockType,
                _typeVariableSet.CreateReferenceToLifetimeType(innerLifetime));
            _nodeFacade[referenceOutput] = new SimpleTerminalFacade(referenceOutput, referenceType);
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitLoopConditionTunnel(LoopConditionTunnel loopConditionTunnel)
        {
            // TODO: how to determine the mutability of the outer loop condition variable?
            Terminal loopConditionInput = loopConditionTunnel.InputTerminals.ElementAt(0),
                loopConditionOutput = loopConditionTunnel.OutputTerminals.ElementAt(0);

            TypeVariableReference boolType = _typeVariableSet.CreateTypeVariableReferenceFromNIType(PFTypes.Boolean);
            _nodeFacade[loopConditionInput] = new SimpleTerminalFacade(loopConditionInput, boolType);
            Lifetime innerLifetime = loopConditionOutput.GetDiagramLifetime();
            TypeVariableReference boolReferenceType = _typeVariableSet.CreateReferenceToReferenceType(
                true,
                boolType,
                _typeVariableSet.CreateReferenceToLifetimeType(innerLifetime));
            _nodeFacade[loopConditionOutput] = new SimpleTerminalFacade(loopConditionOutput, boolReferenceType);
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitMethodCallNode(MethodCallNode methodCallNode)
        {
            VisitFunctionSignatureNode(methodCallNode, methodCallNode.Signature);
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitOptionPatternStructureSelector(OptionPatternStructureSelector optionPatternStructureSelector)
        {
            Terminal selectorInput = optionPatternStructureSelector.InputTerminals[0],
                selectorSomeOutput = optionPatternStructureSelector.OutputTerminals[0];

            TypeVariableReference innerTypeVariable = _typeVariableSet.CreateReferenceToNewTypeVariable(),
                outerTypeReference = _typeVariableSet.CreateReferenceToOptionType(innerTypeVariable);
            _nodeFacade[selectorInput] = new SimpleTerminalFacade(selectorInput, outerTypeReference);
            _nodeFacade[selectorSomeOutput] = new SimpleTerminalFacade(selectorSomeOutput, innerTypeVariable);
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitTunnel(Tunnel tunnel)
        {
            CreateTunnelNodeFacade(tunnel);
            return true;
        }

        internal static void CreateTunnelNodeFacade(Tunnel tunnel)
        {
            AutoBorrowNodeFacade nodeFacade = AutoBorrowNodeFacade.GetNodeFacade(tunnel);
            TypeVariableSet typeVariableSet = tunnel.DfirRoot.GetTypeVariableSet();

            TypeVariableReference typeVariable;

            bool executesConditionally = tunnel.ParentStructure.DoesStructureExecuteConditionally();
            if (executesConditionally && tunnel.Direction == Direction.Output)
            {
                Terminal valueInput = tunnel.InputTerminals.ElementAt(0),
                    valueOutput = tunnel.OutputTerminals.ElementAt(0);

                typeVariable = typeVariableSet.CreateReferenceToNewTypeVariable();
                nodeFacade[valueOutput] = new SimpleTerminalFacade(valueOutput, typeVariable);
                nodeFacade[valueInput] = new TunnelTerminalFacade(valueInput, nodeFacade[valueOutput]);
            }
            else
            {
                List<Constraint> constraints = new List<Constraint>();
                if (tunnel.Direction == Direction.Output)
                {
                    // TODO: for multi-diagram structures, each diagram should share a lifetime related to the entire structure
                    LifetimeGraphIdentifier parentLifetimeGraph = tunnel.InputTerminals[0].ParentDiagram.GetLifetimeGraphIdentifier();
                    constraints.Add(new OutlastsLifetimeGraphConstraint(parentLifetimeGraph));
                }
                typeVariable = typeVariableSet.CreateReferenceToNewTypeVariable(constraints);
                foreach (Terminal terminal in tunnel.Terminals)
                {
                    nodeFacade[terminal] = new SimpleTerminalFacade(terminal, typeVariable);
                }
            }
        }

        bool IDfirNodeVisitor<bool>.VisitTerminateLifetimeTunnel(TerminateLifetimeTunnel terminateLifetimeTunnel)
        {
            Terminal valueOutput = terminateLifetimeTunnel.OutputTerminals.ElementAt(0);
            var valueFacade = new SimpleTerminalFacade(valueOutput, default(TypeVariableReference));
            _nodeFacade[valueOutput] = valueFacade;

            NationalInstruments.Dfir.BorderNode beginLifetimeBorderNode = (NationalInstruments.Dfir.BorderNode)terminateLifetimeTunnel.BeginLifetimeTunnel;
            Terminal beginLifetimeTerminal = beginLifetimeBorderNode.GetOuterTerminal(0);
            valueFacade.FacadeVariable.MergeInto(beginLifetimeTerminal.GetFacadeVariable());
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitUnwrapOptionTunnel(UnwrapOptionTunnel unwrapOptionTunnel)
        {
            Terminal optionInput = unwrapOptionTunnel.InputTerminals[0],
                unwrappedOutput = unwrapOptionTunnel.OutputTerminals[0];
            TypeVariableReference innerTypeVariable = _typeVariableSet.CreateReferenceToNewTypeVariable();
            _nodeFacade[optionInput] = new SimpleTerminalFacade(
                optionInput, 
                _typeVariableSet.CreateReferenceToOptionType(innerTypeVariable));
            _nodeFacade[unwrappedOutput] = new SimpleTerminalFacade(unwrappedOutput, innerTypeVariable);
            return true;
        }

        private void VisitFunctionSignatureNode(Node node, NIType nodeFunctionSignature)
        {
            int inputIndex = 0, outputIndex = 0;
            var genericTypeParameters = new Dictionary<NIType, TypeVariableReference>();
            var lifetimeFacadeGroups = new Dictionary<NIType, ReferenceInputTerminalLifetimeGroup>();
            var lifetimeVariableGroups = new Dictionary<NIType, LifetimeTypeVariableGroup>();

            TypeVariableReference[] signatureTypeParameters;
            if (nodeFunctionSignature.IsOpenGeneric())
            {
                Func<NIType, TypeVariableReference> createLifetimeTypeReference = type =>
                {
                    var group = LifetimeTypeVariableGroup.CreateFromNode(node);
                    lifetimeVariableGroups[type] = group;
                    return group.LifetimeType;
                };
                genericTypeParameters = _typeVariableSet.CreateTypeVariablesForGenericParameters(nodeFunctionSignature, createLifetimeTypeReference);

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
                    TypeVariableReference typeVariableReference = _typeVariableSet.CreateTypeVariableReferenceFromNIType(parameterDataType, genericTypeParameters);
                    _nodeFacade[outputTerminal] = new SimpleTerminalFacade(outputTerminal, typeVariableReference);
                }
                else if (isInput)
                {
                    if (parameterDataType.IsRebarReferenceType())
                    {
                        CreateFacadesForInoutReferenceParameter(
                            parameterDataType,
                            inputTerminal,
                            null,
                            genericTypeParameters,
                            lifetimeFacadeGroups,
                            lifetimeVariableGroups);
                    }
                    else
                    {
                        TypeVariableReference typeVariableReference = _typeVariableSet.CreateTypeVariableReferenceFromNIType(parameterDataType, genericTypeParameters);
                        _nodeFacade[inputTerminal] = new SimpleTerminalFacade(inputTerminal, typeVariableReference);
                    }
                }
                else
                {
                    throw new NotSupportedException("Parameter is neither input nor output");
                }
            }
        }
    }
}
