using System;
using System.Collections.Generic;
using System.Linq;
using NationalInstruments;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler;
using Rebar.Compiler.Nodes;
using Rebar.RebarTarget.LLVM;

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
    internal class Allocator : VisitorTransformBase, IDfirNodeVisitor<bool>, IInternalDfirNodeVisitor<bool>
    {
        private readonly Dictionary<VariableReference, ValueSource> _variableAllocations;
        private readonly Dictionary<object, ValueSource> _additionalAllocations;

        public Allocator(
            Dictionary<VariableReference, ValueSource> variableAllocations,
            Dictionary<object, ValueSource> additionalAllocations)
        {
            _variableAllocations = variableAllocations;
            _additionalAllocations = additionalAllocations;
        }

        public FunctionAllocationSet AllocationSet { get; } = new FunctionAllocationSet();

        private AllocationValueSource CreateLocalAllocation(string name, NIType type)
        {
            return AllocationSet.CreateStateField(name, type);
        }

        private AllocationValueSource CreateLocalAllocationForVariable(VariableReference variable)
        {
            string name = $"v{variable.Id}";
            var localAllocation = AllocationSet.CreateStateField(name, variable.Type);
            _variableAllocations[variable] = localAllocation;
            return localAllocation;
        }

        private AllocationValueSource CreateOutputParameterAllocationForVariable(VariableReference outputParameterVariable)
        {
            string name = $"v{outputParameterVariable.Id}";
            var allocation = AllocationSet.CreateOutputParameter(name, outputParameterVariable.Type);
            _variableAllocations[outputParameterVariable] = allocation;
            return allocation;
        }

        private void CreateConstantLocalReferenceForVariable(VariableReference referencingVariable, VariableReference referencedVariable)
        {
            ValueSource referencedValueSource = _variableAllocations[referencedVariable];
            if (!(referencedValueSource is AllocationValueSource))
            {
                throw new ArgumentException("Referenced variable does not have a local allocation.", "referencedVariable");
            }
            _variableAllocations[referencingVariable] = new ConstantLocalReferenceValueSource((IAddressableValueSource)referencedValueSource);
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
            ValueSource sourceValueSource = _variableAllocations[sourceVariable];
            foreach (var sinkVariable in wire.SinkTerminals.Skip(1).Select(VariableExtensions.GetTrueVariable))
            {
                if (sourceValueSource is AllocationValueSource)
                {
                    CreateLocalAllocationForVariable(sinkVariable);
                }
                else if (sourceValueSource is ConstantLocalReferenceValueSource)
                {
                    ReuseValueSource(sourceVariable, sinkVariable);
                }
            }
        }

        protected override void VisitDfirRoot(DfirRoot dfirRoot)
        {
            dfirRoot.DataItems.OrderBy(d => d.ConnectorPaneIndex).ForEach(VisitDataItem);
            base.VisitDfirRoot(dfirRoot);
        }

        private void VisitDataItem(DataItem dataItem)
        {
            if (dataItem.IsInput)
            {
                CreateLocalAllocationForVariable(dataItem.GetVariable());
            }
            else
            {
                CreateOutputParameterAllocationForVariable(dataItem.GetVariable());
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

        public bool VisitBuildTupleNode(BuildTupleNode buildTupleNode)
        {
            CreateLocalAllocationForVariable(buildTupleNode.OutputTerminals[0].GetTrueVariable());
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

        public bool VisitDecomposeTupleNode(DecomposeTupleNode decomposeTupleNode)
        {
            foreach (Terminal outputTerminal in decomposeTupleNode.OutputTerminals)
            {
                CreateLocalAllocationForVariable(outputTerminal.GetTrueVariable());
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
            _additionalAllocations[iterateTunnel.IntermediateValueName] =
                CreateLocalAllocation(iterateTunnel.IntermediateValueName, outputVariable.Type.CreateOption());
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
            var loopConditionAllocation = (AllocationValueSource)_variableAllocations[inputVariable];
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

        #region IInternalDfirNodeVisitor implementation

        bool IInternalDfirNodeVisitor<bool>.VisitAwaitNode(AwaitNode awaitNode)
        {
            VariableReference outputVariable = awaitNode.OutputTerminal.GetTrueVariable();
            // It may be the case that our output variable is the same as an upstream variable (e.g., because
            // it represents a passthrough of an async node). In that case we should reuse the value source
            // we already have for it.
            if (!_variableAllocations.ContainsKey(outputVariable))
            {
                CreateLocalAllocationForVariable(outputVariable);
            }
            return true;
        }

        bool IInternalDfirNodeVisitor<bool>.VisitCreateMethodCallPromise(CreateMethodCallPromise createMethodCallPromise)
        {
            CreateLocalAllocationForVariable(createMethodCallPromise.PromiseTerminal.GetTrueVariable());
            return true;
        }

#endregion
    }
}
