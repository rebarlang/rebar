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

        public ReferenceInputTerminalLifetimeGroup CreateInputLifetimeGroup(InputReferenceMutability mutability)
        {
            var lifetimeGroup = new ReferenceInputTerminalLifetimeGroup(this, mutability);
            _lifetimeGroups = _lifetimeGroups ?? new List<ReferenceInputTerminalLifetimeGroup>();
            _lifetimeGroups.Add(lifetimeGroup);
            return lifetimeGroup;
        }

        public void UpdateInputsFromFacadeTypes()
        {
            if (_lifetimeGroups != null)
            {
                foreach (var lifetimeGroup in _lifetimeGroups)
                {
                    lifetimeGroup.UpdateFacadesFromInput();
                }
            }
            if (_terminalFacades != null)
            {
                // TODO: need better way to distinguish which facades were not handled by lifetime groups
                foreach (TerminalFacade inputTerminalFacade in _terminalFacades.Values.Where(f => f.Terminal.IsInput).OfType<SimpleTerminalFacade>())
                {
                    inputTerminalFacade.UpdateFromFacadeInput();
                }
            }
        }

        public void CreateBorrowAndTerminateLifetimeNodes()
        {
            if (_lifetimeGroups != null)
            {
                foreach (var lifetimeGroup in _lifetimeGroups)
                {
                    lifetimeGroup.CreateBorrowAndTerminateLifetimeNodes();
                }
            }
        }
    }
}
