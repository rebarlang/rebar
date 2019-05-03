using System;
using System.Collections.Generic;
using System.Linq;
using NationalInstruments;
using NationalInstruments.DataTypes;
using NationalInstruments.Dfir;
using Rebar.Common;
using Rebar.Compiler;
using Rebar.Compiler.Nodes;
using Rebar.RebarTarget.Execution;

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
    internal sealed class Allocator : VisitorTransformBase, IDfirNodeVisitor<bool>
    {
        private readonly Dictionary<VariableReference, ValueSource> _variableAllocations;
        private int _currentIndex = 0;

        public Allocator(Dictionary<VariableReference, ValueSource> variableAllocations)
        {
            _variableAllocations = variableAllocations;
        }

        private LocalAllocationValueSource CreateLocalAllocationForVariable(VariableReference variable)
        {
            int size = GetTypeSize(variable.Type);
            var localAllocation = new LocalAllocationValueSource(_currentIndex, size);
            _variableAllocations[variable] = localAllocation;
            ++_currentIndex;
            return localAllocation;
        }

        private void CreateConstantLocalReferenceForVariable(VariableReference referencingVariable, VariableReference referencedVariable)
        {
            var localAllocation = _variableAllocations[referencedVariable] as LocalAllocationValueSource;
            if (localAllocation == null)
            {
                throw new ArgumentException("Referenced variable does not have a local allocation.", "referencedVariable");
            }
            _variableAllocations[referencingVariable] = new ConstantLocalReferenceValueSource(localAllocation.Index);
        }

        private int GetTypeSize(NIType type)
        {
            if (type.IsRebarReferenceType())
            {
                return TargetConstants.PointerSize;
            }
            NIType innerType;
            if (type.TryDestructureOptionType(out innerType))
            {
                return 4 + GetTypeSize(innerType);
            }
            if (type.IsInt32() || type.IsBoolean())
            {
                return 4;
            }
            if (type.IsIteratorType())
            {
                // for now, the only possible iterator is RangeIterator<int>
                // { current : i32, range_max : i32 }
                return 8;
            }
            throw new NotImplementedException("Unknown size for type " + type);
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
                if (sourceValueSource is LocalAllocationValueSource)
                {
                    CreateLocalAllocationForVariable(sinkVariable);
                }
                else if (sourceValueSource is ConstantLocalReferenceValueSource)
                {
                    _variableAllocations[sinkVariable] = sourceValueSource;
                }
            }
        }

        #region IDfirNodeVisitor implementation
        // This visitor implementation parallels that of SetVariableTypesTransform:
        // For each variable created by a visited node, this should determine the appropriate ValueSource for that variable.

        public bool VisitBorrowTunnel(BorrowTunnel borrowTunnel)
        {
            Terminal inputTerminal = borrowTunnel.Terminals.ElementAt(0),
                outputTerminal = borrowTunnel.Terminals.ElementAt(1);
            VariableReference inputVariable = inputTerminal.GetTrueVariable(),
                outputVariable = outputTerminal.GetTrueVariable();
            CreateReferenceValueSource(outputVariable, inputVariable);
            return true;
        }

        public bool VisitConstant(Constant constant)
        {
            CreateLocalAllocationForVariable(constant.OutputTerminal.GetTrueVariable());
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
                    _variableAllocations[outputVariable] = _variableAllocations[inputVariable];
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
            Signature signature = Signatures.GetSignatureForNIType(functionalNode.Signature);
            foreach (var terminalPair in functionalNode.OutputTerminals.Zip(signature.Outputs))
            {
                SignatureTerminal signatureTerminal = terminalPair.Value;
                if (signatureTerminal.IsPassthrough)
                {
                    continue;
                }
                CreateLocalAllocationForVariable(terminalPair.Key.GetTrueVariable());
            }
            return true;
        }

        public bool VisitIterateTunnel(IterateTunnel iterateTunnel)
        {
            CreateLocalAllocationForVariable(iterateTunnel.OutputTerminals.ElementAt(0).GetTrueVariable());
            return true;
        }

        public bool VisitLockTunnel(LockTunnel lockTunnel)
        {
            CreateLocalAllocationForVariable(lockTunnel.OutputTerminals.ElementAt(0).GetTrueVariable());
            return true;
        }

        public bool VisitLoopConditionTunnel(LoopConditionTunnel loopConditionTunnel)
        {
            Terminal inputTerminal = loopConditionTunnel.Terminals.ElementAt(0),
                outputTerminal = loopConditionTunnel.Terminals.ElementAt(1);
            VariableReference inputVariable = inputTerminal.GetTrueVariable();
            LocalAllocationValueSource loopConditionAllocation;
            if (!inputTerminal.IsConnected)
            {
                loopConditionAllocation = CreateLocalAllocationForVariable(inputVariable);
            }
            else
            {
                loopConditionAllocation = (LocalAllocationValueSource)_variableAllocations[inputVariable];
            }
            CreateConstantLocalReferenceForVariable(outputTerminal.GetTrueVariable(), inputVariable);
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
            VariableReference inputVariable = tunnel.InputTerminals.ElementAt(0).GetTrueVariable(),
                outputVariable = tunnel.OutputTerminals.ElementAt(0).GetTrueVariable();
            _variableAllocations[outputVariable] = _variableAllocations[inputVariable];
            return true;
        }

        public bool VisitUnwrapOptionTunnel(UnwrapOptionTunnel unwrapOptionTunnel)
        {
            // TODO: it would be nice to allow the output value source to reference an offset from the input value source,
            // rather than needing a separate allocation.
            CreateLocalAllocationForVariable(unwrapOptionTunnel.OutputTerminals.ElementAt(0).GetTrueVariable());
            return true;
        }

        #endregion
    }

    internal abstract class ValueSource
    {
        public ValueSource()
        {
        }
    }

    internal class LocalAllocationValueSource : ValueSource
    {
        public LocalAllocationValueSource(int index, int size)
        {
            Index = index;
            Size = size;
        }

        public int Index { get; }

        public int Size { get; }
    }

    internal class ConstantLocalReferenceValueSource : ValueSource
    {
        public ConstantLocalReferenceValueSource(int referencedIndex)
            : base()
        {
            ReferencedIndex = referencedIndex;
        }

        public int ReferencedIndex { get; }
    }
}
