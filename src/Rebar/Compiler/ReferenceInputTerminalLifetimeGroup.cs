using System;
using System.Collections.Generic;
using System.Linq;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler.Nodes;

namespace Rebar.Compiler
{
    internal sealed class ReferenceInputTerminalLifetimeGroup
    {
        private readonly AutoBorrowNodeFacade _nodeFacade;
        private readonly InputReferenceMutability _mutability;
        private readonly List<TerminalFacade> _facades = new List<TerminalFacade>();
        private bool _borrowRequired, _mutableBorrow;
        private readonly Lazy<Lifetime> _lazyBorrowLifetime;

        public ReferenceInputTerminalLifetimeGroup(
            AutoBorrowNodeFacade nodeFacade,
            InputReferenceMutability mutability,
            Lazy<Lifetime> lazyNewLifetime,
            TypeVariableReference lifetimeType)
        {
            _nodeFacade = nodeFacade;
            _mutability = mutability;
            _lazyBorrowLifetime = lazyNewLifetime;
            LifetimeType = lifetimeType;
        }

        private Lifetime BorrowLifetime => _lazyBorrowLifetime.Value;

        public TypeVariableReference LifetimeType { get; }

        private void SetBorrowRequired(bool mutableBorrow)
        {
            _borrowRequired = true;
            _mutableBorrow = mutableBorrow;
        }

        public void AddTerminalFacade(Terminal inputTerminal, TypeVariableReference referentTypeReference, TypeVariableReference mutabilityTypeReference, Terminal terminateLifetimeOutputTerminal = null)
        {
            TypeVariableReference referenceType;
            TypeVariableSet typeVariableSet = inputTerminal.GetTypeVariableSet();
            if (_mutability == InputReferenceMutability.Polymorphic)
            {
                referenceType = typeVariableSet.CreateReferenceToPolymorphicReferenceType(
                    mutabilityTypeReference,
                    referentTypeReference,
                    LifetimeType);
            }
            else
            {
                referenceType = typeVariableSet.CreateReferenceToReferenceType(
                    (_mutability != InputReferenceMutability.AllowImmutable),
                    referentTypeReference,
                    LifetimeType);
            }
            if (_lazyBorrowLifetime.IsValueCreated)
            {
                throw new InvalidOperationException("Cannot add borrowed variables after creating new lifetime.");
            }

            bool isStringSliceReference = typeVariableSet.GetTypeName(referentTypeReference) == DataTypes.StringSliceType.GetName();
            TerminalFacade terminalFacade;
            if (isStringSliceReference)
            {
                terminalFacade = new StringSliceReferenceInputTerminalFacade(inputTerminal, this, referenceType);
            }
            else
            {
                terminalFacade = new ReferenceInputTerminalFacade(inputTerminal, _mutability, this, referenceType);
            }
            _nodeFacade[inputTerminal] = terminalFacade;
            _facades.Add(terminalFacade);
            if (terminateLifetimeOutputTerminal != null)
            {
                var outputFacade = new TerminateLifetimeOutputTerminalFacade(terminateLifetimeOutputTerminal, terminalFacade);
                _nodeFacade[terminateLifetimeOutputTerminal] = outputFacade;
            }
        }

        internal void FinalizeAutoBorrows()
        {
            // For now, assume that the ReferenceInputTerminalFacades are setting _borrowRequired correctly.
            // (They're not, in the case where the true variable lifetimes change.)
            bool borrowRequired = _borrowRequired;
            if (!borrowRequired)
            {
                foreach (TerminalFacade referenceInput in _facades)
                {
                    referenceInput.TrueVariable.MergeInto(referenceInput.FacadeVariable);
                }
            }
        }

        public void SetInterruptedVariables(LifetimeVariableAssociation lifetimeVariableAssociation)
        {
            if (_borrowRequired)
            {
                foreach (var facade in _facades)
                {
                    lifetimeVariableAssociation.AddVariableInterruptedByLifetime(facade.FacadeVariable, BorrowLifetime);
                }
            }
        }

        public void CreateBorrowAndTerminateLifetimeNodes(LifetimeVariableAssociation lifetimeVariableAssociation)
        {
            if (_borrowRequired)
            {
                Node parentNode = _facades.First().Terminal.ParentNode;
                BorrowMode borrowMode = _mutableBorrow ? BorrowMode.Mutable : BorrowMode.Immutable;
                int borrowInputCount = _facades.Count;
                Diagram inputParentDiagram = _facades.First().Terminal.ParentDiagram;
                var explicitBorrow = new ExplicitBorrowNode(inputParentDiagram, borrowMode, borrowInputCount, true, false);
                AutoBorrowNodeFacade borrowNodeFacade = AutoBorrowNodeFacade.GetNodeFacade(explicitBorrow);
                foreach (var terminal in explicitBorrow.Terminals)
                {
                    borrowNodeFacade[terminal] = new SimpleTerminalFacade(terminal, default(TypeVariableReference));
                }

                int index = 0;
                foreach (var facade in _facades)
                {
                    Terminal input = facade.Terminal;
                    VariableReference ownerVariable = input.GetFacadeVariable(), borrowVariable;
                    ((AutoborrowingInputTerminalFacade)facade).AddPostBorrowCoercion(ref input, out borrowVariable);
                    InsertBorrowAheadOfTerminal(input, explicitBorrow, index, ownerVariable, borrowVariable);
                    ++index;
                }

                List<TerminateLifetimeOutputTerminalFacade> terminates = new List<TerminateLifetimeOutputTerminalFacade>();
                foreach (var terminal in parentNode.OutputTerminals)
                {
                    var terminateFacade = _nodeFacade[terminal] as TerminateLifetimeOutputTerminalFacade;
                    if (terminateFacade != null && _facades.Contains(terminateFacade.InputFacade))
                    {
                        terminates.Add(terminateFacade);
                    }
                }

                if (terminates.Count == borrowInputCount)
                {
                    Diagram outputParentDiagram = terminates.First().Terminal.ParentDiagram;
                    var terminateLifetime = TerminateLifetimeNodeHelpers.CreateTerminateLifetimeWithFacades(outputParentDiagram, borrowInputCount, borrowInputCount);

                    index = 0;
                    foreach (var terminate in terminates)
                    {
                        InsertTerminateLifetimeBehindTerminal(terminate.Terminal, terminateLifetime, index, lifetimeVariableAssociation);
                        ++index;
                    }
                }
                else if (terminates.Count > 0)
                {
                    throw new InvalidOperationException("Mismatched terminates and borrows; not sure what to do");
                }
            }
        }

        private static void InsertBorrowAheadOfTerminal(
            Terminal borrowReceiver,
            ExplicitBorrowNode explicitBorrow,
            int index,
            VariableReference ownerVariable,
            VariableReference borrowVariable)
        {
            Terminal borrowInput = explicitBorrow.InputTerminals.ElementAt(index),
                borrowOutput = explicitBorrow.OutputTerminals.ElementAt(index);

            // wiring
            borrowReceiver.ConnectedTerminal.ConnectTo(borrowInput);
            borrowOutput.WireTogether(borrowReceiver, SourceModelIdSource.NoSourceModelId);

            // variables
            borrowInput.GetFacadeVariable().MergeInto(ownerVariable);
            borrowOutput.GetFacadeVariable().MergeInto(borrowVariable);
        }

        private static void InsertTerminateLifetimeBehindTerminal(
            Terminal lifetimeSource,
            TerminateLifetimeNode terminateLifetime,
            int index,
            LifetimeVariableAssociation lifetimeVariableAssociation)
        {
            Terminal terminateLifetimeInput = terminateLifetime.InputTerminals.ElementAt(index),
                terminateLifetimeOutput = terminateLifetime.OutputTerminals.ElementAt(index);

            // wiring: output
            if (lifetimeSource.IsConnected)
            {
                lifetimeSource.ConnectedTerminal.ConnectTo(terminateLifetimeOutput);
            }
            lifetimeSource.WireTogether(terminateLifetimeInput, SourceModelIdSource.NoSourceModelId);

            // variables: output
            terminateLifetimeInput.GetFacadeVariable().MergeInto(lifetimeSource.GetTrueVariable());
            VariableReference facadeVariable = lifetimeSource.GetFacadeVariable();
            terminateLifetimeOutput.GetFacadeVariable().MergeInto(facadeVariable);
            Terminal liveTerminal;
            if (lifetimeVariableAssociation.TryGetVariableLiveTerminal(facadeVariable, out liveTerminal)
                && liveTerminal.GetDownstreamNodesSameDiagram().Contains(terminateLifetimeOutput.ParentNode))
            {
                lifetimeVariableAssociation.MarkVariableLive(facadeVariable, terminateLifetimeOutput);
            }
        }

        private abstract class AutoborrowingInputTerminalFacade : TerminalFacade
        {
            protected AutoborrowingInputTerminalFacade(Terminal terminal)
                : base(terminal)
            {
                TypeVariableSet = terminal.GetTypeVariableSet();
            }

            protected TypeVariableSet TypeVariableSet { get; }

            public override void UnifyWithConnectedWireTypeAsNodeInput(VariableReference wireFacadeVariable, TerminalTypeUnificationResults unificationResults)
            {
                FacadeVariable.MergeInto(wireFacadeVariable);
                bool setExpectedMutable;
                TypeVariableReference typeToUnifyWith = ComputeTypeToUnifyWith(wireFacadeVariable, out setExpectedMutable);
                ITypeUnificationResult unificationResult = unificationResults.GetTypeUnificationResult(
                    Terminal,
                    TrueVariable.TypeVariableReference,
                    typeToUnifyWith);
                if (setExpectedMutable)
                {
                    unificationResult.SetExpectedMutable();
                }
                TypeVariableSet.Unify(TrueVariable.TypeVariableReference, typeToUnifyWith, unificationResult);
            }

            public abstract void AddPostBorrowCoercion(ref Terminal inputTerminal, out VariableReference borrowVariable);

            protected abstract TypeVariableReference ComputeTypeToUnifyWith(VariableReference inputFacadeVariable, out bool setExpectedMutable);
        }

        private class ReferenceInputTerminalFacade : AutoborrowingInputTerminalFacade
        {
            private readonly InputReferenceMutability _mutability;
            private readonly ReferenceInputTerminalLifetimeGroup _group;

            public ReferenceInputTerminalFacade(
                Terminal terminal, 
                InputReferenceMutability mutability, 
                ReferenceInputTerminalLifetimeGroup group,
                TypeVariableReference referenceTypeReference)
                : base(terminal)
            {
                _mutability = mutability;
                _group = group;
                FacadeVariable = terminal.CreateNewVariable();
                TrueVariable = terminal.CreateNewVariable(referenceTypeReference);
            }

            public override VariableReference FacadeVariable { get; }

            public override VariableReference TrueVariable { get; }

            public override void AddPostBorrowCoercion(ref Terminal inputTerminal, out VariableReference borrowVariable)
            {
                borrowVariable = inputTerminal.GetTrueVariable();
            }

            protected override TypeVariableReference ComputeTypeToUnifyWith(VariableReference inputFacadeVariable, out bool setExpectedMutable)
            {
                TypeVariableReference other = inputFacadeVariable.TypeVariableReference;
                TypeVariableReference u, otherReferenceLifetime;
                bool otherIsMutableReference;
                bool otherIsReference = TypeVariableSet.TryDecomposeReferenceType(other, out u, out otherReferenceLifetime, out otherIsMutableReference);
                TypeVariableReference underlyingType = otherIsReference ? u : other;
                bool mutable = otherIsReference ? otherIsMutableReference : inputFacadeVariable.Mutable;

                TypeVariableReference typeToUnifyWith = default(TypeVariableReference);
                setExpectedMutable = false;
                switch (_mutability)
                {
                    case InputReferenceMutability.RequireMutable:
                        {
                            TypeVariableReference lifetimeType;
                            if (!otherIsReference)
                            {
                                _group.SetBorrowRequired(true);
                                lifetimeType = TypeVariableSet.CreateReferenceToLifetimeType(_group.BorrowLifetime);
                            }
                            else
                            {
                                lifetimeType = otherReferenceLifetime;
                            }
                            typeToUnifyWith = TypeVariableSet.CreateReferenceToReferenceType(true, underlyingType, lifetimeType);
                            setExpectedMutable = !mutable;
                            break;
                        }
                    case InputReferenceMutability.AllowImmutable:
                        {
                            bool needsBorrow = !(otherIsReference && !otherIsMutableReference);
                            TypeVariableReference lifetimeType;
                            if (needsBorrow)
                            {
                                lifetimeType = TypeVariableSet.CreateReferenceToLifetimeType(_group.BorrowLifetime);
                                _group.SetBorrowRequired(false);
                            }
                            else
                            {
                                lifetimeType = otherReferenceLifetime;
                            }
                            typeToUnifyWith = TypeVariableSet.CreateReferenceToReferenceType(false, underlyingType, lifetimeType);
                            break;
                        }
                    case InputReferenceMutability.Polymorphic:
                        {
                            TypeVariableReference lifetimeType;
                            if (!otherIsReference)
                            {
                                _group.SetBorrowRequired(false /* TODO depends on current state and input mutability */);
                                lifetimeType = TypeVariableSet.CreateReferenceToLifetimeType(_group.BorrowLifetime);
                            }
                            else
                            {
                                // TODO: if TrueVariable.TypeVariableReference is already known to be immutable and
                                // wire reference is mutable, then we need a borrow
                                lifetimeType = otherReferenceLifetime;
                            }
                            typeToUnifyWith = TypeVariableSet.CreateReferenceToReferenceType(mutable, underlyingType, lifetimeType);
                            break;
                        }
                }
                return typeToUnifyWith;
            }
        }
        
        private class StringSliceReferenceInputTerminalFacade : AutoborrowingInputTerminalFacade
        {
            private readonly ReferenceInputTerminalLifetimeGroup _group;
            private bool _stringToSliceNeeded = false;

            public StringSliceReferenceInputTerminalFacade(
                Terminal terminal,
                ReferenceInputTerminalLifetimeGroup group,
                TypeVariableReference referenceTypeReference)
                : base(terminal)
            {
                _group = group;
                FacadeVariable = terminal.CreateNewVariable();
                TrueVariable = terminal.CreateNewVariable(referenceTypeReference);
            }

            public override VariableReference FacadeVariable { get; }

            public override VariableReference TrueVariable { get; }

            public override void AddPostBorrowCoercion(ref Terminal inputTerminal, out VariableReference borrowVariable)
            {
                borrowVariable = inputTerminal.GetTrueVariable();
                if (_stringToSliceNeeded)
                {
                    InsertStringToSliceAheadOfTerminal(inputTerminal, out borrowVariable, out inputTerminal);
                }
            }

            private void InsertStringToSliceAheadOfTerminal(Terminal sliceReceiver, out VariableReference stringReferenceVariable, out Terminal stringReferenceTerminal)
            {
                FunctionalNode stringToSlice = new FunctionalNode(sliceReceiver.ParentDiagram, Signatures.StringToSliceType);
                Terminal stringToSliceInput = stringToSlice.InputTerminals[0],
                    stringToSliceOutput = stringToSlice.OutputTerminals[0];
                VariableReference sliceReceiverTrueVariable = sliceReceiver.GetTrueVariable();

                TypeVariableSet typeVariableSet = stringToSliceInput.GetTypeVariableSet();
                TypeVariableReference stringSliceReferenceType = sliceReceiverTrueVariable.TypeVariableReference;
                TypeVariableReference u, lifetime;
                bool m;
                typeVariableSet.TryDecomposeReferenceType(stringSliceReferenceType, out u, out lifetime, out m);
                TypeVariableReference stringReferenceType = typeVariableSet.CreateReferenceToReferenceType(
                    false,
                    typeVariableSet.CreateTypeVariableReferenceFromNIType(PFTypes.String),
                    lifetime);

                AutoBorrowNodeFacade stringToSliceFacade = AutoBorrowNodeFacade.GetNodeFacade(stringToSlice);
                stringToSliceFacade[stringToSliceInput] = new SimpleTerminalFacade(stringToSliceInput, stringReferenceType);
                stringToSliceFacade[stringToSliceOutput] = new SimpleTerminalFacade(stringToSliceOutput, default(TypeVariableReference));

                sliceReceiver.ConnectedTerminal.ConnectTo(stringToSliceInput);
                stringToSliceOutput.WireTogether(sliceReceiver, SourceModelIdSource.NoSourceModelId);

                stringToSliceOutput.GetFacadeVariable().MergeInto(sliceReceiverTrueVariable);
                stringReferenceVariable = stringToSliceInput.GetFacadeVariable();
                stringReferenceTerminal = stringToSliceInput;
            }

            protected override TypeVariableReference ComputeTypeToUnifyWith(VariableReference inputFacadeVariable, out bool setExpectedMutable)
            {
                setExpectedMutable = false;
                TypeVariableReference other = inputFacadeVariable.TypeVariableReference;
                TypeVariableReference u, l;
                bool otherIsMutableReference;
                bool otherIsReference = TypeVariableSet.TryDecomposeReferenceType(other, out u, out l, out otherIsMutableReference);
                TypeVariableReference underlyingType = otherIsReference ? u : other;

                string typeName = TypeVariableSet.GetTypeName(underlyingType);
                bool underlyingTypeIsStringSlice = typeName == DataTypes.StringSliceType.GetName(),
                    underlyingTypeIsString = typeName == PFTypes.String.GetName();
                bool inputCoercesToStringSlice = underlyingTypeIsString || underlyingTypeIsStringSlice;
                bool needsBorrow = !otherIsReference || !underlyingTypeIsStringSlice;
                TypeVariableReference lifetimeType = otherIsReference
                    ? l
                    : TypeVariableSet.CreateReferenceToLifetimeType(_group.BorrowLifetime);

                if (needsBorrow)
                {
                    _stringToSliceNeeded = true;
                    _group.SetBorrowRequired(false);
                }
                // If the input is allowed to coerce to a str, we can unify with str;
                // otherwise, unify with the underlying type to get a type mismatch.
                TypeVariableReference toUnifyUnderlyingType = inputCoercesToStringSlice
                    ? TypeVariableSet.CreateTypeVariableReferenceFromNIType(DataTypes.StringSliceType)
                    : underlyingType;
                return TypeVariableSet.CreateReferenceToReferenceType(
                    false,
                    toUnifyUnderlyingType,
                    lifetimeType);
            }
        }

        /// <summary>
        /// <see cref="TerminalFacade"/> implementation for output terminals that will terminate the lifetime started by
        /// a related auto-borrowed input terminal. Its variables are identical to the corresponding variables of the related input.
        /// </summary>
        private class TerminateLifetimeOutputTerminalFacade : TerminalFacade
        {
            public TerminateLifetimeOutputTerminalFacade(Terminal terminal, TerminalFacade inputFacade)
                : base(terminal)
            {
                InputFacade = inputFacade;
            }

            public override VariableReference FacadeVariable => InputFacade.FacadeVariable;

            public override VariableReference TrueVariable => InputFacade.TrueVariable;

            public TerminalFacade InputFacade { get; }

            public override void UnifyWithConnectedWireTypeAsNodeInput(VariableReference wireFacadeVariable, TerminalTypeUnificationResults unificationResults)
            {
                // we're a node output facade; this should never be called.
                throw new NotImplementedException();
            }
        }
    }
}
