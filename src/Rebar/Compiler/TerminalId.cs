using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NationalInstruments.Dfir;

namespace Rebar.Compiler
{
    internal static class TerminalId
    {
        /// <summary>
        /// Token that stores a unique identifier per <see cref="Terminal"/>. This is used instead of <see cref="Terminal.UniqueId"/> because it
        /// is stable across DfirRoot copies, which allows re-using <see cref="VariableSet"/>s across DfirRoot copies.
        /// </summary>
        private static readonly AttributeDescriptor _variableTerminalIdTokenName = new AttributeDescriptor("Rebar.Compiler.TerminalId", true);

        /// <summary>
        /// Token that stores the current highest terminal identifier for a DfirRoot.
        /// </summary>
        private static readonly AttributeDescriptor _currentTerminalIdTokenName = new AttributeDescriptor("Rebar.Compiler.DfirRootCurrentTerminalId", true);

        internal static int GetTerminalId(this Terminal terminal)
        {
            var token = terminal.DfirRoot.GetOrCreateNamedSparseAttributeToken<int>(_variableTerminalIdTokenName);
            int id = terminal.GetAttribute(token);
            if (id == 0)
            {
                var currentTerminalIdToken = terminal.DfirRoot.GetOrCreateNamedSparseAttributeToken<int>(_currentTerminalIdTokenName);
                int dfirRootCurrentTerminalId = terminal.DfirRoot.GetAttribute(currentTerminalIdToken);
                id = ++dfirRootCurrentTerminalId;
                terminal.DfirRoot.SetAttribute(currentTerminalIdToken, dfirRootCurrentTerminalId);
                terminal.SetAttribute(token, id);
            }
            return id;
        }
    }
}
