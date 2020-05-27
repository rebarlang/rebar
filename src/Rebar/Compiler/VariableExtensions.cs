using System;
using NationalInstruments.Dfir;
using Rebar.Common;

namespace Rebar.Compiler
{
    internal static class VariableExtensions
    {
        private static readonly AttributeDescriptor _typeVariableSetTokenName = new AttributeDescriptor("Rebar.Compiler.TypeVariableSet", true);
        private static readonly AttributeDescriptor _lifetimeGraphTreeTokenName = new AttributeDescriptor("Rebar.Compiler.LifetimeGraphTree", true);
        private static readonly AttributeDescriptor _variableSetTokenName = new AttributeDescriptor("Rebar.Compiler.VariableSet", true);
        private static readonly AttributeDescriptor _lifetimeGraphIdentifierTokenName = new AttributeDescriptor("Rebar.Compiler.LifetimeGraphIdentifier", true);
        private static readonly AttributeDescriptor _dataItemVariableTokenName = new AttributeDescriptor("Rebar.Compiler.DataItemVariable", true);
        private static readonly AttributeDescriptor _frameConditionVariableTokenName = new AttributeDescriptor("Rebar.Compiler.FrameConditionVariable", true);

        public static TypeVariableSet GetTypeVariableSet(this DfirRoot dfirRoot)
        {
            var token = dfirRoot.GetOrCreateNamedSparseAttributeToken<TypeVariableSet>(_typeVariableSetTokenName);
            return token.GetAttribute(dfirRoot);
        }

        public static LifetimeGraphTree GetLifetimeGraphTree(this DfirRoot dfirRoot)
        {
            var token = dfirRoot.GetOrCreateNamedSparseAttributeToken<LifetimeGraphTree>(_lifetimeGraphTreeTokenName);
            return token.GetAttribute(dfirRoot);
        }

        public static LifetimeGraphIdentifier GetLifetimeGraphIdentifier(this Diagram diagram)
        {
            var token = diagram.DfirRoot.GetNamedSparseAttributeToken<LifetimeGraphIdentifier>(_lifetimeGraphIdentifierTokenName);
            return token.GetAttribute(diagram);
        }

        public static void SetLifetimeGraphIdentifier(this Diagram diagram, LifetimeGraphIdentifier identifier)
        {
            var token = diagram.DfirRoot.GetOrCreateNamedSparseAttributeToken<LifetimeGraphIdentifier>(_lifetimeGraphIdentifierTokenName);
            token.SetAttribute(diagram, identifier);
        }

        public static Diagram FindDiagramForGraphIdentifier(this LifetimeGraphIdentifier identifier, Diagram startSearch)
        {
            Diagram current = startSearch;
            while (current != null)
            {
                if (current.GetLifetimeGraphIdentifier().Equals(identifier))
                {
                    return current;
                }
                current = current.ParentStructure?.ParentDiagram;
            }
            return null;
        }

        public static VariableReference GetVariable(this DataItem dataItem)
        {
            var token = dataItem.DfirRoot.GetOrCreateNamedSparseAttributeToken<VariableReference>(_dataItemVariableTokenName);
            return token.GetAttribute(dataItem);
        }

        public static void SetVariable(this DataItem dataItem, VariableReference variableReference)
        {
            var token = dataItem.DfirRoot.GetOrCreateNamedSparseAttributeToken<VariableReference>(_dataItemVariableTokenName);
            token.SetAttribute(dataItem, variableReference);
        }

        public static VariableReference GetConditionVariable(this Frame frame)
        {
            var token = frame.DfirRoot.GetOrCreateNamedSparseAttributeToken<VariableReference>(_frameConditionVariableTokenName);
            return token.GetAttribute(frame);
        }

        public static void SetConditionVariable(this Frame frame, VariableReference variableReference)
        {
            var token = frame.DfirRoot.GetOrCreateNamedSparseAttributeToken<VariableReference>(_frameConditionVariableTokenName);
            token.SetAttribute(frame, variableReference);
        }

        public static TypeVariableSet GetTypeVariableSet(this DfirElement element)
        {
            return element.DfirRoot.GetTypeVariableSet();
        }

        public static VariableSet GetVariableSet(this DfirRoot dfirRoot)
        {
            var token = dfirRoot.GetOrCreateNamedSparseAttributeToken<VariableSet>(_variableSetTokenName);
            return token.GetAttribute(dfirRoot);
        }

        public static void SetVariableSet(this DfirRoot dfirRoot, VariableSet variableSet)
        {
            var token = dfirRoot.GetOrCreateNamedSparseAttributeToken<VariableSet>(_variableSetTokenName);
            dfirRoot.SetAttribute(token, variableSet);
        }

        public static VariableSet GetVariableSet(this DfirElement dfirElement)
        {
            if (dfirElement is BorderNode)
            {
                throw new ArgumentException("Cannot get a VariableSet from a BorderNode; specify a Terminal instead.");
            }
            return dfirElement.DfirRoot.GetVariableSet();
        }

        /// <summary>
        /// Gets the true <see cref="VariableReference"/> associated with the <see cref="Terminal"/>, i.e., the reference
        /// to the variable that will be supplied directly to the terminal as input.
        /// </summary>
        /// <param name="terminal">The terminal.</param>
        /// <returns>The true <see cref="VariableReference"/>.</returns>
        public static VariableReference GetTrueVariable(this Terminal terminal)
        {
            TerminalFacade terminalFacade = AutoBorrowNodeFacade.GetNodeFacade(terminal.ParentNode)[terminal];
            return terminalFacade?.TrueVariable ?? new VariableReference();
        }

        /// <summary>
        /// Gets the facade <see cref="VariableReference"/> associated with the <see cref="Terminal"/>, i.e., the reference
        /// to the variable seen from the outside to be associated with the terminal. This may be different from the 
        /// terminal's true variable as a result of auto-borrowing.
        /// </summary>
        /// <param name="terminal">The terminal.</param>
        /// <returns>The true <see cref="VariableReference"/>.</returns>
        public static VariableReference GetFacadeVariable(this Terminal terminal)
        {
            TerminalFacade terminalFacade = AutoBorrowNodeFacade.GetNodeFacade(terminal.ParentNode)[terminal];
            return terminalFacade?.FacadeVariable ?? new VariableReference();
        }

        public static VariableReference CreateNewVariable(this Terminal terminal, TypeVariableReference typeVariableReference = default(TypeVariableReference), bool mutable = false)
        {
            int diagramLifetimeId = terminal.ParentDiagram.GetLifetimeGraphIdentifier().Id;
            return terminal.GetVariableSet().CreateNewVariable(diagramLifetimeId, typeVariableReference, mutable);
        }

        public static VariableReference CreateNewVariableForUnwiredTerminal(this Terminal terminal)
        {
            VariableSet variableSet = terminal.GetVariableSet();
            int diagramLifetimeId = terminal.ParentDiagram.GetLifetimeGraphIdentifier().Id;
            return variableSet.CreateNewVariable(diagramLifetimeId, variableSet.TypeVariableSet.CreateReferenceToNewTypeVariable());
        }

        public static VariableUsageValidator GetValidator(this Terminal terminal)
        {
            return new VariableUsageValidator(terminal);
        }

        public static Lifetime GetDiagramLifetime(this Terminal terminal)
        {
            return terminal.DfirRoot.GetLifetimeGraphTree().GetLifetimeGraphRootLifetime(terminal.ParentDiagram.GetLifetimeGraphIdentifier());
        }

        public static Lifetime DefineLifetimeThatIsBoundedByDiagram(this Terminal terminal)
        {
            return terminal.DfirRoot.GetLifetimeGraphTree().CreateLifetimeThatIsBoundedByLifetimeGraph(terminal.ParentDiagram.GetLifetimeGraphIdentifier());
        }

        public static bool IsDiagramLifetime(this Lifetime lifetime, Diagram diagram)
        {
            return lifetime == diagram.DfirRoot.GetLifetimeGraphTree().GetLifetimeGraphRootLifetime(diagram.GetLifetimeGraphIdentifier());
        }

        public static bool DoesOutlastDiagram(this Lifetime lifetime, Diagram diagram)
        {
            return lifetime.DoesOutlastLifetimeGraph(diagram.GetLifetimeGraphIdentifier());
        }

        public static ITypeUnificationResult UnifyTerminalTypeWith(
            this Terminal terminal,
            TypeVariableReference terminalType,
            TypeVariableReference unifyWithType,
            ITypeUnificationResultFactory unificationResultFactory)
        {
            ITypeUnificationResult unificationResult = unificationResultFactory.GetTypeUnificationResult(
                terminal,
                terminalType,
                unifyWithType);
            terminalType.TypeVariableSet.Unify(terminalType, unifyWithType, unificationResult);
            return unificationResult;
        }
    }
}
