using System;
using System.Collections.Generic;
using System.Linq;
using NationalInstruments.Dfir;
using Rebar.Common;

namespace Rebar.Compiler
{
    /// <summary>
    /// Groups together all of the <see cref="TerminalFacade"/>s for a <see cref="Node"/>. For input terminals that expect to take
    /// references, groups these facades into <see cref="ReferenceInputTerminalLifetimeGroup"/>s.
    /// </summary>
    internal class AutoBorrowNodeFacade
    {
        /// <summary>
        /// Token that stores an <see cref="AutoBorrowNodeFacade"/>.
        /// </summary>
        private static readonly AttributeDescriptor _nodeFacadeTokenName = new AttributeDescriptor("Rebar.Compiler.NodeFacade", true);

        public static AutoBorrowNodeFacade GetNodeFacade(Node node)
        {
            var token = node.DfirRoot.GetOrCreateNamedSparseAttributeToken<AutoBorrowNodeFacade>(_nodeFacadeTokenName);
            return token.GetAttribute(node);
        }

        private Dictionary<int, TerminalFacade> _terminalFacades;
        private List<ReferenceInputTerminalLifetimeGroup> _lifetimeGroups;

        public TerminalFacade this[Terminal terminal]
        {
            get
            {
                if (_terminalFacades == null)
                {
                    return null;
                }
                TerminalFacade facade;
                _terminalFacades.TryGetValue(terminal.GetTerminalId(), out facade);
                return facade;
            }
            set
            {
                _terminalFacades = _terminalFacades ?? new Dictionary<int, TerminalFacade>();
                _terminalFacades[terminal.GetTerminalId()] = value;
            }
        }

        public ReferenceInputTerminalLifetimeGroup CreateInputLifetimeGroup(InputReferenceMutability mutability, Lazy<Lifetime> lazyNewLifetime, TypeVariableReference lifetimeType)
        {
            var lifetimeGroup = new ReferenceInputTerminalLifetimeGroup(this, mutability, lazyNewLifetime, lifetimeType);
            _lifetimeGroups = _lifetimeGroups ?? new List<ReferenceInputTerminalLifetimeGroup>();
            _lifetimeGroups.Add(lifetimeGroup);
            return lifetimeGroup;
        }

        private void DoForEachLifetimeGroup(Action<ReferenceInputTerminalLifetimeGroup> action)
        {
            foreach (ReferenceInputTerminalLifetimeGroup lifetimeGroup in _lifetimeGroups ?? Enumerable.Empty<ReferenceInputTerminalLifetimeGroup>())
            {
                action(lifetimeGroup);
            }
        }

        public void SetLifetimeInterruptedVariables(LifetimeVariableAssociation lifetimeVariableAssociation)
        {
            DoForEachLifetimeGroup(l => l.SetInterruptedVariables(lifetimeVariableAssociation));
        }

        public void FinalizeAutoBorrows()
        {
            DoForEachLifetimeGroup(l => l.FinalizeAutoBorrows());
        }

        public void CreateBorrowAndTerminateLifetimeNodes()
        {
            DoForEachLifetimeGroup(l => l.CreateBorrowAndTerminateLifetimeNodes());
        }
    }
}
