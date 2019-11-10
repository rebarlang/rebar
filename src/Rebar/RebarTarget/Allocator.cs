using System;
using System.Collections.Generic;
using System.Linq;
using NationalInstruments;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler;
using Rebar.Compiler.Nodes;

namespace Rebar.RebarTarget
{
    /// <summary>
    /// Transform that associates a local slot with each <see cref="VariableReference"/> in a <see cref="DfirRoot"/>.
    /// </summary>
    /// <remarks>For now, the implementation is the most naive one possible; it assigns every Variable
    /// its own unique local slot. Future implementations can improve on this by:
    /// * Determining when variables from two different sets can reuse local slots
    /// * Using the same frame space for variables of different types
    /// * Determining when semantic variables are actually constants and thus do not need to be
    /// allocated in the frame</remarks>
    internal abstract class Allocator<TValueSource, TAllocation, TReference> : VisitorTransformBase, IDfirNodeVisitor<bool>
        where TAllocation : TValueSource 
        where TReference : TValueSource
    {
        private readonly Dictionary<VariableReference, TValueSource> _variableAllocations;
        private readonly Dictionary<object, TValueSource> _additionalAllocations;

        public Allocator(
            Dictionary<VariableReference, TValueSource> variableAllocations,
            Dictionary<object, TValueSource> additionalAllocations)
        {
            _variableAllocations = variableAllocations;
            _additionalAllocations = additionalAllocations;
        }

        protected TValueSource GetValueSourceForVariable(VariableReference variable)
        {
            return _variableAllocations[variable];
        }

        protected abstract TAllocation CreateLocalAllocation(VariableReference variable);

        protected abstract TAllocation CreateLocalAllocation(string name, NIType type);

        private TAllocation CreateLocalAllocationForVariable(VariableReference variable)
        {
            var localAllocation = CreateLocalAllocation(variable);
            _variableAllocations[variable] = localAllocation;
            return localAllocation;
        }

        protected abstract TReference CreateConstantLocalReference(VariableReference referencedVariable);
        
        private void CreateConstantLocalReferenceForVariable(VariableReference referencingVariable, VariableReference referencedVariable)
        {
            if (!(_variableAllocations[referencedVariable] is TAllocation))
            {
                throw new ArgumentException("Referenced variable does not have a local allocation.", "referencedVariable");
            }
            _variableAllocations[referencingVariable] = CreateConstantLocalReference(referencedVariable);
        }

        private void CreateReferenceValueSource(VariableReference referenceVariable, VariableReference referencedVariable)
        {
            if (referenceVariable.Mutable)
            {
                CreateLocalAllocationForVariable(referenceVariable);
                return;
            }

            // If the created reference binding is non-mutable, then we can create a ConstantLocalReferenceValueSource
            CreateConstantLocalReferenceForVariable(referenceVariable, referencedVariable);
            return;
        }

        private void ReuseValueSource(VariableReference originalVariable, VariableReference newVariable)
        {
            _variableAllocations[newVariable] = _variableAllocations[originalVariable];
        }

        protected override void VisitBorderNode(NationalInstruments.Dfir.BorderNode borderNode)
        {
            this.VisitRebarNode(borderNode);
        }

        protected override void VisitNode(Node node)
        {
            this.VisitRebarNode(node);
        }

        protected override void VisitWire(Wire wire)
        {
            if (!wire.SinkTerminals.HasMoreThan(1))
            {
                return;
            }
            VariableReference sourceVariable = wire.SourceTerminal.GetTrueVariable();
            TValueSource sourceValueSource = _variableAllocations[sourceVariable];
            foreach (var sinkVariable in wire.SinkTerminals.Skip(1).Select(VariableExtensions.GetTrueVariable))
            {
                if (sourceValueSource is TAllocation)
                {
                    CreateLocalAllocationForVariable(sinkVariable);
                }
                else if (sourceValueSource is TReference)
                {
                    ReuseValueSource(sourceVariable, sinkVariable);
                }
            }
        }

        #region IDfirNodeVisitor implementation
        // This visitor implementation parallels that of SetVariableTypesTransform:
        // For each variable created by a visited node, this should determine the appropriate ValueSource for that variable.

        public bool VisitBorrowTunnel(BorrowTunnel borrowTunnel)
        {
            VariableReference inputVariable = borrowTunnel.InputTerminals[0].GetTrueVariable(),
                outputVariable = borrowTunnel.OutputTerminals[0].GetTrueVariable();
            CreateReferenceValueSource(outputVariable, inputVariable);
            return true;
        }

        public bool VisitConstant(Constant constant)
        {
            CreateLocalAllocationForVariable(constant.OutputTerminal.GetTrueVariable());
            return true;
        }

        public bool VisitDataAccessor(DataAccessor dataAccessor)
        {
            if (dataAccessor.Terminal.Direction == Direction.Output)
            {
                VariableReference variable = dataAccessor.Terminal.GetTrueVariable();
                // For now, create a local allocation and copy the parameter value into it.
                CreateLocalAllocationForVariable(variable);
            }
            return true;
        }

        public bool VisitDropNode(DropNode dropNode)
        {
            return true;
        }

        public bool VisitExplicitBorrowNode(ExplicitBorrowNode explicitBorrowNode)
        {
            foreach (KeyValuePair<Terminal, Terminal> terminalPair in explicitBorrowNode.InputTerminals.Zip(explicitBorrowNode.OutputTerminals))
            {
                VariableReference inputVariable = terminalPair.Key.GetTrueVariable(),
                    outputVariable = terminalPair.Value.GetTrueVariable();
                if (inputVariable.Type == outputVariable.Type || inputVariable.Type.IsReferenceToSameTypeAs(outputVariable.Type))
                {
                    ReuseValueSource(inputVariable, outputVariable);
                }
                else
                {
                    // TODO: there is a bug here with creating a reference to an immutable reference binding;
                    // in CreateReferenceValueSource we create a constant reference value source for the immutable reference,
                    // which means we can't create a reference to an allocation for it.
                    CreateReferenceValueSource(outputVariable, inputVariable);
                }
            }
            return true;
        }

        public bool VisitFunctionalNode(FunctionalNode functionalNode)
        {
            VisitFunctionSignatureNode(functionalNode, functionalNode.Signature);
            return true;
        }

        public bool VisitIterateTunnel(IterateTunnel iterateTunnel)
        {
            VariableReference outputVariable = iterateTunnel.OutputTerminals[0].GetTrueVariable();
            CreateLocalAllocationForVariable(outputVariable);
            _additionalAllocations[iterateTunnel] = CreateLocalAllocation(
                "iterateTunnel" + iterateTunnel.UniqueId,
                outputVariable.Type.CreateOption());
            return true;
        }

        public bool VisitLockTunnel(LockTunnel lockTunnel)
        {
            CreateLocalAllocationForVariable(lockTunnel.OutputTerminals[0].GetTrueVariable());
            return true;
        }

        public bool VisitLoopConditionTunnel(LoopConditionTunnel loopConditionTunnel)
        {
            Terminal inputTerminal = loopConditionTunnel.InputTerminals[0],
                outputTerminal = loopConditionTunnel.OutputTerminals[0];
            VariableReference inputVariable = inputTerminal.GetTrueVariable();
            if (!inputTerminal.IsConnected)
            {
                CreateLocalAllocationForVariable(inputVariable);
            }
            var loopConditionAllocation = (TAllocation)_variableAllocations[inputVariable];
            CreateConstantLocalReferenceForVariable(outputTerminal.GetTrueVariable(), inputVariable);
            return true;
        }

        public bool VisitMethodCallNode(MethodCallNode methodCallNode)
        {
            VisitFunctionSignatureNode(methodCallNode, methodCallNode.Signature);
            return true;
        }

        public bool VisitOptionPatternStructureSelector(OptionPatternStructureSelector optionPatternStructureSelector)
        {
            Terminal someValueTerminal = optionPatternStructureSelector.OutputTerminals[0];
            VariableReference someValueVariable = someValueTerminal.GetTrueVariable();
            CreateLocalAllocationForVariable(someValueVariable);
            return true;
        }

        public bool VisitTerminateLifetimeNode(TerminateLifetimeNode terminateLifetimeNode)
        {
            return true;
        }

        public bool VisitTerminateLifetimeTunnel(TerminateLifetimeTunnel terminateLifetimeTunnel)
        {
            return true;
        }

        public bool VisitTunnel(Tunnel tunnel)
        {
            if (tunnel.Terminals.HasExactly(2))
            {
                VariableReference inputVariable = tunnel.InputTerminals.ElementAt(0).GetTrueVariable(),
                    outputVariable = tunnel.OutputTerminals.ElementAt(0).GetTrueVariable();
                if (outputVariable.Type == inputVariable.Type.CreateOption())
                {
                    CreateLocalAllocationForVariable(outputVariable);
                }
                else
                {
                    ReuseValueSource(inputVariable, outputVariable);
                }
            }
            else
            {
                // If this is an output tunnel, each input variable already has its own allocation, but
                // the output needs a distinct one (for now)
                // (Eventually we should try to share a single allocation for all variables.)
                if (tunnel.InputTerminals.HasMoreThan(1))
                {
                    CreateLocalAllocationForVariable(tunnel.OutputTerminals[0].GetTrueVariable());
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            return true;
        }

        public bool VisitUnwrapOptionTunnel(UnwrapOptionTunnel unwrapOptionTunnel)
        {
            // TODO: it would be nice to allow the output value source to reference an offset from the input value source,
            // rather than needing a separate allocation.
            CreateLocalAllocationForVariable(unwrapOptionTunnel.OutputTerminals[0].GetTrueVariable());
            return true;
        }

        private void VisitFunctionSignatureNode(Node node, NIType nodeFunctionSignature)
        {
            Signature signature = Signatures.GetSignatureForNIType(nodeFunctionSignature);
            foreach (var terminalPair in node.OutputTerminals.Zip(signature.Outputs).Where(pair => !pair.Value.IsPassthrough))
            {
                CreateLocalAllocationForVariable(terminalPair.Key.GetTrueVariable());
            }
        }

        #endregion
    }
}
