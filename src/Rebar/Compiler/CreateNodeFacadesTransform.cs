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

        protected override void VisitDiagram(Diagram diagram)
        {
            _typeVariableSet = _typeVariableSet ?? diagram.DfirRoot.GetTypeVariableSet();
            LifetimeGraphIdentifier diagramGraphIdentifier = new LifetimeGraphIdentifier(diagram.UniqueId);
            diagram.SetLifetimeGraphIdentifier(diagramGraphIdentifier);
            Diagram parentDiagram = diagram.ParentNode?.ParentDiagram;
            LifetimeGraphIdentifier parentGraphIdentifier = parentDiagram != null 
                ? new LifetimeGraphIdentifier(parentDiagram.UniqueId) 
                : default(LifetimeGraphIdentifier);
            diagram.DfirRoot.GetLifetimeGraphTree().EstablishLifetimeGraph(diagramGraphIdentifier, parentGraphIdentifier);
            diagram.SetVariableSet(new VariableSet(_typeVariableSet));
        }

        protected override void VisitWire(Wire wire)
        {
            TypeVariableReference wireTypeVariable;
            var constraints = new List<Constraint>();
            if (wire.SinkTerminals.HasMoreThan(1))
            {
                constraints.Add(new CopyConstraint());
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

            private LifetimeTypeVariableGroup(Diagram diagram, VariableSet variableSet)
            {                
                _variableSet = variableSet;
                _typeVariableSet = variableSet.TypeVariableSet;
                LifetimeGraphTree lifetimeGraphTree = diagram.DfirRoot.GetLifetimeGraphTree();
                LifetimeGraphIdentifier diagramGraphIdentifier = diagram.GetLifetimeGraphIdentifier();
                LazyNewLifetime = new Lazy<Lifetime>(() => lifetimeGraphTree.CreateLifetimeThatIsBoundedByLifetimeGraph(diagramGraphIdentifier));
                LifetimeType = _typeVariableSet.CreateReferenceToLifetimeType(LazyNewLifetime);
            }

            public static LifetimeTypeVariableGroup CreateFromTerminal(Terminal terminal)
            {
                return new LifetimeTypeVariableGroup(terminal.ParentDiagram, terminal.ParentDiagram.GetVariableSet());
            }

            public static LifetimeTypeVariableGroup CreateFromNode(Node node)
            {
                return new LifetimeTypeVariableGroup(node.ParentDiagram, node.ParentDiagram.GetVariableSet());
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
                    _typeVariableSet.CreateReferenceToLiteralType(constant.DataType.GetReferentType()),
                    // Assume for now that the reference will be in Lifetime.Static
                    _typeVariableSet.CreateReferenceToLifetimeType(Lifetime.Static));
            }
            else
            {
                constantTypeReference = _typeVariableSet.CreateReferenceToLiteralType(constant.DataType);
            }
            _nodeFacade[valueOutput] = new SimpleTerminalFacade(valueOutput, constantTypeReference);
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
                VariableSet variableSet = explicitBorrowNode.ParentDiagram.GetVariableSet();
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
            int inputIndex = 0, outputIndex = 0;
            var genericTypeParameters = new Dictionary<NIType, TypeVariableReference>();
            var lifetimeFacadeGroups = new Dictionary<NIType, ReferenceInputTerminalLifetimeGroup>();
            var lifetimeVariableGroups = new Dictionary<NIType, LifetimeTypeVariableGroup>();

            if (functionalNode.Signature.IsOpenGeneric())
            {
                foreach (NIType genericParameterNIType in functionalNode.Signature.GetGenericParameters())
                {
                    if (genericParameterNIType.IsGenericParameter())
                    {
                        if (genericParameterNIType.IsLifetimeType())
                        {
                            var group = LifetimeTypeVariableGroup.CreateFromNode(functionalNode);
                            lifetimeVariableGroups[genericParameterNIType] = group;
                            genericTypeParameters[genericParameterNIType] = group.LifetimeType;
                        }
                        else if (genericParameterNIType.IsMutabilityType())
                        {
                            genericTypeParameters[genericParameterNIType] = _typeVariableSet.CreateReferenceToMutabilityType();
                        }
                        else
                        {
                            var typeConstraints = genericParameterNIType.GetConstraints().Select(CreateConstraintFromGenericNITypeConstraint).ToList();
                            genericTypeParameters[genericParameterNIType] = _typeVariableSet.CreateReferenceToNewTypeVariable(typeConstraints);
                        }
                    }
                }
            }

            foreach (NIType parameter in functionalNode.Signature.GetParameters())
            {
                NIType parameterDataType = parameter.GetDataType();
                bool isInput = parameter.GetInputParameterPassingRule() != NIParameterPassingRule.NotAllowed,
                    isOutput = parameter.GetOutputParameterPassingRule() != NIParameterPassingRule.NotAllowed;
                Terminal inputTerminal = null, outputTerminal = null;
                if (isInput)
                {
                    inputTerminal = functionalNode.InputTerminals[inputIndex];
                    ++inputIndex;
                }
                if (isOutput)
                {
                    outputTerminal = functionalNode.OutputTerminals[outputIndex];
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
                    TypeVariableReference typeVariableReference = CreateTypeVariableReferenceFromNIType(parameterDataType, genericTypeParameters);
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
                        TypeVariableReference typeVariableReference = CreateTypeVariableReferenceFromNIType(parameterDataType, genericTypeParameters);
                        _nodeFacade[inputTerminal] = new SimpleTerminalFacade(inputTerminal, typeVariableReference);
                    }
                }
                else
                {
                    throw new NotSupportedException("Parameter is neither input nor output");
                }
            }
            return true;
        }

        private Constraint CreateConstraintFromGenericNITypeConstraint(NIType niTypeConstraint)
        {
            if (niTypeConstraint.IsInterface() && niTypeConstraint.GetName() == "Display")
            {
                return new DisplayTraitConstraint();
            }
            else
            {
                throw new NotImplementedException("Don't know how to translate generic type constraint " + niTypeConstraint);
            }
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
            TypeVariableReference referentTypeVariableReference = CreateTypeVariableReferenceFromNIType(parameterDataType.GetReferentType(), genericTypeParameters);
            TypeVariableReference mutabilityTypeVariableReference = mutability == InputReferenceMutability.Polymorphic
                ? genericTypeParameters[parameterDataType.GetReferenceMutabilityType()]
                : default(TypeVariableReference);
            facadeGroup.AddTerminalFacade(inputTerminal, referentTypeVariableReference, mutabilityTypeVariableReference, outputTerminal);
        }

        private TypeVariableReference CreateTypeVariableReferenceFromNIType(NIType type, Dictionary<NIType, TypeVariableReference> genericTypeParameters)
        {
            if (type.IsGenericParameter())
            {
                return genericTypeParameters[type];
            }
            else if (!type.IsGenericType())
            {
                return _typeVariableSet.CreateReferenceToLiteralType(type);
            }
            else
            {
                if (type.IsRebarReferenceType())
                {
                    TypeVariableReference referentType = CreateTypeVariableReferenceFromNIType(type.GetReferentType(), genericTypeParameters);
                    TypeVariableReference lifetimeType = CreateTypeVariableReferenceFromNIType(type.GetReferenceLifetimeType(), genericTypeParameters);
                    if (type.IsPolymorphicReferenceType())
                    {
                        TypeVariableReference mutabilityType = CreateTypeVariableReferenceFromNIType(type.GetReferenceMutabilityType(), genericTypeParameters);
                        return _typeVariableSet.CreateReferenceToPolymorphicReferenceType(mutabilityType, referentType, lifetimeType);
                    }
                    return _typeVariableSet.CreateReferenceToReferenceType(type.IsMutableReferenceType(), referentType, lifetimeType);
                }
                string constructorTypeName = type.GetName();
                var constructorParameters = type.GetGenericParameters();
                if (constructorParameters.Count == 1)
                {
                    TypeVariableReference parameterType = CreateTypeVariableReferenceFromNIType(constructorParameters.ElementAt(0), genericTypeParameters);
                    return _typeVariableSet.CreateReferenceToConstructorType(constructorTypeName, parameterType);
                }
                throw new NotImplementedException();
            }
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
            // TODO: iteratorType should be an Iterator trait constraint, related to itemType
            TypeVariableReference itemType = _typeVariableSet.CreateReferenceToLiteralType(PFTypes.Int32);
            TypeVariableReference iteratorType = _typeVariableSet.CreateReferenceToConstructorType("Iterator", itemType);
            _nodeFacade
                .CreateInputLifetimeGroup(InputReferenceMutability.RequireMutable, lifetimeTypeVariableGroup.LazyNewLifetime, lifetimeTypeVariableGroup.LifetimeType)
                .AddTerminalFacade(iteratorInput, iteratorType, default(TypeVariableReference));

            _nodeFacade[itemOutput] = new SimpleTerminalFacade(itemOutput, itemType);
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitLockTunnel(LockTunnel lockTunnel)
        {
            Terminal lockInput = lockTunnel.InputTerminals.ElementAt(0),
                referenceOutput = lockTunnel.OutputTerminals.ElementAt(0);
            LifetimeTypeVariableGroup lifetimeTypeVariableGroup = LifetimeTypeVariableGroup.CreateFromTerminal(lockInput);
            TypeVariableReference dataVariableType = _typeVariableSet.CreateReferenceToNewTypeVariable();
            TypeVariableReference lockType = _typeVariableSet.CreateReferenceToConstructorType("LockingCell", dataVariableType);
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

            TypeVariableReference boolType = _typeVariableSet.CreateReferenceToLiteralType(PFTypes.Boolean);
            _nodeFacade[loopConditionInput] = new SimpleTerminalFacade(loopConditionInput, boolType);
            Lifetime innerLifetime = loopConditionOutput.GetDiagramLifetime();
            TypeVariableReference boolReferenceType = _typeVariableSet.CreateReferenceToReferenceType(
                true,
                boolType,
                _typeVariableSet.CreateReferenceToLifetimeType(innerLifetime));
            _nodeFacade[loopConditionOutput] = new SimpleTerminalFacade(loopConditionOutput, boolReferenceType);
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitTunnel(Tunnel tunnel)
        {
            Terminal valueInput = tunnel.InputTerminals.ElementAt(0),
                valueOutput = tunnel.OutputTerminals.ElementAt(0);

            TypeVariableReference typeVariable;

            bool executesConditionally = DoesStructureExecuteConditionally(tunnel.ParentStructure);
            if (executesConditionally && tunnel.Direction == Direction.Output)
            {
                typeVariable = _typeVariableSet.CreateReferenceToNewTypeVariable();
                _nodeFacade[valueOutput] = new SimpleTerminalFacade(valueOutput, typeVariable);
                _nodeFacade[valueInput] = new TunnelTerminalFacade(valueInput, _nodeFacade[valueOutput]);
            }
            else
            {
                List<Constraint> constraints = new List<Constraint>();
                if (tunnel.Direction == Direction.Output)
                {
                    // TODO: for multi-frame structures, not sure which lifetime graph to use here
                    LifetimeGraphIdentifier parentLifetimeGraph = valueInput.ParentDiagram.GetLifetimeGraphIdentifier();
                    constraints.Add(new OutlastsLifetimeGraphConstraint(parentLifetimeGraph));
                }
                typeVariable = _typeVariableSet.CreateReferenceToNewTypeVariable(constraints);
                _nodeFacade[valueOutput] = new SimpleTerminalFacade(valueOutput, typeVariable);
                _nodeFacade[valueInput] = new SimpleTerminalFacade(valueInput, typeVariable);
            }
            return true;
        }

        private bool DoesStructureExecuteConditionally(Structure structure)
        {
            Frame frame = structure as Frame;
            if (frame != null)
            {
                // TODO: handle multi-frame flat sequence structures
                return frame.BorderNodes.OfType<UnwrapOptionTunnel>().Any();
            }
            return structure is Nodes.Loop;
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
                _typeVariableSet.CreateReferenceToConstructorType("Option", innerTypeVariable));
            _nodeFacade[unwrappedOutput] = new SimpleTerminalFacade(unwrappedOutput, innerTypeVariable);
            return true;
        }
    }
}
