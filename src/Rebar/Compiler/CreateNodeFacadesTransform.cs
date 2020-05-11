using System;
using System.Collections.Generic;
using System.Linq;
using NationalInstruments;
using NationalInstruments.CommonModel;
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
            var variableSet = new VariableSet(_typeVariableSet);
            dfirRoot.SetVariableSet(variableSet);

            int rootDiagramLifetimeId = 0;
            foreach (DataItem dataItem in dfirRoot.DataItems)
            {
                // TODO: eventually we want to reuse each DataItem's variable for its DataAccessors' terminals.
                TypeVariableReference dataTypeVariable = _typeVariableSet.CreateTypeVariableReferenceFromNIType(dataItem.DataType);
                dataItem.SetVariable(variableSet.CreateNewVariable(rootDiagramLifetimeId, dataTypeVariable, true));
            }
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

        bool IDfirNodeVisitor<bool>.VisitWire(Wire wire)
        {
            VisitWire(wire);
            return true;
        }

        protected override void VisitWire(Wire wire)
        {
            TypeVariableReference wireTypeVariable;
            var constraints = new List<Constraint>();
            if (wire.SinkTerminals.HasMoreThan(1))
            {
                constraints.Add(new SimpleTraitConstraint("Copy"));
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

        bool IDfirNodeVisitor<bool>.VisitBuildTupleNode(BuildTupleNode buildTupleNode)
        {
            TypeVariableReference[] elementTypes = new TypeVariableReference[buildTupleNode.InputTerminals.Count];
            for (int i = 0; i < buildTupleNode.InputTerminals.Count; ++i)
            {
                Terminal inputTerminal = buildTupleNode.InputTerminals[i];
                // TODO: constrain these to be unbounded lifetime
                TypeVariableReference elementType = _typeVariableSet.CreateReferenceToNewTypeVariable();
                _nodeFacade[inputTerminal] = new SimpleTerminalFacade(inputTerminal, elementType);
                elementTypes[i] = elementType;
            }
            TypeVariableReference tupleType = _typeVariableSet.CreateReferenceToTupleType(elementTypes);
            Terminal outputTerminal = buildTupleNode.OutputTerminals[0];
            _nodeFacade[outputTerminal] = new SimpleTerminalFacade(outputTerminal, tupleType);
            return true;
        }


        private ReferenceInputTerminalLifetimeGroup CreateTerminalLifetimeGroup(
            InputReferenceMutability mutability,
            LifetimeTypeVariableGroup variableGroup)
        {
            return _nodeFacade.CreateInputLifetimeGroup(
                mutability,
                variableGroup.LazyNewLifetime,
                variableGroup.LifetimeType);
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
            TypeVariableReference dataTypeVariable = _typeVariableSet.CreateTypeVariableReferenceFromNIType(dataAccessor.DataItem.DataType);
            _nodeFacade[dataAccessor.Terminal] = new SimpleTerminalFacade(dataAccessor.Terminal, dataTypeVariable);
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitDecomposeTupleNode(DecomposeTupleNode decomposeTupleNode)
        {
            TypeVariableReference[] elementTypes = new TypeVariableReference[decomposeTupleNode.OutputTerminals.Count];
            TypeVariableReference mutabilityType = default(TypeVariableReference);
            ReferenceInputTerminalLifetimeGroup inputTerminalGroup = null;
            if (decomposeTupleNode.DecomposeMode == DecomposeMode.Borrow)
            {
                mutabilityType = _typeVariableSet.CreateReferenceToMutabilityType();
                var lifetimeVariableGroup = LifetimeTypeVariableGroup.CreateFromNode(decomposeTupleNode);
                inputTerminalGroup = _nodeFacade.CreateInputLifetimeGroup(
                    InputReferenceMutability.Polymorphic,
                    lifetimeVariableGroup.LazyNewLifetime,
                    lifetimeVariableGroup.LifetimeType);
            }

            for (int i = 0; i < decomposeTupleNode.OutputTerminals.Count; ++i)
            {
                Terminal outputTerminal = decomposeTupleNode.OutputTerminals[i];
                TypeVariableReference elementType = _typeVariableSet.CreateReferenceToNewTypeVariable();
                TypeVariableReference outputTerminalType = decomposeTupleNode.DecomposeMode == DecomposeMode.Borrow
                    ? _typeVariableSet.CreateReferenceToPolymorphicReferenceType(
                        mutabilityType,
                        elementType,
                        inputTerminalGroup.LifetimeType)
                    : elementType;
                _nodeFacade[outputTerminal] = new SimpleTerminalFacade(outputTerminal, outputTerminalType);
                elementTypes[i] = elementType;
            }

            TypeVariableReference tupleType = _typeVariableSet.CreateReferenceToTupleType(elementTypes);
            Terminal inputTerminal = decomposeTupleNode.InputTerminals[0];
            if (decomposeTupleNode.DecomposeMode == DecomposeMode.Borrow)
            {
                inputTerminalGroup.AddTerminalFacade(
                    inputTerminal,
                    tupleType,
                    mutabilityType);
            }
            else
            {
                _nodeFacade[inputTerminal] = new SimpleTerminalFacade(inputTerminal, tupleType);
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
            functionalNode.CreateFacadesForFunctionSignatureNode(functionalNode.Signature);
            return true;
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
            ReferenceInputTerminalLifetimeGroup group = CreateTerminalLifetimeGroup(InputReferenceMutability.RequireMutable, lifetimeTypeVariableGroup);
            group.AddTerminalFacade(iteratorInput, implementsIteratorTypeVariable, default(TypeVariableReference));

            _nodeFacade[itemOutput] = new SimpleTerminalFacade(itemOutput, itemTypeVariable);

            iterateTunnel.IteratorNextFunctionType = new FunctionType(
                Signatures.IteratorNextType,
                new TypeVariableReference[]
                {
                    implementsIteratorTypeVariable,
                    itemTypeVariable,
                    group.LifetimeType
                });
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitLockTunnel(LockTunnel lockTunnel)
        {
            Terminal lockInput = lockTunnel.InputTerminals.ElementAt(0),
                referenceOutput = lockTunnel.OutputTerminals.ElementAt(0);
            LifetimeTypeVariableGroup lifetimeTypeVariableGroup = LifetimeTypeVariableGroup.CreateFromTerminal(lockInput);
            TypeVariableReference dataVariableType = _typeVariableSet.CreateReferenceToNewTypeVariable();
            TypeVariableReference lockType = _typeVariableSet.CreateReferenceToLockingCellType(dataVariableType);
            CreateTerminalLifetimeGroup(InputReferenceMutability.AllowImmutable, lifetimeTypeVariableGroup)
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

            TypeVariableReference boolType = _typeVariableSet.CreateTypeVariableReferenceFromNIType(NITypes.Boolean);
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
            methodCallNode.CreateFacadesForFunctionSignatureNode(methodCallNode.Signature);
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

        bool IDfirNodeVisitor<bool>.VisitStructConstructorNode(StructConstructorNode structConstructorNode)
        {
            NIType structType = structConstructorNode.Type;
            foreach (var pair in structConstructorNode.InputTerminals.Zip(structType.GetFields()))
            {
                Terminal inputTerminal = pair.Key;
                _nodeFacade[inputTerminal] = new SimpleTerminalFacade(
                    inputTerminal,
                    _typeVariableSet.CreateTypeVariableReferenceFromNIType(pair.Value.GetDataType()));
            }
            _nodeFacade[structConstructorNode.OutputTerminals[0]] = new SimpleTerminalFacade(
                structConstructorNode.OutputTerminals[0],
                _typeVariableSet.CreateTypeVariableReferenceFromNIType(structType));
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitStructFieldAccessorNode(StructFieldAccessorNode structFieldAccessorNode)
        {
            TypeVariableReference mutabilityType = _typeVariableSet.CreateReferenceToMutabilityType();
            var lifetimeVariableGroup = LifetimeTypeVariableGroup.CreateFromNode(structFieldAccessorNode);
            ReferenceInputTerminalLifetimeGroup inputTerminalGroup = CreateTerminalLifetimeGroup(
                InputReferenceMutability.Polymorphic,
                lifetimeVariableGroup);

            var fieldTypes = new Dictionary<string, TypeVariableReference>();
            foreach (var terminalPair in structFieldAccessorNode.OutputTerminals.Zip(structFieldAccessorNode.FieldNames))
            {
                string fieldName = terminalPair.Value;
                TypeVariableReference fieldType;
                if (string.IsNullOrEmpty(fieldName))
                {
                    fieldType = _typeVariableSet.CreateReferenceToNewTypeVariable();
                }
                else if (!fieldTypes.TryGetValue(fieldName, out fieldType))
                {
                    fieldType = _typeVariableSet.CreateReferenceToNewTypeVariable();
                    fieldTypes[fieldName] = fieldType;
                }
                TypeVariableReference terminalTypeVariable = _typeVariableSet.CreateReferenceToPolymorphicReferenceType(
                    mutabilityType,
                    fieldType,
                    inputTerminalGroup.LifetimeType);
                Terminal outputTerminal = terminalPair.Key;
                _nodeFacade[outputTerminal] = new SimpleTerminalFacade(outputTerminal, terminalTypeVariable);
            }

            TypeVariableReference fieldedType = _typeVariableSet.CreateReferenceToIndefiniteFieldedType(fieldTypes);
            inputTerminalGroup.AddTerminalFacade(
                structFieldAccessorNode.StructInputTerminal,
                fieldedType,
                mutabilityType);

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

        bool IDfirNodeVisitor<bool>.VisitVariantConstructorNode(VariantConstructorNode variantConstructorNode)
        {
            Terminal inputTerminal = variantConstructorNode.InputTerminals[0],
                outputTerminal = variantConstructorNode.OutputTerminals[0];
            NIType elementType = variantConstructorNode.VariantType.GetFields().ElementAt(variantConstructorNode.SelectedFieldIndex).GetDataType();
            _nodeFacade[inputTerminal] = new SimpleTerminalFacade(
                inputTerminal,
                _typeVariableSet.CreateTypeVariableReferenceFromNIType(elementType));
            _nodeFacade[outputTerminal] = new SimpleTerminalFacade(
                outputTerminal,
                _typeVariableSet.CreateTypeVariableReferenceFromNIType(variantConstructorNode.VariantType));
            return true;
        }

        bool IDfirNodeVisitor<bool>.VisitVariantMatchStructureSelector(VariantMatchStructureSelector variantMatchStructureSelector)
        {
            var fieldTypes = new Dictionary<string, TypeVariableReference>();
            int fieldIndex = 0;
            foreach (var outputTerminal in variantMatchStructureSelector.OutputTerminals)
            {
                string fieldName = $"_{fieldIndex}";
                TypeVariableReference fieldType = _typeVariableSet.CreateReferenceToNewTypeVariable();
                fieldTypes[fieldName] = fieldType;
                _nodeFacade[outputTerminal] = new SimpleTerminalFacade(outputTerminal, fieldType);
                ++fieldIndex;
            }

            TypeVariableReference fieldedType = _typeVariableSet.CreateReferenceToIndefiniteFieldedType(fieldTypes);
            Terminal inputTerminal = variantMatchStructureSelector.InputTerminals[0];
            _nodeFacade[inputTerminal] = new SimpleTerminalFacade(inputTerminal, fieldedType);
            return true;
        }
    }
}
