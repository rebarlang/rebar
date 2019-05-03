using System;
using System.Collections.Generic;
using System.Linq;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler.Nodes;

namespace Rebar.Compiler
{
    internal sealed class ReferenceInputTerminalLifetimeGroup
    {
        private readonly AutoBorrowNodeFacade _nodeFacade;
        private readonly InputReferenceMutability _mutability;
        private readonly List<ReferenceInputTerminalFacade> _facades = new List<ReferenceInputTerminalFacade>();
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

        private TypeVariableReference LifetimeType;

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

            var terminalFacade = new ReferenceInputTerminalFacade(inputTerminal, _mutability, this, referenceType);
            _nodeFacade[inputTerminal] = terminalFacade;
            _facades.Add(terminalFacade);
            if (terminateLifetimeOutputTerminal != null)
            {
                var outputFacade = new TerminateLifetimeOutputTerminalFacade(terminateLifetimeOutputTerminal, terminalFacade);
                _nodeFacade[terminateLifetimeOutputTerminal] = outputFacade;
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

        public void CreateBorrowAndTerminateLifetimeNodes()
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
                    InsertBorrowAheadOfTerminal(input, explicitBorrow, index);
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
                    var terminateLifetime = new TerminateLifetimeNode(outputParentDiagram, borrowInputCount, borrowInputCount);
                    AutoBorrowNodeFacade terminateLifetimeFacade = AutoBorrowNodeFacade.GetNodeFacade(terminateLifetime);
                    foreach (var terminal in terminateLifetime.Terminals)
                    {
                        terminateLifetimeFacade[terminal] = new SimpleTerminalFacade(terminal, default(TypeVariableReference));
                    }

                    index = 0;
                    foreach (var terminate in terminates)
                    {
                        InsertTerminateLifetimeBehindTerminal(terminate.Terminal, terminateLifetime, index);
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
            int index)
        {
            Terminal borrowInput = explicitBorrow.InputTerminals.ElementAt(index),
                borrowOutput = explicitBorrow.OutputTerminals.ElementAt(index);

            // wiring
            borrowReceiver.ConnectedTerminal.ConnectTo(borrowInput);
            borrowOutput.WireTogether(borrowReceiver, SourceModelIdSource.NoSourceModelId);

            // variables
            borrowInput.GetFacadeVariable().MergeInto(borrowReceiver.GetFacadeVariable());
            borrowOutput.GetFacadeVariable().MergeInto(borrowReceiver.GetTrueVariable());
        }

        private static void InsertTerminateLifetimeBehindTerminal(
            Terminal lifetimeSource,
            TerminateLifetimeNode terminateLifetime,
            int index)
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
            terminateLifetimeOutput.GetFacadeVariable().MergeInto(lifetimeSource.GetFacadeVariable());
        }

        private class ReferenceInputTerminalFacade : TerminalFacade
        {
            private readonly VariableSet _variableSet;
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
                _variableSet = terminal.GetVariableSet();
                FacadeVariable = _variableSet.CreateNewVariable(default(TypeVariableReference));
                TrueVariable = _variableSet.CreateNewVariable(referenceTypeReference);
            }

            public override VariableReference FacadeVariable { get; }

            public override VariableReference TrueVariable { get; }

            public override void UnifyWithConnectedWireTypeAsNodeInput(VariableReference wireFacadeVariable, TerminalTypeUnificationResults unificationResults)
            {
                FacadeVariable.MergeInto(wireFacadeVariable);

                TypeVariableSet typeVariableSet = _variableSet.TypeVariableSet;
                TypeVariableReference other = wireFacadeVariable.TypeVariableReference;
                TypeVariableReference u, l;
                bool otherIsMutableReference;
                bool otherIsReference = typeVariableSet.TryDecomposeReferenceType(other, out u, out l, out otherIsMutableReference);
                TypeVariableReference underlyingType = otherIsReference ? u : other;
                switch (_mutability)
                {
                    case InputReferenceMutability.RequireMutable:
                        {
                            TypeVariableReference lifetimeType;
                            if (!otherIsReference)
                            {
                                _group.SetBorrowRequired(true);
                                lifetimeType = typeVariableSet.CreateReferenceToLifetimeType(_group.BorrowLifetime);                                
                            }
                            else
                            {
                                lifetimeType = l;
                            }
                            TypeVariableReference mutableReference = typeVariableSet.CreateReferenceToReferenceType(true, underlyingType, lifetimeType);
                            ITypeUnificationResult unificationResult = unificationResults.GetTypeUnificationResult(
                                Terminal,
                                TrueVariable.TypeVariableReference,
                                mutableReference);
                            bool mutable = otherIsReference ? otherIsMutableReference : wireFacadeVariable.Mutable;
                            if (!mutable)
                            {
                                unificationResult.SetExpectedMutable();
                            }
                            typeVariableSet.Unify(TrueVariable.TypeVariableReference, mutableReference, unificationResult);
                            // TODO: after unifying these two, might be good to remove mutRef--I guess by merging?
                            break;
                        }
                    case InputReferenceMutability.AllowImmutable:
                        {
                            bool needsBorrow = !(otherIsReference && !otherIsMutableReference);
                            TypeVariableReference lifetimeType;
                            if (needsBorrow)
                            {
                                lifetimeType = typeVariableSet.CreateReferenceToLifetimeType(_group.BorrowLifetime);
                                _group.SetBorrowRequired(false);
                            }
                            else
                            {
                                lifetimeType = l;
                            }
                            TypeVariableReference immutableReference = typeVariableSet.CreateReferenceToReferenceType(false, underlyingType, lifetimeType);
                            ITypeUnificationResult unificationResult = unificationResults.GetTypeUnificationResult(
                                Terminal,
                                TrueVariable.TypeVariableReference,
                                immutableReference);
                            typeVariableSet.Unify(TrueVariable.TypeVariableReference, immutableReference, unificationResult);
                            // TODO: after unifying these two, might be good to remove immRef--I guess by merging?
                            break;
                        }
                    case InputReferenceMutability.Polymorphic:
                        {
                            TypeVariableReference lifetimeType;
                            if (!otherIsReference)
                            {
                                _group.SetBorrowRequired(false /* TODO depends on current state and input mutability */);
                                lifetimeType = typeVariableSet.CreateReferenceToLifetimeType(_group.BorrowLifetime);
                            }
                            else
                            {
                                // TODO: if TrueVariable.TypeVariableReference is already known to be immutable and
                                // wire reference is mutable, then we need a borrow
                                lifetimeType = l;
                            }
                            bool mutable = otherIsReference ? otherIsMutableReference : wireFacadeVariable.Mutable;
                            TypeVariableReference reference = typeVariableSet.CreateReferenceToReferenceType(mutable, underlyingType, lifetimeType);
                            ITypeUnificationResult unificationResult = unificationResults.GetTypeUnificationResult(
                                Terminal,
                                TrueVariable.TypeVariableReference,
                                reference);
                            typeVariableSet.Unify(TrueVariable.TypeVariableReference, reference, unificationResult);
                            break;
                        }
                }
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
